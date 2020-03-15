using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Drawing;
using System.IO;

namespace PrimePartsPrices.Utils
{
    public static class ImageProcessor
    {
        /// <summary>
        /// Crops an image on the given area
        /// </summary>
        /// <param name="initialImage">The image to crop</param>
        /// <param name="cropArea">The area to crop</param>
        /// <returns>The cropped image</returns>
        public static Bitmap CropImage(Bitmap initialImage, Rectangle cropArea)
        {
            return initialImage.Clone(cropArea, initialImage.PixelFormat);
        }

        /// <summary>
        /// Inverts the colors from the image using the answer from https://stackoverflow.com/questions/33024881/invert-image-faster-in-c-sharp
        /// </summary>
        /// <param name="image">The image to have the colors inverted</param>
        /// <returns>The resulting image from inverting the colors of the given one</returns>
        public static Bitmap InvertColors(Bitmap image)
        {
            for (int y = 0; (y <= (image.Height - 1)); y++)
            {
                for (int x = 0; (x <= (image.Width - 1)); x++)
                {
                    Color inv = image.GetPixel(x, y);
                    inv = Color.FromArgb(255, (255 - inv.R), (255 - inv.G), (255 - inv.B));
                    image.SetPixel(x, y, inv);
                }
            }

            return image;
        }

        /// <summary>
        /// Sharpens the image using the answer from https://stackoverflow.com/questions/903632/sharpen-on-a-bitmap-using-c-sharp/1319999
        /// </summary>
        /// <param name="image">Then image to sharpen</param>
        /// <returns>The resulting image from sharpening the given one</returns>
        public static Bitmap Sharpen(Bitmap image)
        {
            Bitmap sharpenImage = new Bitmap(image.Width, image.Height);

            int filterWidth = 3;
            int filterHeight = 3;
            int w = image.Width;
            int h = image.Height;

            double[,] filter = new double[filterWidth, filterHeight];

            filter[0, 0] = filter[0, 1] = filter[0, 2] = filter[1, 0] = filter[1, 2] = filter[2, 0] = filter[2, 1] = filter[2, 2] = -1;
            filter[1, 1] = 9;

            double factor = 1.0;
            double bias = 0.0;

            Color[,] result = new Color[image.Width, image.Height];

            for (int x = 0; x < w; ++x)
            {
                for (int y = 0; y < h; ++y)
                {
                    double red = 0.0, green = 0.0, blue = 0.0;

                    for (int filterX = 0; filterX < filterWidth; filterX++)
                    {
                        for (int filterY = 0; filterY < filterHeight; filterY++)
                        {
                            int imageX = (x - filterWidth / 2 + filterX + w) % w;
                            int imageY = (y - filterHeight / 2 + filterY + h) % h;

                            Color imageColor = image.GetPixel(imageX, imageY);

                            red += imageColor.R * filter[filterX, filterY];
                            green += imageColor.G * filter[filterX, filterY];
                            blue += imageColor.B * filter[filterX, filterY];
                        }
                        int r = Math.Min(Math.Max((int)(factor * red + bias), 0), 255);
                        int g = Math.Min(Math.Max((int)(factor * green + bias), 0), 255);
                        int b = Math.Min(Math.Max((int)(factor * blue + bias), 0), 255);

                        result[x, y] = Color.FromArgb(r, g, b);
                    }
                }
            }
            for (int i = 0; i < w; ++i)
            {
                for (int j = 0; j < h; ++j)
                {
                    sharpenImage.SetPixel(i, j, result[i, j]);
                }
            }
            return sharpenImage;
        }

        /// <summary>
        /// Processes the given image with custom settings for the prime part image
        /// </summary>
        /// <param name="image">The image to apply filter to</param>
        /// <returns>The resulted image after applying custom filters to the given one</returns>
        public static Bitmap PrimePartImageCustomProcess(Bitmap image)
        {
            string tempImagePath = Path.Combine(GeneralUtils.GetAssemblyPath(), "temp.tiff");
            image.Save(tempImagePath, System.Drawing.Imaging.ImageFormat.Tiff);

            Mat src_gray = new Mat(tempImagePath, ImreadModes.Grayscale);
            GeneralUtils.DeleteFile(tempImagePath);

            Cv2.AddWeighted(src_gray, 1.5, src_gray, -0.5, 0, src_gray);
            Cv2.Threshold(src_gray, src_gray, 150, 255, ThresholdTypes.Binary);

            return BitmapConverter.ToBitmap(src_gray);
        }
    }
}
