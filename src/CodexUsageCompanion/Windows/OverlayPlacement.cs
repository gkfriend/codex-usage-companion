namespace CodexUsageCompanion.Windows;

public readonly record struct PixelRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;
    public int Height => Bottom - Top;
}

public readonly record struct PixelPoint(int X, int Y);

public enum OverlayPosition
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public readonly record struct OverlayPlacementRequest(
    nint OwnerHandle,
    int X,
    int Y,
    int Width,
    int Height);

public static class OverlayPlacement
{
    public static PixelPoint Calculate(
        PixelRect ownerBounds,
        int overlayWidth,
        int overlayHeight,
        int margin)
    {
        return Calculate(ownerBounds, overlayWidth, overlayHeight, margin, OverlayPosition.BottomRight);
    }

    public static PixelPoint Calculate(
        PixelRect ownerBounds,
        int overlayWidth,
        int overlayHeight,
        int margin,
        OverlayPosition position)
    {
        var left = ownerBounds.Left + margin;
        var right = ownerBounds.Right - overlayWidth - margin;
        var top = ownerBounds.Top + margin;
        var bottom = ownerBounds.Bottom - overlayHeight - margin;
        return new PixelPoint(
            position is OverlayPosition.TopLeft or OverlayPosition.BottomLeft ? left : right,
            position is OverlayPosition.TopLeft or OverlayPosition.TopRight ? top : bottom);
    }

    public static OverlayPosition ParsePosition(string position)
    {
        return position switch
        {
            "top-left" => OverlayPosition.TopLeft,
            "top-right" => OverlayPosition.TopRight,
            "bottom-left" => OverlayPosition.BottomLeft,
            _ => OverlayPosition.BottomRight
        };
    }

    public static bool ShouldApply(
        OverlayPlacementRequest? previous,
        OverlayPlacementRequest current)
    {
        return previous is null || previous.Value != current;
    }
}
