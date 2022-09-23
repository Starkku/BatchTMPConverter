/*
 * Copyright 2016-2022 by Starkku
 * This file is part of BatchTMPConverter, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see LICENSE.txt.
 */

using System;
using System.Drawing;

namespace BatchTMPConverter.Logic
{
    /// <summary>
    /// Color space & conversion utilities, mostly pertaining CIE color spaces.
    /// Formulas come from http://www.easyrgb.com/en/math.php.
    /// </summary>
    public class ColorUtilities
    {
        public struct ColorLab
        {
            public double L;
            public double a;
            public double b;
        }

        public static void RGBToXYZ(Color color, out double X, out double Y, out double Z)
        {
            double R = color.R / (double)255;
            double G = color.G / (double)255;
            double B = color.B / (double)255;

            if (R > 0.04045) R = Math.Pow(((R + 0.055) / 1.055), 2.4);
            else R /= 12.92;
            if (G > 0.04045) G = Math.Pow(((G + 0.055) / 1.055), 2.4);
            else G /= 12.92;
            if (B > 0.04045) B = Math.Pow(((B + 0.055) / 1.055), 2.4);
            else B /= 12.92;

            R *= 100;
            G *= 100;
            B *= 100;

            X = R * 0.4124 + G * 0.3576 + B * 0.1805;
            Y = R * 0.2126 + G * 0.7152 + B * 0.0722;
            Z = R * 0.0193 + G * 0.1192 + B * 0.9505;
        }

        public static void XYZToLab(double X, double Y, double Z, out ColorLab colorLab)
        {
            // sRGB reference values.
            double var_X = X / 95.047;
            double var_Y = Y / 100.000;
            double var_Z = Z / 108.883;

            if (var_X > 0.008856) var_X = Math.Pow(var_X, 1 / (double)3);
            else var_X = (7.787 * var_X) + (16 / (double)116);
            if (var_Y > 0.008856) var_Y = Math.Pow(var_Y, 1 / (double)3);
            else var_Y = (7.787 * var_Y) + (16 / (double)116);
            if (var_Z > 0.008856) var_Z = Math.Pow(var_Z, 1 / (double)3);
            else var_Z = (7.787 * var_Z) + (16 / (double)116);

            colorLab = new ColorLab
            {
                L = (116 * var_Y) - 16,
                a = 500 * (var_X - var_Y),
                b = 200 * (var_Y - var_Z)
            };
        }

        public static void LabToXYZ(ColorLab colorLab, out double X, out double Y, out double Z)
        {
            double var_Y = (colorLab.L + 16) / 116;
            double var_X = colorLab.a / 500 + var_Y;
            double var_Z = var_Y - colorLab.b / 200;

            if (Math.Pow(var_Y, 3) > 0.008856) var_Y = Math.Pow(var_Y, 3);
            else var_Y = (var_Y - 16 / (double)116) / 7.787;
            if (Math.Pow(var_X, 3) > 0.008856) var_X = Math.Pow(var_X, 3);
            else var_X = (var_X - 16 / (double)116) / 7.787;
            if (Math.Pow(var_Z, 3) > 0.008856) var_Z = Math.Pow(var_Z, 3);
            else var_Z = (var_Z - 16 / (double)116) / 7.787;

            // sRGB reference values.
            X = 95.047 * var_X;
            Y = 100.000 * var_Y;
            Z = 108.883 * var_Z;
        }

        public static void XYZToRGB(double X, double Y, double Z, out Color color)
        {
            double var_X = X / 100;
            double var_Y = Y / 100;
            double var_Z = Z / 100;

            double var_R = var_X * 3.2406 + var_Y * -1.5372 + var_Z * -0.4986;
            double var_G = var_X * -0.9689 + var_Y * 1.8758 + var_Z * 0.0415;
            double var_B = var_X * 0.0557 + var_Y * -0.2040 + var_Z * 1.0570;

            if (var_R > 0.0031308) var_R = 1.055 * Math.Pow(var_R, 1 / (double)2.4) - 0.055;
            else var_R = 12.92 * var_R;
            if (var_G > 0.0031308) var_G = 1.055 * Math.Pow(var_G, 1 / (double)2.4) - 0.055;
            else var_G = 12.92 * var_G;
            if (var_B > 0.0031308) var_B = 1.055 * Math.Pow(var_B, 1 / (double)2.4) - 0.055;
            else var_B = 12.92 * var_B;

            color = Color.FromArgb((int)(var_R * 255), (int)(var_G * 255), (int)(var_B * 255));
        }

        public static void RGBToLab(Color color, out ColorLab colorLab)
        {
            RGBToXYZ(color, out double X, out double Y, out double Z);
            XYZToLab(X, Y, Z, out colorLab);
        }

