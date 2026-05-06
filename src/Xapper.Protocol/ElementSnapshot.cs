namespace Xapper.Protocol;

/// <summary>
/// WPF 비주얼 트리의 단일 UI 요소를 나타내는 스냅샷.
/// TreeWalker가 비주얼 트리를 순회하며 생성하고, 자식 요소를 재귀적으로 포함.
/// </summary>
public sealed class ElementSnapshot
{
    /// <summary>요소를 식별하는 참조 번호. 스냅샷마다 재할당됨.</summary>
    public int Ref { get; set; }

    /// <summary>WPF 컨트롤 타입의 전체 이름 (예: "System.Windows.Controls.Button").</summary>
    public required string Type { get; set; }

    /// <summary>FrameworkElement.Name 속성값.</summary>
    public string? Name { get; set; }

    /// <summary>AutomationProperties.AutomationId 속성값.</summary>
    public string? AutomationId { get; set; }

    /// <summary>컨트롤의 텍스트 콘텐츠 (TextBlock.Text, ContentControl.Content 등).</summary>
    public string? Text { get; set; }

    /// <summary>요소의 활성화 상태 (IsEnabled).</summary>
    public bool IsEnabled { get; set; }

    /// <summary>요소의 가시성 상태 (Visibility == Visible).</summary>
    public bool IsVisible { get; set; }

    /// <summary>화면 좌표 기준의 바운딩 박스. 계산 불가 시 null.</summary>
    public BoundingBox? Bounds { get; set; }

    /// <summary>자식 요소 목록. 재귀적 트리 구조.</summary>
    public List<ElementSnapshot> Children { get; set; } = [];
}

/// <summary>
/// UI 요소의 화면 좌표 기준 바운딩 박스.
/// </summary>
public sealed class BoundingBox
{
    /// <summary>좌상단 X 좌표 (픽셀).</summary>
    public double X { get; set; }

    /// <summary>좌상단 Y 좌표 (픽셀).</summary>
    public double Y { get; set; }

    /// <summary>너비 (픽셀).</summary>
    public double Width { get; set; }

    /// <summary>높이 (픽셀).</summary>
    public double Height { get; set; }
}
