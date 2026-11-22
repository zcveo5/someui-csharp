using SkiaSharp;

namespace someui;


public static class FontManager
{
    private static Dictionary<string, SKTypeface> _typefaceCache = new();

    public static SKTypeface GetFromFamilyName(string familyName)
    {
        if (_typefaceCache.TryGetValue(familyName, out var value))
            return value;
        var created = SKTypeface.FromFamilyName(familyName);
        if (created != null)
        {
            _typefaceCache[familyName] = created;
            return created;
        }
        return SKTypeface.Default;
    }
    
}

public struct Paddings(float left, float top, float right, float bottom)
{
    public float Left = left;
    public float Top = top;
    public float Right = right;
    public float Bottom = bottom;
}

public class Label : SceneWidget
{
    public string Text { get; private set; } = "";
    private SKTextBlob? _blob = null;
    private readonly SKTypeface _typeface;
    private readonly SKFont _font;
    private readonly SKPaint _textPaint;
    protected readonly SKPaint BackgroundPaint;
    private bool _dirtyBlob = true;

    public float TextPadding = 12f;

    private float _textHeight = 0;
    private float _textWidth  = 0;
    private float _textOffsetX = 0;
    private float _textOffsetY = 0;
    
    public Label(BaseWidget parent, string text, string fontFamily = "Inter", int fontSize = 14, SKTypeface? typeface = null, 
        SKFont? font = null, SKColor? textColor = null, SKColor? backgroundColor = null) : base(parent)
    {
        Debug = true;        
        _textPaint = new SKPaint {Color = textColor ?? SKColors.White};
        BackgroundPaint = new SKPaint { Color = backgroundColor ?? SKColors.Black };
        _typeface = typeface
                    ?? FontManager.GetFromFamilyName(fontFamily);

        _font = font ?? new SKFont(_typeface, fontSize) { Edging = SKFontEdging.SubpixelAntialias, Hinting = SKFontHinting.Normal, Subpixel = true, LinearMetrics = true};
        
        SetText(text);
    }

    public void SetText(string newText)
    {
        Text = newText;
        _dirtyBlob = true;
        RealWindow?.Invalidate();
    }
    
    public void SetTextColor(SKColor newColor)
    {
        _textPaint.Color = newColor;
        RealWindow?.Invalidate();
    }
    
    public void SetBackgroundColor(SKColor newColor)
    {
        BackgroundPaint.Color = newColor;
        RealWindow?.Invalidate();
    }

    private void RebuildBlob()
    {
        _dirtyBlob = false;

        _blob?.Dispose();
        _blob = SKTextBlob.Create(Text, _font);

        if (_blob == null)
            return;

        _font.MeasureText(Text, out SKRect bounds);

        _textWidth = bounds.Width;
        _textHeight = bounds.Height;

        var formattedWidth = _textWidth + TextPadding * 2;
        var formattedHeight = _textHeight + TextPadding;

        _textOffsetX = formattedWidth / 2 - _textWidth / 2;
        _textOffsetY = _textHeight + (formattedHeight / 2 - _textHeight / 2);

        SetSize(formattedWidth, formattedHeight);
    }

    protected virtual void DrawSelfBackdrop(SKCanvas ctx)
    {
        var targetRect = new SKRect(0, 0, ActualWidth, ActualHeight);
        ctx.DrawRect(targetRect, BackgroundPaint);
    }

    public override void DrawSelf(SKCanvas ctx)
    {
        if (_dirtyBlob) RebuildBlob();
        DrawSelfBackdrop(ctx);
        ctx.DrawText(_blob, _textOffsetX, _textOffsetY, _textPaint);
    }
}