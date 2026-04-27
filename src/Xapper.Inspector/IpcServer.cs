using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Windows;
using Xapper.Protocol;
using Xapper.Protocol.Messages.Requests;
using Xapper.Protocol.Messages.Responses;
using Xapper.Inspector.VisualTree;
using Xapper.Inspector.Actions;

namespace Xapper.Inspector;

public sealed class IpcServer
{
    private readonly string _pipeName;
    private readonly RefRegistry _refRegistry = new();
    private readonly TreeWalker _treeWalker = new();
    private CancellationTokenSource? _cts;

    public IpcServer(string pipeName)
    {
        _pipeName = pipeName;
    }

    public async Task StartListening()
    {
        _cts = new CancellationTokenSource();

        while (!_cts.IsCancellationRequested)
        {
            await using var pipe = new NamedPipeServerStream(
                _pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            await pipe.WaitForConnectionAsync(_cts.Token);

            try
            {
                await HandleConnection(pipe, _cts.Token);
            }
            catch (Exception ex) when (ex is IOException or OperationCanceledException)
            {
                // Client disconnected or shutdown requested
            }
        }
    }

    private async Task HandleConnection(NamedPipeServerStream pipe, CancellationToken ct)
    {
        while (pipe.IsConnected && !ct.IsCancellationRequested)
        {
            var message = await IpcSerializer.DeserializeAsync(pipe, ct);
            if (message == null) break;

            var response = await ProcessMessage(message);
            var responseBytes = IpcSerializer.Serialize(response);
            await pipe.WriteAsync(responseBytes, ct);
            await pipe.FlushAsync(ct);
        }
    }

    private async Task<IpcMessage> ProcessMessage(IpcMessage message)
    {
        try
        {
            return message.Method switch
            {
                "ping" => await HandlePing(message),
                "snapshot" => await HandleSnapshot(message),
                "click" => await HandleClick(message),
                "type" => await HandleType(message),
                _ => IpcSerializer.CreateError(message.Id, $"Unknown method: {message.Method}")
            };
        }
        catch (Exception ex)
        {
            return IpcSerializer.CreateError(message.Id, ex.Message);
        }
    }

    private Task<IpcMessage> HandlePing(IpcMessage message)
    {
        var response = new PongResponse
        {
            ProcessId = Environment.ProcessId,
            ProcessName = System.Diagnostics.Process.GetCurrentProcess().ProcessName
        };
        return Task.FromResult(IpcSerializer.CreateResponse(message.Id, response));
    }

    private async Task<IpcMessage> HandleSnapshot(IpcMessage message)
    {
        var request = message.Payload.HasValue
            ? IpcSerializer.DeserializePayload<SnapshotRequest>(message.Payload.Value)
            : new SnapshotRequest();

        var snapshot = await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _refRegistry.Clear();

            DependencyObject? root = null;
            if (request.RootRef.HasValue)
            {
                root = _refRegistry.Resolve(request.RootRef.Value);
            }
            root ??= Application.Current.MainWindow;

            if (root == null)
                throw new InvalidOperationException("No root element found");

            return _treeWalker.Walk(root, _refRegistry, request.MaxDepth);
        });

        var response = new SnapshotResponse
        {
            Generation = _refRegistry.Generation,
            Root = snapshot
        };
        return IpcSerializer.CreateResponse(message.Id, response);
    }

    private async Task<IpcMessage> HandleClick(IpcMessage message)
    {
        var request = IpcSerializer.DeserializePayload<ClickRequest>(message.Payload!.Value);

        var element = _refRegistry.Resolve(request.Ref)
            ?? throw new InvalidOperationException($"Element ref={request.Ref} not found. Call snapshot first.");

        var waiter = new AutoWait.ElementWaiter(TimeSpan.FromMilliseconds(request.Timeout));
        await waiter.WaitForReady(element);

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            ClickAction.Execute(element);
        });

        var response = new ActionResponse { Success = true, Message = $"Clicked ref={request.Ref}" };
        return IpcSerializer.CreateResponse(message.Id, response);
    }

    private async Task<IpcMessage> HandleType(IpcMessage message)
    {
        var request = IpcSerializer.DeserializePayload<TypeTextRequest>(message.Payload!.Value);

        var element = _refRegistry.Resolve(request.Ref)
            ?? throw new InvalidOperationException($"Element ref={request.Ref} not found. Call snapshot first.");

        var waiter = new AutoWait.ElementWaiter(TimeSpan.FromMilliseconds(request.Timeout));
        await waiter.WaitForReady(element);

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            TypeAction.Execute(element, request.Text, request.Clear);
        });

        var response = new ActionResponse { Success = true, Message = $"Typed into ref={request.Ref}" };
        return IpcSerializer.CreateResponse(message.Id, response);
    }

    public void Stop() => _cts?.Cancel();
}
