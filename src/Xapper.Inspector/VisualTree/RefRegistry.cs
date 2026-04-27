using System.Windows;

namespace Xapper.Inspector.VisualTree;

public sealed class RefRegistry
{
    private readonly Dictionary<int, WeakReference<DependencyObject>> _refs = new();
    private int _nextRef;

    public int Generation { get; private set; }

    public int Register(DependencyObject element)
    {
        var refId = ++_nextRef;
        _refs[refId] = new WeakReference<DependencyObject>(element);
        return refId;
    }

    public DependencyObject? Resolve(int refId)
    {
        if (_refs.TryGetValue(refId, out var weakRef) && weakRef.TryGetTarget(out var element))
            return element;
        return null;
    }

    public void Clear()
    {
        _refs.Clear();
        _nextRef = 0;
        Generation++;
    }
}
