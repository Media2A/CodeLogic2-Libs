using System;
using System.IO;
using ImageMagick;

namespace CL.Core.Utilities
{
    public partial class CLU_Imaging
    {
        public enum ImageFormat
        {
            Jpeg,
            Png,
            Webp,
            Bmp,
            Gif,
        }

        /// <summary>
        /// Checks if the image dimensions are within the specified maximum width and height.
        /// </summary>
        /// <param name="imageStream">The input stream containing the image data.</param>
        /// <param name="maxWidth">   The maximum allowed width.</param>
        /// <param name="maxHeight">  The maximum allowed height.</param>
        /// <returns>True if the dimensions are valid, false otherwise.</returns>
        public static bool IsValidSize(Stream imageStream, int maxWidth, int maxHeight)
        {
            using var image = new MagickImage(imageStream);

            if (image == null)
            {
                Console.WriteLine("Invalid image format.");
                return false;
            }

            if (image.Width > (uint)maxWidth || image.Height > (uint)maxHeight)
            {
                Console.WriteLine("Image dimensions exceed the allowed maximum.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the image has a 1:1 aspect ratio.
        /// </summary>
        /// <param name="imageStream">The input stream containing the image data.</param>
        /// <returns>True if the aspect ratio is 1:1, false otherwise.</returns>
        public static bool IsValidAspectRatio(Stream imageStream)
        {
            using var image = new MagickImage(imageStream);

            if (image == null)
            {
                Console.WriteLine("Invalid image format.");
                return false;
            }

            if (image.Width != image.Height)
            {
                Console.WriteLine("Image is not 1:1 ratio.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Converts an image to the specified format and returns the resulting data as a memory stream.
        /// </summary>
        /// <param name="imageStream">The input stream containing the image data.</param>
        /// <param name="format">     The desired output image format.</param>
        /// <returns>A memory stream containing the converted image data.</returns>
        public static MemoryStream ConvertImage(Stream imageStream, ImageFormat format)
        {
            using var image = new MagickImage(imageStream);

            if (image == null)
            {
                throw new InvalidOperationException("Invalid image format.");
            }

            MagickFormat magickFormat = format switch
            {
                ImageFormat.Jpeg => MagickFormat.Jpeg,
                ImageFormat.Png => MagickFormat.Png,
                ImageFormat.Webp => MagickFormat.WebP,
                ImageFormat.Bmp => MagickFormat.Bmp,
                ImageFormat.Gif => MagickFormat.Gif,
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported image format")
            };

            image.Format = magickFormat;

            var memoryStream = new MemoryStream();
            image.Write(memoryStream);
            memoryStream.Position = 0;

            return memoryStream;
        }

        /// <summary>
        /// Resizes an image to the specified width and height. Optionally, crops the image from the
        /// center if the aspect ratio does not match.
        /// </summary>
        /// <param name="imageStream"> The input stream containing the image data.</param>
        /// <param name="targetWidth"> The target width.</param>
        /// <param name="targetHeight">The target height.</param>
        /// <param name="allowCrop">   
        /// Whether to crop the image from the center if it doesn't fit. Default is true.
        /// </param>
        /// <returns>A memory stream containing the resized image data.</returns>
        public static MemoryStream ResizeImage(Stream imageStream, int targetWidth, int targetHeight, bool allowCrop = true)
        {
            using var image = new MagickImage(imageStream);

            if (image == null)
            {
                throw new InvalidOperationException("Invalid image format.");
            }

            if (allowCrop)
            {
                // Calculate the crop area to center the image
                var cropWidth = (uint)Math.Min(image.Width, targetWidth);
                var cropHeight = (uint)Math.Min(image.Height, targetHeight);

                int offsetX = (int)((image.Width - cropWidth) / 2);
                int offsetY = (int)((image.Height - cropHeight) / 2);

                // Perform the crop operation
                image.Crop(new MagickGeometry(offsetX, offsetY, cropWidth, cropHeight));
            }

            // Resize the image to the target dimensions
            image.Resize((uint)targetWidth, (uint)targetHeight);

            // Save the resized image to a MemoryStream
            var memoryStream = new MemoryStream();
            image.Write(memoryStream);
            memoryStream.Position = 0;

            return memoryStream;
        }
    }
}