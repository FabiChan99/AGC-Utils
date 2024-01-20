#region

using SkiaSharp;

#endregion

namespace AGC_Management.Utils;

public sealed class ImageUtils
{
    public static SKBitmap CreateProgressBar(int width, int height, float percentage, string text)
    {
        var bmp = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.White);

        var backgroundPaint = new SKPaint
        {
            Color = SKColors.Gray
        };
        canvas.DrawRect(0, 0, width, height, backgroundPaint);

        var progressPaint = new SKPaint
        {
            Color = SKColors.BlueViolet
        };
        canvas.DrawRect(0, 0, width * percentage, height, progressPaint);

        var textPaint = new SKPaint
        {
            Color = SKColors.White,
            FilterQuality = SKFilterQuality.High,
            TextSize = 18,
            TextAlign = SKTextAlign.Center,
            IsAntialias = true
        };
        var textBounds = new SKRect();
        using var typeface = SKTypeface.FromFamilyName("Verdana");
        textPaint.Typeface = typeface;
        textPaint.MeasureText(text, ref textBounds);
        canvas.DrawText(text, width / 2, (height + textBounds.Height) / 2, textPaint);

        return bmp;
    }
}