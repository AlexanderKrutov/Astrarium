﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Planetarium.Renderers
{
    /// <summary>
    /// This class is responsible for downloading latest solar images from web URL. 
    /// </summary>
    public class SolarTextureDownloader
    {
        public static readonly string SunImagesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ADK", "SunImages");

        public SolarTextureDownloader()
        {
            if (!Directory.Exists(SunImagesPath))
            {
                try
                {
                    Directory.CreateDirectory(SunImagesPath);
                }
                catch
                {
                    // TODO: log
                }
            }
        }

        public Image Download(string url)
        {
            string imageFile = Path.Combine(SunImagesPath, Path.GetFileName(url));

            try
            {
                if (!File.Exists(imageFile))
                {
                    // Download latest Solar image from provided URL
                    using (var client = new WebClient())
                    {
                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol =
                            SecurityProtocolType.Tls |
                            SecurityProtocolType.Tls11 |
                            SecurityProtocolType.Tls12 |
                            SecurityProtocolType.Ssl3;
                        client.DownloadFile(new Uri(url), imageFile);
                    }
                }

                // Prepare resulting circle image with transparent background
                using (var image = (Bitmap)Image.FromFile(imageFile))
                {
                    // default value of crop factor
                    float cropFactor = 0.93f;

                    // find first non-black pixel position
                    for (int x = 0; x < image.Width / 2; x++)
                    {
                        var color = image.GetPixel(x, image.Height / 2);
                        if (color.A > 20)
                        {
                            int grayscaled = (color.R + color.G + color.B) / 3;
                            if (grayscaled > 20)
                            {
                                cropFactor = 1 - 2 * (float)(x + 2) / image.Width;
                                break;
                            }
                        }
                    }

                    Image result = new Bitmap(
                        (int)(image.Width * cropFactor),
                        (int)(image.Height * cropFactor),
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    using (var g = Graphics.FromImage(result))
                    {
                        g.Clear(Color.Transparent);
                        g.SmoothingMode = SmoothingMode.AntiAlias;

                        using (var crop = new GraphicsPath())
                        {
                            g.TranslateTransform(
                                image.Width * cropFactor / 2,
                                image.Height * cropFactor / 2);

                            float cropMargin = 1e-3f;

                            crop.AddEllipse(
                                -image.Width * cropFactor / 2 * (1 - cropMargin),
                                -image.Height * cropFactor / 2 * (1 - cropMargin),
                                image.Width * cropFactor * (1 - cropMargin),
                                image.Height * cropFactor * (1 - cropMargin));

                            g.SetClip(crop);

                            g.DrawImage(image, -image.Width / 2, -image.Height / 2, image.Width, image.Height);
                        }
                    }

                    return result;
                }
            }
            catch
            {
                // TODO: log
                return null;
            }
        }
    }
}
