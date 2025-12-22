using System;
using System.Drawing;
using System.Drawing.Imaging;

using Patagames.Ocr;
using Patagames.Ocr.Enums;


namespace Disambiguator
{
    /// <summary>
    /// Class used to convert a bounds on screen into text
    /// </summary>
    internal class BoundsToText
    {
        Rectangle _bounds = new Rectangle();

        internal BoundsToText(Rectangle bounds)
        {
            DisambiguatorExt.Debug("BoundsToText created with bounds: " + bounds.ToString());
            _bounds = bounds;
        }


        private Bitmap Snapshot(Rectangle bounds)
        {
            // Create a new Bitmap object with the specified width and height
            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

            // Create a Graphics object from the bitmap. This object acts as the drawing surface.
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                // Copy the image from the screen to the bitmap
                // Parameters:
                // 1. Source point (upper-left corner of the screen region)
                // 2. Destination point (upper-left corner of the bitmap, usually 0, 0)
                // 3. Size of the region to copy
                // 4. Pixel operation (SourceCopy is typical)
                graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            }

            DisambiguatorExt.Debug("BoundsToText snapshotted using bounds: " + bounds.ToString());
            return bitmap;
        }


        /// <summary>
        /// Actually convert the bounds on screen to a piece of text
        /// </summary>
        /// <returns></returns>
        internal string Convert()
        {
            //guard conditions
            if (_bounds.IsEmpty) { return string.Empty; }

            var text = string.Empty;

            // Ensure native Tesseract DLL is loaded from embedded resources
            DisambiguatorExt.Debug("BoundsToText loading Tesseract");
            NativeDllLoader.EnsureTesseractLoaded();
            DisambiguatorExt.Debug("BoundsToText loaded Tesseract");

            using (var api = OcrApi.Create())
            {
                DisambiguatorExt.Debug("BoundsToText created Tesseract api: " + api.Handle);

                api.Init(Languages.English);
                DisambiguatorExt.Debug("BoundsToText Tesseract Inited");

                var bitmap = Snapshot(_bounds);
                DisambiguatorExt.Debug("BoundsToText snapshotted");

                //bail early if we couldn't get a bitmap
                if (bitmap == null) return text;

                DisambiguatorExt.Debug("BoundsToText got bitmap");
                text = api.GetTextFromImage(bitmap) ?? string.Empty;
                DisambiguatorExt.Debug("BoundsToText resolve text as raw: " + text);

                text = text.Replace('\n', ' ');
                text = text.Replace('\r', ' ');
                text = text.Replace('\t', ' ');
                text = text.Replace("     ", " ");
                text = text.Replace("    ", " ");
                text = text.Replace("   ", " ");
                text = text.Replace("  ", " ");
                text = text.Trim();

                DisambiguatorExt.Debug("BoundsToText cleaned text: " + text);

                return text;
            }
        }
    }
}
