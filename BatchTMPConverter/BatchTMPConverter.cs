using System;
using System.Collections.Generic;
using BatchTMPConverter.Logic;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using BatchTMPConverter.Utility;
using System.Diagnostics;

namespace BatchTMPConverter
{
    public class BatchTMPConverter
    {
        private readonly string[] tmpExtensions = new string[] { ".tem", ".sno", ".urb", ".des", ".ubn", ".lun" };
        private readonly Palette palette;
        private readonly List<string> files = new List<string>();
        private readonly bool replaceRadarColors = false;
        private readonly double radarColorMultiplier = 1.0;
        private readonly bool extraDataBGOverride = false;
        private readonly bool accurateColorMatching = false;
        private FileLog processedFilesLog = null;
        private readonly bool supressBackups = false;
        private List<Tuple<string, string>> preprocessCommands = new List<Tuple<string, string>>();

        public BatchTMPConverter(string filenames, string palette, string customExtensions, bool replaceRadarColors, double radarColorMultiplier, bool extraDataBGOverride,
            bool accurateColorMatching, string preprocessCommands, string fileLogFilename, bool supressBackups)
        {
            if (!string.IsNullOrEmpty(fileLogFilename))
                processedFilesLog = new FileLog(fileLogFilename);

            AddFiles(filenames);
            this.palette = new Palette(palette);
            string[] cExtensions = null;

            if (!string.IsNullOrWhiteSpace(customExtensions))
                cExtensions = customExtensions.Split(',');
            if (cExtensions != null && cExtensions.Length < 1)
                tmpExtensions = cExtensions;

            this.replaceRadarColors = replaceRadarColors;
            this.radarColorMultiplier = radarColorMultiplier;
            this.extraDataBGOverride = extraDataBGOverride;
            this.accurateColorMatching = accurateColorMatching;
            this.supressBackups = supressBackups;
            ParsePreprocessCommands(preprocessCommands);
        }

        private void AddFiles(string filenames)
        {
            if (string.IsNullOrEmpty(filenames))
                return;

            string[] fns1 = filenames.Split(',');
            foreach (string filename in fns1)
            {
                if (Directory.Exists(filename))
                {
                    string[] fns2 = Directory.GetFiles(filename);
                    foreach (string f in fns2)
                    {
                        AddFile(f);
                    }
                }
                else if (File.Exists(filename))
                {
                    AddFile(filename);
                }
            }
        }

        private void AddFile(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return;

            string extension = Path.GetExtension(filename);
            var index = Array.FindIndex(tmpExtensions, x => x == extension);

            if (index >= 0)
                files.Add(filename);
        }

        private void ParsePreprocessCommands(string commandString)
        {
            if (string.IsNullOrEmpty(commandString))
                return;

            string[] commands = commandString.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string command in commands)
            {
                string[] parts = command.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 1)
                    continue;

                string executable = parts[0];
                string arguments = parts.Length > 1 ? parts[1] : string.Empty;

