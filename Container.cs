using SkiaSharp;

namespace someui;

public class Container(BaseWidget? parent, SKColor color) : SceneWidget(parent)
{
    private SKPaint _rectPaint = new() { Color = color};

    public override void DrawSelf(SKCanvas ctx)
    {
        base.DrawSelf(ctx);
        ctx.DrawRoundRect(0, 0, ActualWidth, ActualHeight, 5f, 5f, _rectPaint);
    }
}