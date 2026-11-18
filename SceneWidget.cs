using SkiaSharp;

namespace someui;

public enum PlacementType
{
    Fixed = 0,
    Auto = 1,
    Relative = 2
}

public struct PlacementValue(PlacementType type, float value = 0)
{
    public PlacementType Type = type;
    public float Value = value;
    
    public override string ToString() => $"{GetType()}(type={Type} value={Value})";

    public static PlacementValue Fixed(float value)
        => new(PlacementType.Fixed, value);

    public static PlacementValue Auto()
        => new(PlacementType.Auto);

    public static PlacementValue Relative(float value)
        => new(PlacementType.Relative, value);
}

public struct PlacementConfig
{
    public PlacementValue X;
    public PlacementValue Y;
    public PlacementValue Width;
    public PlacementValue Height;
    
    public override string ToString() => $"{GetType()}(x={X} y={Y} width={Width} height={Height})";
}

public struct Margins
{
    public float Left;
    public float Top;
    public float Right;
    public float Bottom;
}

public enum Direction
{
    Column = 0,
    Row = 1
}

public struct ChildrenPlacementConfig()
{
    public Direction ChildDirection = Direction.Column;
    public float Gap = 0;
    public Margins Margins = new Margins {};
    public bool Propagate = true;
}


// internal enum DebugElementType
// {
//     Text = 0,
//     Point = 1,
//     Line = 2
// }
// 
// 
// internal struct DebugElement(DebugElementType type, float x, float y, SKColor color, string text="", float x1 = 0, float y1 = 0)
// {
//     public DebugElementType Type = type;
//     public float X = x;
//     public float Y = y;
//     public SKColor Color = color;
//     
//     // Options
//     
//     // Only for Text type
//     public string Text = text; 
//     
//     // Only for Line type
//     public float X1 = x1;
//     public float Y1 = y1;
// }

public class SceneWidget : BaseWidget
{
    public SceneWidget(BaseWidget? parent) : base(parent)
    {
        if (RealWindow != null)
        {
            OnResize.Add(RealWindow.InvalidateScene);
        }
    }
    
    public PlacementConfig PlacementConfig = new()
    {
        X = PlacementValue.Fixed(0),
        Y = PlacementValue.Fixed(0),
        Width = PlacementValue.Auto(),
        Height = PlacementValue.Auto()
    };
    
    public ChildrenPlacementConfig ChildrenPlacementConfig = new()
    {
        ChildDirection = Direction.Column,
        Gap = 2f,
        Margins = new Margins() { Left = 2f, Top = 2f}
    };
    public IEnumerable<SceneWidget> SceneChildren
        => Children.Cast<SceneWidget>();

    public void Place(PlacementConfig config)
    {
        PlacementConfig = config;
        RealWindow?.InvalidateScene();
    }

    protected override bool CanAddChild(BaseWidget child)
        => child is SceneWidget;

    private (float, float, float, float, float) ResolvePlace(SceneWidget child, float auto)
    {
        var childrenConfig = ChildrenPlacementConfig;
        
        var availableWidth = ActualWidth - childrenConfig.Margins.Left - childrenConfig.Margins.Right;
        var availableHeight = ActualHeight - childrenConfig.Margins.Top - childrenConfig.Margins.Bottom;
        
        var placement = child.PlacementConfig;
        float x = 0;
        float y = 0;
        float width = 0;
        float height = 0;

        switch (placement.Width.Type)
        {
            case PlacementType.Fixed:
                width = placement.Width.Value;
                break;
            case PlacementType.Relative:
                width = availableWidth * placement.Width.Value;
                break;
            case PlacementType.Auto:
            default:
                width = child.RequiredWidth;
                break;
        }
        
        switch (placement.Height.Type)
        {
            case PlacementType.Fixed:
                height = placement.Height.Value;
                break;
            case PlacementType.Relative:
                height = availableHeight * placement.Height.Value;
                break;
            case PlacementType.Auto:
            default:
                height = child.RequiredHeight;
                break;
        }

        switch (placement.X.Type)
        {
            case PlacementType.Fixed:
                x = placement.X.Value;
                break;
            case PlacementType.Relative:
                x = availableWidth * placement.X.Value;
                break;
            case PlacementType.Auto:
            default:
            {
                if (placement.X.Type == PlacementType.Auto &&
                    ChildrenPlacementConfig.ChildDirection == Direction.Row)
                {
                    x = auto;
                    auto += width + ChildrenPlacementConfig.Gap;
                }
                break;
            }
        }

        x += childrenConfig.Margins.Left;
        
        switch (placement.Y.Type)
        {
            case PlacementType.Fixed:
                y = placement.Y.Value;
                break;
            case PlacementType.Relative:
                y = availableHeight * placement.Y.Value;
                break;
            case PlacementType.Auto:
            default:
            {
                if (placement.Y.Type == PlacementType.Auto &&
                    ChildrenPlacementConfig.ChildDirection == Direction.Column)
                {
                    y = auto;
                    auto += height + ChildrenPlacementConfig.Gap;
                }
                break;
            }
        }
        
        y += childrenConfig.Margins.Top;

        return (x, y, width, height, auto);
    }

    public virtual void Measure()
    {
        float childrenTotalWidth = 0;
        float childrenTotalHeight = 0;
        
        foreach (var child in SceneChildren)
        {
            if (ChildrenPlacementConfig.Propagate)
            {
                child.Measure();
            }
            
            if (ChildrenPlacementConfig.ChildDirection == Direction.Column)
            {
                childrenTotalHeight += child.RequiredHeight + ChildrenPlacementConfig.Gap;
                childrenTotalWidth = Math.Max(childrenTotalWidth, child.RequiredWidth);
            }
            else
            {
                childrenTotalWidth += child.RequiredWidth + ChildrenPlacementConfig.Gap;
                childrenTotalHeight = Math.Max(childrenTotalHeight, child.RequiredHeight);
            }
        }
        
        if (ChildrenPlacementConfig.Propagate && Children.Count > 0)
        {
            RequiredWidth = childrenTotalWidth + ChildrenPlacementConfig.Margins.Left + ChildrenPlacementConfig.Margins.Right;
            RequiredHeight = childrenTotalHeight + ChildrenPlacementConfig.Margins.Top + ChildrenPlacementConfig.Margins.Bottom;   
        }
    }
    
    protected void RebuildScene()
    {
        if (ChildrenPlacementConfig.Propagate)
        {
            Measure();
        }

        float auto = 0;
        
        foreach (var child in SceneChildren)
        {
            var tuple = ResolvePlace(child, auto);
            auto = tuple.Item5;
            
            child.SetActualSizes(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
            
            child.RebuildScene();
        }
    }
}