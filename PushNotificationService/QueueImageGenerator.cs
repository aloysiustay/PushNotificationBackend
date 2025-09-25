using SkiaSharp;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using System.Text;

namespace PushNotificationService
{
    public class QueueImageGenerator
    {
        public static byte[] GenerateQueueImage(string baseImagePath, int queueNumber)
        {
            // Load the base image
            using var inputStream = File.OpenRead(baseImagePath);
            using var baseBitmap = SKBitmap.Decode(inputStream);

            // Create a surface
            using var surface = SKSurface.Create(new SKImageInfo(baseBitmap.Width, baseBitmap.Height));
            var canvas = surface.Canvas;

            // Draw the base image
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(baseBitmap, 0, 0);

            // Setup paint for text color & antialiasing
            using var paint = new SKPaint
            {
                Color = SKColors.DarkBlue,
                IsAntialias = true,
            };
           
            // Setup font (size + typeface)
            using var typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
            using var font = new SKFont(typeface, size: 85);

            // Convert text to UTF-8 bytes for measuring
            var utf8Text = Encoding.UTF8.GetBytes(queueNumber.ToString());

            // Measure text bounds
            var bounds = new SKTextEncoding();
            float textWidth = font.MeasureText(utf8Text, bounds, paint);

            var metrics = font.Metrics;

            // Position text in the center
            float x = (baseBitmap.Width - textWidth) * 0.5f;
            float y = baseBitmap.Height * 0.5f - 105f;


            // Draw text
            canvas.DrawText(queueNumber.ToString(), x, y, font, paint);

            // Save to PNG byte array
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            return data.ToArray();
        }
    }
}
