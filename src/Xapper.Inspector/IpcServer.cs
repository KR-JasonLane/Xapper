using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Windows;
using Xapper.Protocol;
using Xapper.Protocol.Messages.Requests;
using Xapper.Protocol.Messages.Responses;
using Xapper.Inspector.VisualTree;
using Xapper.Inspector.Actions;
using Xapper.Inspector.Diagnostics;
using Xapper.Inspector.Capture;

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
                "select" => await HandleSelect(message),
                "toggle" => await HandleToggle(message),
                "expand" => await HandleExpand(message),
                "scroll" => await HandleScroll(message),
                "getProperty" => await HandleGetProperty(message),
                "getBindings" => await HandleGetBindings(message),
                "screenshot" => await HandleScreenshot(message),
                "assert" => await HandleAssert(message),
                "find" => await HandleFind(message),
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

    private async Task<IpcMessage> HandleSelect(IpcMessage message)
    {
        var request = IpcSerializer.DeserializePayload<SelectRequest>(message.Payload!.Value);
        var element = _refRegistry.Resolve(request.Ref)
            ?? throw new InvalidOperationException($"Element ref={request.Ref} not found.");

        var waiter = new AutoWait.ElementWaiter(TimeSpan.FromMilliseconds(request.Timeout));
        await waiter.WaitForReady(element);

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            SelectAction.Execute(element, request.ItemText, request.ItemIndex);
        });

        var response = new ActionResponse { Success = true, Message = $"Selected item in ref={request.Ref}" };
        return IpcSerializer.CreateResponse(message.Id, response);
    }

    private async Task<IpcMessage> HandleToggle(IpcMessage message)
    {
        var request = IpcSerializer.DeserializePayload<ToggleRequest>(message.Payload!.Value);
        var element = _refRegistry.Resolve(request.Ref)
            ?? throw new InvalidOperationException($"Element ref={request.Ref} not found.");

        var waiter = new AutoWait.ElementWaiter(TimeSpan.FromMilliseconds(request.Timeout));
        await waiter.WaitForReady(element);

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            ToggleAction.Execute(element);
        });

        var response = new ActionResponse { Success = true, Message = $"Toggled ref={request.Ref}" };
        return IpcSerializer.CreateResponse(message.Id, response);
    }

    private async Task<IpcMessage> HandleExpand(IpcMessage message)
    {
        var request = IpcSerializer.DeserializePayload<ExpandRequest>(message.Payload!.Value);
        var element = _refRegistry.Resolve(request.Ref)
            ?? throw new InvalidOperationException($"Element ref={request.Ref} not found.");

        var waiter = new AutoWait.ElementWaiter(TimeSpan.FromMilliseconds(request.Timeout));
        await waiter.WaitForReady(element);

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            ExpandAction.Execute(element, request.Expand);
        });

        var action = request.Expand ? "Expanded" : "Collapsed";
        var response = new ActionResponse { Success = true, Message = $"{action} ref={request.Ref}" };
        return IpcSerializer.CreateResponse(message.Id, response);
    }

    private async Task<IpcMessage> HandleScroll(IpcMessage message)
    {
        var request = IpcSerializer.DeserializePayload<ScrollRequest>(message.Payload!.Value);
        var element = _refRegistry.Resolve(request.Ref)
            ?? throw new InvalidOperationException($"Element ref={request.Ref} not found.");

        var waiter = new AutoWait.ElementWaiter(TimeSpan.FromMilliseconds(request.Timeout));
        await waiter.WaitForReady(element);

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            ScrollAction.Execute(element, request.HorizontalPercent, request.VerticalPercent);
        });

        var response = new ActionResponse { Success = true, Message = $"Scrolled ref={request.Ref}" };
        return IpcSerializer.CreateResponse(message.Id, response);
    }

    private async Task<IpcMessage> HandleGetProperty(IpcMessage message)
    {
        var request = IpcSerializer.DeserializePayload<GetPropertyRequest>(message.Payload!.Value);
        var element = _refRegistry.Resolve(request.Ref)
            ?? throw new InvalidOperationException($"Element ref={request.Ref} not found.");

        var response = await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var result = PropertyReader.ReadProperty(element, request.PropertyName);
            result.Ref = request.Ref;
            return result;
        });

        return IpcSerializer.CreateResponse(message.Id, response);
    }

    private async Task<IpcMessage> HandleGetBindings(IpcMessage message)
    {
        var request = IpcSerializer.DeserializePayload<GetBindingsRequest>(message.Payload!.Value);
        var element = _refRegistry.Resolve(request.Ref)
            ?? throw new InvalidOperationException($"Element ref={request.Ref} not found.");

        var response = await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var result = BindingInspector.GetBindings(element);
            result.Ref = request.Ref;
            return result;
        });

        return IpcSerializer.CreateResponse(message.Id, response);
    }

    private async Task<IpcMessage> HandleScreenshot(IpcMessage message)
    {
        var request = message.Payload.HasValue
            ? IpcSerializer.DeserializePayload<ScreenshotRequest>(message.Payload.Value)
            : new ScreenshotRequest();

        var response = await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (request.Ref.HasValue)
            {
                var element = _refRegistry.Resolve(request.Ref.Value) as UIElement
                    ?? throw new InvalidOperationException($"Element ref={request.Ref} not found or not a UIElement.");
                return ScreenshotCapture.CaptureElement(element);
            }
            return ScreenshotCapture.CaptureWindow();
        });

        return IpcSerializer.CreateResponse(message.Id, response);
    }

    private async Task<IpcMessage> HandleAssert(IpcMessage message)
    {
        var request = IpcSerializer.DeserializePayload<AssertRequest>(message.Payload!.Value);
        var element = _refRegistry.Resolve(request.Ref)
            ?? throw new InvalidOperationException($"Element ref={request.Ref} not found.");

        var (actual, passed) = await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var prop = PropertyReader.ReadProperty(element, request.Property);
            var actualValue = prop.Value ?? "null";
            var pass = actualValue.Equals(request.Expected, StringComparison.OrdinalIgnoreCase);
            return (actualValue, pass);
        });

        var response = new ActionResponse
        {
            Success = passed,
            Message = passed
                ? $"PASS: {request.Property} == \"{request.Expected}\""
                : $"FAIL: {request.Property} expected \"{request.Expected}\" but was \"{actual}\""
        };
        return IpcSerializer.CreateResponse(message.Id, response);
    }

    private async Task<IpcMessage> HandleFind(IpcMessage message)
    {
        var request = IpcSerializer.DeserializePayload<FindElementRequest>(message.Payload!.Value);

        var response = await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var root = Application.Current.MainWindow as DependencyObject;
            if (root == null)
                throw new InvalidOperationException("No main window found");

            var finder = new ElementFinder();
            return finder.Find(root, request, _refRegistry);
        });

        return IpcSerializer.CreateResponse(message.Id, response);
    }

    public void Stop() => _cts?.Cancel();
}
