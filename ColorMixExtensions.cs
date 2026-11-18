using System;
using SkiaSharp;

public static class ColorMixExtensions
{
    /// <summary>
    /// Смешивает два цвета SkiaSharp в стиле CSS color-mix.
    /// </summary>
    /// <param name="color1">Первый цвет</param>
    /// <param name="percent1">Процент первого цвета (от 0 до 100)</param>
    /// <param name="color2">Второй цвет</param>
    /// <param name="useLinearSpace">Если true, смешивание происходит в линейном пространстве (гамма-коррекция)</param>
    public static SKColor ColorMix(this SKColor color1, float percent1, SKColor color2, bool useLinearSpace = false)
    {
        // Нормализуем проценты (например, color-mix(in srgb, red 30%, blue))
        percent1 = Math.Clamp(percent1, 0f, 100f);
        float p1 = percent1 / 100f;
        float p2 = 1f - p1;

        if (useLinearSpace)
        {
            // Переводим в линейное пространство для честного смешивания яркости (gamma 2.2 приближенно)
            float r1 = GammaToLinear(color1.Red);
            float g1 = GammaToLinear(color1.Green);
            float b1 = GammaToLinear(color1.Blue);
            float a1 = color1.Alpha / 255f;

            float r2 = GammaToLinear(color2.Red);
            float g2 = GammaToLinear(color2.Green);
            float b2 = GammaToLinear(color2.Blue);
            float a2 = color2.Alpha / 255f;

            float rMix = LinearToGamma(r1 * p1 + r2 * p2);
            float gMix = LinearToGamma(g1 * p1 + g2 * p2);
            float bMix = LinearToGamma(b1 * p1 + b2 * p2);
            float aMix = (a1 * p1 + a2 * p2) * 255f;

            return new SKColor(
                (byte)Math.Clamp(rMix, 0, 255),
                (byte)Math.Clamp(gMix, 0, 255),
                (byte)Math.Clamp(bMix, 0, 255),
                (byte)Math.Clamp(aMix, 0, 255)
            );
        }
        else
        {
            // Простое классическое линейное смешивание (как в sRGB без учета гаммы)
            byte r = (byte)(color1.Red * p1 + color2.Red * p2);
            byte g = (byte)(color1.Green * p1 + color2.Green * p2);
            byte b = (byte)(color1.Blue * p1 + color2.Blue * p2);
            byte a = (byte)(color1.Alpha * p1 + color2.Alpha * p2);

            return new SKColor(r, g, b, a);
        }
    }

    // Вспомогательные методы для конвертации гаммы sRGB <-> Linear
    private static float GammaToLinear(byte val)
    {
        float v = val / 255f;
        return v <= 0.04045f ? v / 12.92f : (float)Math.Pow((v + 0.055f) / 1.055f, 2.4f);
    }

    private static float LinearToGamma(float val)
    {
        val = Math.Clamp(val, 0f, 1f);
        float v = val <= 0.003308f ? val * 12.92f : 1.055f * (float)Math.Pow(val, 1.0f / 2.4f) - 0.055f;
        return v * 255f;
    }
}