                preprocessCommands.Add(new Tuple<string, string>(executable, arguments));
                Logger.Info("Added preprocess command: " + executable + " " + arguments);
            }
        }

        public void ConvertImagesToTMP()
        {
            if (!palette.Loaded)
            {
                Logger.Error("No palette file has been loaded - cannot convert image data to templates.");
                return;
            }

            foreach (string filename in files)
            {
                Tmp tmp = new Tmp(filename);

                if (!tmp.Initialized)
                    continue;

                string imageFilename = Path.ChangeExtension(tmp.FilenameInput, ".png");

                if (!File.Exists(imageFilename))
                {
                    Logger.Warn("Image '" + imageFilename + "' does not exist. Skip converting image data to template.");
                    continue;
                }
                else if (processedFilesLog != null && !processedFilesLog.HasFileBeenModified(imageFilename))
                {
                    Logger.Warn("Image '" + imageFilename + "' has not been modified. Skip converting image data to template.");
                    continue;
                }
                else
                {
                    string actualImageFilename = PreprocessImage(imageFilename);
                    Bitmap bitmap = new Bitmap(actualImageFilename);
                    Rectangle bitmapRectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                    Rectangle tmpRectangle = tmp.GetRectangle();
                    int X = tmpRectangle.Width - tmpRectangle.Left;
                    int Y = tmpRectangle.Height - tmpRectangle.Top;

                    if (X != bitmapRectangle.Width || Y != bitmapRectangle.Height)
                    {
                        Logger.Warn("Image '" + imageFilename + "' does not match template size. Skip converting image data to template.");
                        continue;
                    }

                    BitmapData bitmapData = bitmap.LockBits(bitmapRectangle, ImageLockMode.ReadWrite, bitmap.PixelFormat);
                    IntPtr ptr = bitmapData.Scan0;
                    int ColorDepth = GetBitmapColorDepth(bitmap);
                    int bytes = Math.Abs(bitmapData.Stride) * bitmap.Height;
                    byte[] rgbValues = new byte[bytes];
                    Marshal.Copy(ptr, rgbValues, 0, bytes);

                    if (ColorDepth == 24)
                        rgbValues = TrimRGBValues(rgbValues, bitmapData.Stride, bitmap.Height);

                    PixelFormat pixelFormat = bitmap.PixelFormat;
                    bitmap.UnlockBits(bitmapData);
                    bitmap.Dispose();

                    if (ColorDepth == 8)
                        tmp.ConvertImageData(palette.GetPaletteColors(rgbValues), tmpRectangle, replaceRadarColors, radarColorMultiplier, palette.GetColor(0), extraDataBGOverride);
                    else if (ColorDepth == 32)
                        tmp.ConvertImageData(GetPalettedImageData(ColorDepth, rgbValues), tmpRectangle, replaceRadarColors, radarColorMultiplier, palette.GetColor(0), extraDataBGOverride);
                    else if (ColorDepth == 24)
                        tmp.ConvertImageData(GetPalettedImageData(ColorDepth, rgbValues), tmpRectangle, replaceRadarColors, radarColorMultiplier, palette.GetColor(0), extraDataBGOverride);
                    else
                    {
                        Logger.Warn("Bitmap format '" + pixelFormat.ToString() + "' is not supported. Skip converting image data to template.");

                        if (imageFilename != actualImageFilename)
                            File.Delete(actualImageFilename);

                        continue;
                    }

                    if (tmp.Save(supressBackups))
                        processedFilesLog?.UpdateOrAddFile(imageFilename);
                    
                    if (imageFilename != actualImageFilename)
                        File.Delete(actualImageFilename);
                }
            }
            processedFilesLog?.Save();
        }

        private string PreprocessImage(string imageFilename)
        {
            if (preprocessCommands.Count < 1)
                return imageFilename;

            string filename = imageFilename + ".preproc";
            File.Copy(imageFilename, filename, true);

            foreach (Tuple<string, string> command in preprocessCommands)
            {
                string executable = command.Item1;
                string arguments = command.Item2.Replace("$FILENAME", filename);
                Process process = new Process();
                process.StartInfo.FileName = executable;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.Start();
                process.WaitForExit();
            }

            return filename;
        }

        public void OutputTMPImageData()
        {
            if (!palette.Loaded)
            {
                Logger.Error("No palette file has been loaded - cannot convert templates to images.");
                return;
            }

            foreach (string filename in files)
            {
                if (processedFilesLog != null && !processedFilesLog.HasFileBeenModified(filename))
                {
                    Logger.Warn("Template file '" + filename + "' has not been modified. Skip converting template to image.");
                    continue;
                }

                Tmp tmp = new Tmp(filename);

                if (!tmp.Initialized)
                    continue;

                Rectangle tmpRectangle = tmp.GetRectangle();
                byte[] data = tmp.GetImageData(tmpRectangle);

                string imagefilename = Path.ChangeExtension(tmp.FilenameInput, ".png");
                Bitmap bitmap = new Bitmap(tmpRectangle.Width - tmpRectangle.Left, tmpRectangle.Height - tmpRectangle.Top, PixelFormat.Format24bppRgb);

                try
                {
                    SaveImageDataToBitmap(bitmap, data, palette);
                    bitmap.Save(imagefilename);
                }
                catch (Exception e)
                {
                    Logger.Error("Could not save template data to image. Error message: " + e.Message);
                    continue;
                }

                processedFilesLog?.UpdateOrAddFile(filename);
                Logger.Info(filename + " template data saved to image file " + imagefilename + ".");
            }
            processedFilesLog?.Save();
        }

        private byte[] TrimRGBValues(byte[] rgbValues, int X, int Y)
        {
            int rowlength = 3 * (X / 3);
            byte[] newvalues = new byte[rowlength * Y];

            for (int y = 0; y < Y; y++)
            {
                byte[] row = new byte[X];
                Array.Copy(rgbValues, y * X, row, 0, X);

                for (int x = 0; x < rowlength; x++)
                {
                    newvalues[(y * rowlength) + x] = row[x];
                }
            }

            return newvalues;
        }

        private int GetBitmapColorDepth(Bitmap bitmap)
        {
            switch (bitmap.PixelFormat)
            {
                case PixelFormat.Format8bppIndexed:
                    return 8;
                case PixelFormat.Format24bppRgb:
                    return 24;
                case PixelFormat.Format32bppArgb:
                    return 32;
                case PixelFormat.Format32bppPArgb:
                    return 32;
                case PixelFormat.Format32bppRgb:
                    return 32;
                default:
                    return -1;
            }
        }

        private PaletteColor[] GetPalettedImageData(int colorDepth, byte[] rgbValues)
        {
            int bytes = colorDepth / 8;
            int length = rgbValues.Length / bytes;
            PaletteColor[] palValues = new PaletteColor[length];
            int j = 0;

            for (int i = 0; i < length; i++)
            {
                palValues[i] = palette.MatchColor(rgbValues[j + 2], rgbValues[j + 1], rgbValues[j], accurateColorMatching);
                j += bytes;
            }

            return palValues;
        }

        private void SaveImageDataToBitmap(Bitmap bitmap, byte[] data, Palette palette)
        {
            int c = 0;

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    PaletteColor color = palette.GetColor(data[c++]);
                    bitmap.SetPixel(x, y, Color.FromArgb(color.Red, color.Green, color.Blue));
                }
            }
        }
    }
}
