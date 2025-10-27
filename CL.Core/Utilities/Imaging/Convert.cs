using System;
using System.IO;
using ImageMagick;

namespace CL.Core.Utilities
{
    public partial class CLU_Imaging
    {
        public static string ImageToBase64(string imagePath)
        {
            using (var image = new MagickImage(imagePath))
            {
                using (var memoryStream = new MemoryStream())
                {
                    image.Write(memoryStream);
                    return Convert.ToBase64String(memoryStream.ToArray());
                }
            }
        }

        public static void ResizeImage(string inputPath, string outputPath, int width, int height, bool maintainAspectRatio = true)
        {
            using (var image = new MagickImage(inputPath))
            {
                uint newWidth = (uint)width;
                uint newHeight = (uint)height;

                if (maintainAspectRatio)
                {
                    image.Resize(newWidth, newHeight);
                }
                else
                {
                    image.Scale(newWidth, newHeight);
                }

                image.Write(outputPath);
            }
        }

        public static void ConvertImageFormat(string inputPath, string outputPath, MagickFormat format)
        {
            using (var image = new MagickImage(inputPath))
            {
                image.Format = format;
                image.Write(outputPath);
            }
        }

        public static void CreateThumbnail(string inputPath, string outputPath, int thumbnailSize = 100)
        {
            using (var image = new MagickImage(inputPath))
            {
                uint newWidth, newHeight;
                if (image.Width > image.Height)
                {
                    newWidth = (uint)thumbnailSize;
                    newHeight = (uint)(image.Height * thumbnailSize / (float)image.Width);
                }
                else
                {
                    newHeight = (uint)thumbnailSize;
                    newWidth = (uint)(image.Width * thumbnailSize / (float)image.Height);
                }

                image.Resize(newWidth, newHeight);
                image.Write(outputPath);
            }
        }

        public static (int width, int height) GetImageDimensions(string imagePath)
        {
            using (var image = new MagickImage(imagePath))
            {
                // Convert uint to int explicitly
                int width = (int)image.Width;
                int height = (int)image.Height;

                return (width, height);
            }
        }
    }
}