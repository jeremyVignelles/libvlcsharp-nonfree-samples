// Sample created by Jérémy VIGNELLES
// You can copy, paste, modify, use this file in any of your projects as long as you have supported my work through github sponsors (monthly or one-time payment) : https://github.com/sponsors/jeremyVignelles
// After payment, the file is considered yours and no copyright notice is required (though it would be appreciated).
// The file is provided as-is without any guarantee or support, and you still need to comply to the licenses of the dependencies of this file.

using System.IO.Pipelines;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using Brushes = SixLabors.ImageSharp.Drawing.Processing.Brushes;
using PointF = SixLabors.ImageSharp.PointF;
using SixLabors.Fonts;

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
        var fillColor = SystemColors.Highlight;
        var fillBrush = Brushes.Solid(SixLabors.ImageSharp.Color.FromRgb(fillColor.R, fillColor.G, fillColor.B));
        var textColor = SystemColors.HighlightText;
        var textBrush = Brushes.Solid(SixLabors.ImageSharp.Color.FromRgb(textColor.R, textColor.G, textColor.B));

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
        using var jpegOutputMemoryStream = new MemoryStream();
        while (!token.IsCancellationRequested)
        {
            // Draw the image
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
        }

        await writer.CompleteAsync();
    }
}
