using System.Windows;

namespace Xapper.Inspector.VisualTree;

/// <summary>
/// 스냅샷에서 생성된 UI 요소 참조를 관리하는 레지스트리.
/// WeakReference를 사용하여 GC가 원본 요소를 수집할 수 있도록 하며,
/// 스냅샷마다 Clear()로 초기화되어 새 참조 번호가 할당됨.
/// </summary>
public sealed class RefRegistry
{
    #region Fields

    private readonly Dictionary<int, WeakReference<DependencyObject>> _refs = new();
    private int _nextRef;

    #endregion

    #region Properties

    /// <summary>
    /// 현재 세대 번호. Clear()가 호출될 때마다 증가하여 이전 세대의 참조가 무효화되었음을 나타냄.
    /// </summary>
    public int Generation { get; private set; }

    #endregion

    #region Public Methods

    /// <summary>
    /// UI 요소를 레지스트리에 등록하고 고유한 참조 번호를 반환합니다.
    /// </summary>
    /// <param name="element">등록할 DependencyObject.</param>
    /// <returns>할당된 참조 번호 (1부터 시작).</returns>
    public int Register(DependencyObject element)
    {
        var refId = ++_nextRef;
        _refs[refId] = new WeakReference<DependencyObject>(element);
        return refId;
    }

    /// <summary>
    /// 참조 번호로 UI 요소를 조회합니다.
    /// </summary>
    /// <param name="refId">조회할 참조 번호.</param>
    /// <returns>해당 요소. GC에 의해 수집되었거나 존재하지 않으면 null.</returns>
    public DependencyObject? Resolve(int refId)
    {
        if (_refs.TryGetValue(refId, out var weakRef) && weakRef.TryGetTarget(out var element))
            return element;
        return null;
    }

    /// <summary>
    /// 모든 참조를 초기화하고 세대 번호를 증가시킵니다.
    /// 새 스냅샷을 생성하기 전에 호출.
    /// </summary>
    public void Clear()
    {
        _refs.Clear();
        _nextRef = 0;
        Generation++;
    }

    #endregion
}
