/*
 * Copyright 2016-2022 by Starkku
 * This file is part of BatchTMPConverter, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see LICENSE.txt.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using BatchTMPConverter.Utility;

namespace BatchTMPConverter.Logic
{
    public class Palette
    {
        public string FilenameInput { get; private set; }
        public bool Loaded { get; private set; }

        private readonly List<PaletteColor> paletteColors = new List<PaletteColor>(new PaletteColor[256]);
        private Dictionary<PaletteColor, int> colorMatchLookup = new Dictionary<PaletteColor, int>();

        public Palette(string filenameInput)
        {
            FilenameInput = filenameInput;
            Load();
        }

        public void Load()
        {
            if (string.IsNullOrEmpty(FilenameInput))
            {
                Logger.Error("No valid input filename given - could not load palette file.");
                return;
            }

            FileStream fs = null;

            try
            {
                fs = new FileStream(FilenameInput, FileMode.Open);
            }
            catch (Exception e)
            {
                Logger.Error("Could not open palette file '" + FilenameInput + "'. Error message: " + e.Message);
                fs.Close();
                return;
            }

            if (fs.Length != 768)
            {
                Logger.Error("File '" + FilenameInput + "' is not a proper palette file.");
                return;
            }

            byte[] b = new byte[768];
            fs.Read(b, 0, b.Length);
            fs.Close();
            int j = 0;

            for (int i = 0; i < b.Length; i += 3)
            {
                paletteColors[j++] = new PaletteColor(j - 1, b[i] * 4, b[i + 1] * 4, b[i + 2] * 4);
            }

            Loaded = true;
        }

        public PaletteColor GetColor(int index)
        {
            if (index < 0 || index > 255)
                return new PaletteColor(-1);

            return paletteColors[index];
        }

        public PaletteColor MatchColor(int red, int green, int blue, bool useCieDE2000 = false)
        {
            double closestDeltaValue = double.MaxValue;
            double deltaValue;
            PaletteColor color = new PaletteColor(-1);
            PaletteColor originalColor = new PaletteColor(-1, red, green, blue);

            if (colorMatchLookup.TryGetValue(originalColor, out int index))
                return paletteColors[index];

            for (int i = 0; i < paletteColors.Count; i++)
            {
                if (useCieDE2000)
                    deltaValue = CompareColorsCIEDE2000(originalColor, paletteColors[i]);
                else
                    deltaValue = CompareColorsEuclidean(originalColor, paletteColors[i]);

                if (deltaValue < closestDeltaValue)
                {
                    closestDeltaValue = deltaValue;
                    color = paletteColors[i];
                }
            }

            colorMatchLookup.Add(originalColor, color.Index);

            return color;
        }

        private static double CompareColorsCIEDE2000(PaletteColor color1, PaletteColor color2)
        {
            return ColorUtilities.CompareDE2000(Color.FromArgb(color1.Red, color1.Green, color1.Blue), Color.FromArgb(color2.Red, color2.Green, color2.Blue));
        }

        private static double CompareColorsEuclidean(PaletteColor color1, PaletteColor color2)
        {
            // Formula from https://www.compuphase.com/cmetric.htm
            long rmean = (color1.Red + color2.Red) / 2;
            long r = color1.Red - color2.Red;
            long g = color1.Green - color2.Green;
            long b = color1.Blue - color2.Blue;

            return Math.Sqrt((((512 + rmean) * r * r) >> 8) + 4 * g * g + (((767 - rmean) * b * b) >> 8));
        }

        public PaletteColor[] GetPaletteColors(byte[] indexes)
        {
            PaletteColor[] palcolors = new PaletteColor[indexes.Length];
            int c = 0;

            foreach (byte b in indexes)
            {
                palcolors[c++] = GetColor(b);
            }

            return palcolors;
        }
    }

    public struct PaletteColor
    {
        public byte Index { get; set; }
        public byte Red { get; set; }
        public byte Green { get; set; }
        public byte Blue { get; set; }

        public PaletteColor(byte index, byte red, byte green, byte blue)
        {
            Index = index;
            Red = red;
            Green = green;
            Blue = blue;
        }

        public PaletteColor(int index, int red, int green, int blue)
        {
            Index = (byte)index;
            Red = (byte)red;
            Green = (byte)green;
            Blue = (byte)blue;
        }

        public PaletteColor(byte index)
        {
            Index = index;
            Red = 0;
            Green = 0;
            Blue = 0;
        }

        public PaletteColor(int index)
        {
            Index = (byte)index;
            Red = 0;
            Green = 0;
            Blue = 0;
        }
    }
}
