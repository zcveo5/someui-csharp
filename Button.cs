using SkiaSharp;

namespace someui;


public class Button : Label
{
    private readonly SKColor _backgroundIdleColor;
    private readonly SKColor _backgroundHoverColor;
    private readonly SKColor _backgroundHeldColor;

    private readonly SKColor _borderColor;
    private readonly SKPaint _borderPaint;
    
    public Button(BaseWidget parent, string text, Action command, string fontFamily = "Inter", int fontSize = 14,
        SKTypeface? typeface = null,
        SKFont? font = null, 
        SKColor? textColor = null, 
        SKColor? backgroundIdleColor = null, 
        SKColor? borderColor = null,
        SKColor? backgroundHoverColor = null, 
        SKColor? backgroundHeldColor = null) : 
        base(parent, text, fontFamily, fontSize, typeface, font, textColor, backgroundIdleColor)
    {
        _borderColor = borderColor ?? textColor ?? SKColors.White;
        _borderPaint = new SKPaint() { Color = _borderColor, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true, IsDither = true};
        
        _backgroundIdleColor = backgroundIdleColor ?? SKColors.Black;
        _backgroundHoverColor = backgroundHoverColor ?? _backgroundIdleColor.ColorMix(80f, SKColors.White);
        _backgroundHeldColor = backgroundHeldColor ?? _backgroundIdleColor.ColorMix(80f, SKColors.Black);
        
        MouseEvents.OnLeftButtonRelease.Add((_, _) => {command(); SetColors();});
        MouseEvents.OnLeftButtonPress.Add((_, _) => {SetColors();});
        
        MouseEvents.OnHoverEnter.Add((_, _) => {SetColors();});
        MouseEvents.OnHoverExit.Add((_, _) => {SetColors();});
        
        SetColors();
    }

    private void SetColors()
    {
        if (HeldNow)
            SetBackgroundColor(_backgroundHeldColor);
        else if (HoveredNow)
             SetBackgroundColor(_backgroundHoverColor);
        else
            SetBackgroundColor(_backgroundIdleColor);
    }

    protected override void DrawSelfBackdrop(SKCanvas ctx)
    {
        var targetRect = new SKRect(0, 0, (float)Math.Round(ActualWidth), (float)Math.Round(ActualHeight));
        ctx.DrawRoundRect(targetRect, 5f, 5f, BackgroundPaint);
        ctx.DrawRoundRect(targetRect, 5f, 5f, _borderPaint);
    }
}