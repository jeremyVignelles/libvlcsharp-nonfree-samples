// Sample created by Jérémy VIGNELLES
// You can copy, paste, modify, use this file in any of your projects as long as you have supported my work through github sponsors (monthly or one-time payment) : https://github.com/sponsors/jeremyVignelles
// After payment, the file is considered yours and no copyright notice is required (though it would be appreciated).
// The file is provided as-is without any guarantee or support, and you still need to comply to the licenses of the dependencies of this file.

using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO.Pipelines;
using System.Numerics;
using Brushes = SixLabors.ImageSharp.Drawing.Processing.Brushes;
using Color = SixLabors.ImageSharp.Color;
using PointF = SixLabors.ImageSharp.PointF;

namespace ImageSharpMjpegInput;

internal static class Producer
{
    public static async Task Run(PipeWriter writer, CancellationToken token)
    {
        var increment = TimeSpan.FromMilliseconds(100);// Produce 10 FPS

        using var image = new Image<Rgba32>(800, 600);
        var fontCollection = new FontCollection();
        var family = fontCollection.Install("OpenSans-Regular.ttf");
        var font = family.CreateFont(40);
        var textBrush = Brushes.Solid(Color.White);
        var colorSpaceConverter = new ColorSpaceConverter();

        var textPosition = new PointF(400, 300);
        var textOptions = new DrawingOptions
        {
            TextOptions =
            {
                HorizontalAlignment = SixLabors.Fonts.HorizontalAlignment.Center,
                VerticalAlignment = SixLabors.Fonts.VerticalAlignment.Center
            }
        };

        var date = DateTime.Now;
        var colorStep = 0;
        using var jpegOutputMemoryStream = new MemoryStream();
        while (!token.IsCancellationRequested)
        {
            // Draw the image
            var fillBrush = Brushes.Solid(GetColor(colorSpaceConverter, colorStep));
            var text = date.ToString("o");
            image.Mutate(x =>
            {
                x.Fill(fillBrush);
                x.DrawText(textOptions, text, font, textBrush, textPosition);
            });

            await image.SaveAsJpegAsync(jpegOutputMemoryStream);
            var length = jpegOutputMemoryStream.Position;
            var memory = writer.GetMemory((int)length);
            jpegOutputMemoryStream.Position = 0;

            // Wait for the right time to present the frame
            var waitTime = date - DateTime.Now;
            if (waitTime.TotalMilliseconds > 0)
            {
                await Task.Delay(waitTime);
            }

            // Make the frame available to the reader
            writer.Advance(jpegOutputMemoryStream.Read(memory.Span));
            var flushResult = await writer.FlushAsync(token);

            jpegOutputMemoryStream.SetLength(0);

            if (flushResult.IsCompleted || flushResult.IsCanceled)
            {
                break;
            }

            date += increment;
            colorStep = (colorStep + 1) % 360;
        }

        await writer.CompleteAsync();
    }

    /// <summary>
    /// Changes the hue of the background color at each frame
    /// </summary>
    /// <param name="converter">The color converter</param>
    /// <param name="colorStep">The color step, between 0 and 360</param>
    /// <returns>The background color of this step</returns>
    private static Color GetColor(ColorSpaceConverter converter, int colorStep)
    {
        var hsl = new Hsl(new Vector3(colorStep, 0.5f, 0.5f));
        var rgb = converter.ToRgb(hsl);
        return new Color((Rgba32)rgb);
    }
}