        public static void LabToRGB(ColorLab colorLab, out Color color)
        {
            LabToXYZ(colorLab, out double X, out double Y, out double Z);
            XYZToRGB(X, Y, Z, out color);
        }

        public static double CompareDE2000(Color color1, Color color2)
        {
            RGBToLab(color1, out ColorLab colorLab1);
            RGBToLab(color2, out ColorLab colorLab2);

            double WHT_L = 1.0;
            double WHT_C = 1.0;
            double WHT_H = 1.0;

            double xC1 = Math.Sqrt(Math.Pow(colorLab1.a, 2) + Math.Pow(colorLab1.b, 2));
            double xC2 = Math.Sqrt(Math.Pow(colorLab2.a, 2) + Math.Pow(colorLab2.b, 2));

            double xCX = (xC1 + xC2) / 2;
            double xGX = 0.5 * (1 - Math.Sqrt(Math.Pow(xCX, 7) / (Math.Pow(xCX, 7) + Math.Pow(25, 7))));
            double xNN = (1 + xGX) * colorLab1.a;
            xC1 = Math.Sqrt(xNN * xNN + colorLab1.b * colorLab1.b);
            double xH1 = LabToHue(xNN, colorLab1.b);
            xNN = (1 + xGX) * colorLab2.a;
            xC2 = Math.Sqrt(xNN * xNN + colorLab2.b * colorLab2.b);
            double xH2 = LabToHue(xNN, colorLab2.b);
            double xDL = colorLab2.L - colorLab1.L;
            double xDC = xC2 - xC1;
            double xDH;
            if ((xC1 * xC2) == 0)
            {
                xDH = 0;
            }
            else
            {
                xNN = Math.Round(xH2 - xH1, 12);
                if (Math.Abs(xNN) <= 180)
                {
                    xDH = xH2 - xH1;
                }
                else
                {
                    if (xNN > 180) xDH = xH2 - xH1 - 360;
                    else xDH = xH2 - xH1 + 360;
                }
            }
            xDH = 2 * Math.Sqrt(xC1 * xC2) * Math.Sin(Deg2Rad(xDH / 2));
            double xLX = (colorLab1.L + colorLab2.L) / 2;
            double xCY = (xC1 + xC2) / 2;
            double xHX;
            if ((xC1 * xC2) == 0)
            {
                xHX = xH1 + xH2;
            }
            else
            {
                xNN = Math.Abs(Math.Round(xH1 - xH2, 12));
                if (xNN > 180)
                {
                    if ((xH2 + xH1) < 360) xHX = xH1 + xH2 + 360;
                    else xHX = xH1 + xH2 - 360;
                }
                else
                {
                    xHX = xH1 + xH2;
                }
                xHX /= 2;
            }
            double xTX = 1 - 0.17 * Math.Cos(Deg2Rad(xHX - 30)) + 0.24
                           * Math.Cos(Deg2Rad(2 * xHX)) + 0.32
                           * Math.Cos(Deg2Rad(3 * xHX + 6)) - 0.20
                           * Math.Cos(Deg2Rad(4 * xHX - 63));
            double xPH = 30 * Math.Exp(-((xHX - 275) / 25) * ((xHX - 275) / 25));
            double xRC = 2 * Math.Sqrt(Math.Pow(xCY, 7) / (Math.Pow(xCY, 7) + Math.Pow(25, 7)));
            double xSL = 1 + ((0.015 * ((xLX - 50) * (xLX - 50)))
                    / Math.Sqrt(20 + ((xLX - 50) * (xLX - 50))));
            double xSC = 1 + 0.045 * xCY;
            double xSH = 1 + 0.015 * xCY * xTX;
            double xRT = -Math.Sin(Deg2Rad(2 * xPH)) * xRC;
            xDL /= (WHT_L * xSL);
            xDC /= (WHT_C * xSC);
            xDH /= (WHT_H * xSH);
            double DeltaE2000 = Math.Sqrt(Math.Pow(xDL, 2) + Math.Pow(xDC, 2) + Math.Pow(xDH, 2) + xRT * xDC * xDH);
            return DeltaE2000;
        }

        public static double LabToHue(double var_a, double var_b)
        {
            double var_bias = 0;
            if (var_a >= 0 && var_b == 0) return 0;
            if (var_a < 0 && var_b == 0) return 180;
            if (var_a == 0 && var_b > 0) return 90;
            if (var_a == 0 && var_b < 0) return 270;
            if (var_a > 0 && var_b > 0) var_bias = 0;
            if (var_a < 0) var_bias = 180;
            if (var_a > 0 && var_b < 0) var_bias = 360;
            return (Rad2Deg(Math.Atan(var_b / var_a)) + var_bias);
        }

        public static double Deg2Rad(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        public static double Rad2Deg(double angle)
        {
            return angle * (180.0 / Math.PI);
        }
    }
}