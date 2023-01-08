/*
 * Copyright 2016-2023 by Starkku
 * This file is part of BatchTMPConverter, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see LICENSE.txt.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using BatchTMPConverter.Utility;

namespace BatchTMPConverter.Logic
{
    [Flags]
    public enum DataPrecencyFlags : uint
    {
        ExtraData = 0x01,
        ZData = 0x02,
        DamagedData = 0x04,
    }

    public struct RadarColor
    {
        public int R;
        public int G;
        public int B;
        public int Count;
    }

    public class Tmp
    {
        public string FilenameInput { get; private set; }
        public string FilenameOutput { get; private set; }

        // Header
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int BlockWidth { get; private set; }
        public int BlockHeight { get; private set; }
        public int WidthMin { get; private set; } = int.MaxValue;
        public int WidthMax { get; private set; } = int.MinValue;
        public int HeightMin { get; private set; } = int.MaxValue;
        public int HeightMax { get; private set; } = int.MinValue;

        public bool Initialized { get; private set; }

        private readonly List<TmpTile> tiles = new List<TmpTile>();

        public Tmp(string filenameInput, string filenameOutput = null)
        {
            FilenameInput = filenameInput;

            if (!string.IsNullOrWhiteSpace(filenameOutput))
                FilenameOutput = filenameOutput;
            else
                FilenameOutput = filenameInput;

            Load();
        }

        public void Load()
        {
            if (string.IsNullOrWhiteSpace(FilenameInput))
            {
                Logger.Error("No valid input filename given - could not load template file.");
                return;
            }

            FileStream fs = null;
            try
            {
                fs = new FileStream(FilenameInput, FileMode.Open)
                {
                    Position = 0
                };
                byte[] b = new byte[sizeof(int)];
                fs.Read(b, 0, b.Length);
                Width = BitConverter.ToInt32(b, 0);
                fs.Read(b, 0, b.Length);
                Height = BitConverter.ToInt32(b, 0);
                fs.Read(b, 0, b.Length);
                BlockWidth = BitConverter.ToInt32(b, 0);
                fs.Read(b, 0, b.Length);
                BlockHeight = BitConverter.ToInt32(b, 0);

                if (!(BlockWidth == (BlockHeight * 2)) || BlockHeight < 1 || BlockWidth < 1)
                {
                    Logger.Error("Could not open file '" + FilenameInput + "' - not a valid template file.");
                    return;
                }

                byte[] index = new byte[Width * Height * sizeof(int)];
                fs.Read(index, 0, index.Length);
                for (int x = 0; x < Width * Height; x++)
                {
                    int imageData = BitConverter.ToInt32(index, x * 4);
                    fs.Seek(imageData, SeekOrigin.Begin);
                    TmpTile tile = new TmpTile(imageData != 0);
                    tile.Read(this, fs);
                    tiles.Add(tile);

                    if (!tile.IsValid)
                        continue;

                    if (tile.X < WidthMin)
                        WidthMin = tile.X;
                    else if (tile.X > WidthMax)
                        WidthMax = tile.X;

                    if ((tile.Y - (tile.Height * BlockHeight / 2)) < HeightMin)
                        HeightMin = tile.Y - (tile.Height * BlockHeight / 2);
                    else if ((tile.Y - (tile.Height * BlockHeight / 2)) > HeightMax)
                        HeightMax = tile.Y - (tile.Height * BlockHeight / 2);

                    if ((tile.ExtraY - (tile.Height * BlockHeight / 2)) < HeightMin)
                        HeightMin = tile.ExtraY - (tile.Height * BlockHeight / 2);

                    Initialized = true;
                }
            }
            catch (Exception e)
            {
                Logger.Error("Could not open template file '" + FilenameInput + "'. Error message: " + e.Message);
                return;
            }
            finally
            {
                fs.Close();
            }
        }

        public bool Save(bool supressBackups = false)
        {
            if (string.IsNullOrEmpty(FilenameOutput))
            {
                Logger.Error("No valid output filename given - could not save template file.");
                return false;
            }

            FileStream fs = null;
            try
            {
                if (File.Exists(FilenameOutput))
                {
                    if (!supressBackups)
                    {
                        string backupPath = Path.ChangeExtension(FilenameOutput, ".old");
                        if (File.Exists(backupPath))
                            File.Delete(backupPath);

                        File.Move(FilenameOutput, backupPath);
                    }
                }

                fs = new FileStream(FilenameOutput, FileMode.Create, FileAccess.Write);
                fs.Write(BitConverter.GetBytes(Width), 0, sizeof(int));
                fs.Write(BitConverter.GetBytes(Height), 0, sizeof(int));
                fs.Write(BitConverter.GetBytes(BlockWidth), 0, sizeof(int));
                fs.Write(BitConverter.GetBytes(BlockHeight), 0, sizeof(int));
                int offset = 16 + tiles.Count * sizeof(int);
                List<byte> data = new List<byte>();
                List<byte> index = new List<byte>();

                foreach (TmpTile tile in tiles)
                {
                    if (!tile.IsValid)
                    {
                        index.AddRange(new byte[4]);
                        continue;
                    }

                    int len1 = data.Count;
                    data.AddRange(BitConverter.GetBytes(tile.X));
                    data.AddRange(BitConverter.GetBytes(tile.Y));
                    data.AddRange(BitConverter.GetBytes(tile.ExtraDataOffset));
                    data.AddRange(BitConverter.GetBytes(tile.ZDataOffset));
                    data.AddRange(BitConverter.GetBytes(tile.ExtraZDataOffset));
                    data.AddRange(BitConverter.GetBytes(tile.ExtraX));
                    data.AddRange(BitConverter.GetBytes(tile.ExtraY));
                    data.AddRange(BitConverter.GetBytes(tile.ExtraWidth));
                    data.AddRange(BitConverter.GetBytes(tile.ExtraHeight));
                    data.AddRange(BitConverter.GetBytes((uint)tile.DataPrecencyFlags));
                    data.Add(tile.Height);
                    data.Add(tile.TerrainType);
                    data.Add(tile.RampType);
                    data.Add(tile.RadarRedLeft);
                    data.Add(tile.RadarGreenLeft);
                    data.Add(tile.RadarBlueLeft);
                    data.Add(tile.RadarRedRight);
                    data.Add(tile.RadarGreenRight);
                    data.Add(tile.RadarBlueRight);
                    data.AddRange(new byte[3]);
                    data.AddRange(tile.GetTileData());

                    if (tile.HasZData)
                        data.AddRange(tile.GetZData());
                    if (tile.HasExtraData)
                        data.AddRange(tile.GetExtraData());
                    if (tile.HasZData && tile.HasExtraData && 0 < tile.ExtraZDataOffset)
                        data.AddRange(tile.GetExtraZData());

                    int len2 = data.Count;
                    index.AddRange(BitConverter.GetBytes(offset));
                    offset += len2 - len1;
                }

                fs.Write(index.ToArray(), 0, index.Count);
                fs.Write(data.ToArray(), 0, data.Count);

                return true;
            }
            catch (Exception e)
            {
                Logger.Error("Could not save template file '" + FilenameOutput + "'. Error message: " + e.Message);
                return false;
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
        }

        public Rectangle GetRectangle(bool viewTrueHeight = false)
        {
            int half_cy = BlockHeight / 2;
            int X, Y, R, B, TX, TY, TR, TB;

            if (tiles.Count < 1)
            {
                X = Y = 0;
                R = B = 0;
            }
            else
            {
                X = Y = int.MaxValue;
                R = B = int.MinValue;

                foreach (TmpTile tile in tiles)
                {
                    if (!tile.IsValid)
                        continue;

                    TX = tile.X;
                    TY = tile.Y;
                    TR = TX + BlockWidth;
                    TB = TY + BlockHeight;

                    if (tile.HasExtraData)
                    {
                        TX = Math.Min(TX, tile.ExtraX);
                        TY = Math.Min(TY, tile.ExtraY);
                        TR = Math.Max(TR, tile.ExtraX + tile.ExtraWidth);
                        TB = Math.Max(TB, tile.ExtraY + tile.ExtraHeight);
                    }

                    TY -= tile.Height * half_cy;
                    TB -= tile.Height * half_cy;
                    X = Math.Min(X, TX);
                    Y = Math.Min(Y, TY);
                    R = Math.Max(R, TR);
                    B = Math.Max(B, TB);
                }
            }
            if (viewTrueHeight)
            {
                int y = half_cy * (Width + Height);
                B = Math.Max(B, y);
            }

            return new Rectangle(X, Y, R, B);
        }

        public void ConvertImageData(PaletteColor[] imageData, Rectangle tmpRectangle, bool editRadarColors, double radarColorMultiplier, PaletteColor backgroundColor, bool extraDataBGOverride)
        {
            int HalfHeight = BlockHeight / 2;
            int gx = tmpRectangle.Width - tmpRectangle.Left;
            int gy = tmpRectangle.Height - tmpRectangle.Top;
            byte[] clippingMask = GetImageData(tmpRectangle, true);

            foreach (TmpTile tile in tiles)
            {
                if (!tile.IsValid)
                    continue;

                int p = GetCoord(tile.X - tmpRectangle.Left, tile.Y - tmpRectangle.Top - (tile.Height * HalfHeight), gx);
                int x = BlockWidth / 2;
                int cx = 0;
                int y = 0;
                int tdc = 0;
                RadarColor radarColor = new RadarColor();

                for (; y < HalfHeight; y++)
                {
                    cx += 4;
                    x -= 2;
                    CopyArray(imageData, p + x, tile.GetTileData(), tdc, cx, ref radarColor, backgroundColor);
                    tdc += cx;
                    p += gx;
                }

                for (; y < BlockHeight; y++)
                {
                    cx -= 4;
                    x += 2;
                    CopyArray(imageData, p + x, tile.GetTileData(), tdc, cx, ref radarColor, backgroundColor);
                    tdc += cx;
                    p += gx;
                }

                if (tile.HasExtraData)
                {
                    p = GetCoord(tile.ExtraX - tmpRectangle.Left, tile.ExtraY - tmpRectangle.Y - (tile.Height * HalfHeight), gx);
                    cx = tile.ExtraWidth;
                    int cy = tile.ExtraHeight;
                    int edc = 0;

                    for (y = 0; y < cy; y++)
                    {
                        int p2 = p;

                        for (int i = 0; i < cx; i++)
                        {
                            CopyArray(imageData, p2, tile.GetExtraData(), edc, 1, ref radarColor, backgroundColor, true, clippingMask, extraDataBGOverride);
                            edc++;
                            p2++;
                        }

                        p += gx;
                    }
                }

                if (editRadarColors)
                    tile.ReplaceRadarColors(radarColor, radarColorMultiplier);
            }

            Logger.Info(FilenameInput + " tile data successfully replaced.");
        }

        public byte[] GetImageData(Rectangle tmpRectangle, bool ignoreExtraData = false, bool useZData = false)
        {
            int HalfHeight = BlockHeight / 2;
            int gx = tmpRectangle.Width - tmpRectangle.Left;
            int gy = tmpRectangle.Height - tmpRectangle.Top;
            byte[] imageData = new byte[gx * gy];

            foreach (TmpTile tile in tiles)
            {
                if (!tile.IsValid)
                    continue;

                int p = GetCoord(tile.X - tmpRectangle.Left, tile.Y - tmpRectangle.Top - (tile.Height * HalfHeight), gx);
                int x = BlockWidth / 2;
                int cx = 0;
                int y = 0;
                int tdc = 0;

                for (; y < HalfHeight; y++)
                {
                    cx += 4;
                    x -= 2;
                    var data = useZData ? tile.GetZData() : tile.GetTileData();
                    CopyArray(data, tdc, imageData, p + x, cx, false, useZData);
                    tdc += cx;
                    p += gx;
                }

                for (; y < BlockHeight; y++)
                {
                    cx -= 4;
                    x += 2;
                    var data = useZData ? tile.GetZData() : tile.GetTileData();
                    CopyArray(data, tdc, imageData, p + x, cx, false, useZData);
                    tdc += cx;
                    p += gx;
                }

                if (!ignoreExtraData && tile.HasExtraData)
                {
                    p = GetCoord(tile.ExtraX - tmpRectangle.Left, tile.ExtraY - tmpRectangle.Y - (tile.Height * HalfHeight), gx);
                    cx = tile.ExtraWidth;
                    int cy = tile.ExtraHeight;
                    int edc = 0;

                    for (y = 0; y < cy; y++)
                    {
                        int p2 = p;

                        for (int i = 0; i < cx; i++)
                        {
                            var data = useZData ? tile.GetExtraZData() : tile.GetExtraData();
                            CopyArray(data, edc, imageData, p2, 1, true, useZData);
                            edc++;
                            p2++;
                        }

                        p += gx;
                    }
                }
            }

            return imageData;
        }

        public void FixZData()
        {
            for (int t = 0; t < tiles.Count; t++)
            {
                var tile = tiles[t];
                var zData = tile.GetExtraZData();

                if (zData != null)
                {
                    for (int i = 0; i < zData.Length; i++)
                    {
                        if (zData[i] > 31)
                        {
                            //Logger.Info($"{FilenameInput} tile #{t + 1} extra image z-data at index {i} changed from {zData[i]} to 0.");
                            zData[i] = 0;
                        }
                    }
                }
            }
        }

        private void CopyArray(PaletteColor[] src, int srcOffset, byte[] dst, int dstOffset, int length, ref RadarColor radarColor,
            PaletteColor backgroundColor, bool isExtra = false, byte[] clippingMask = null, bool extraDataBGOverride = false)
        {
            for (int i = 0; i < length; i++)
            {
                if (isExtra && !extraDataBGOverride && dst[dstOffset + i] == backgroundColor.Index)
                    continue;

                PaletteColor c;
                if (isExtra && clippingMask != null && clippingMask[srcOffset + i] != backgroundColor.Index)
                    c = backgroundColor;
                else
                    c = src[srcOffset + i];

                dst[dstOffset + i] = c.Index;

                if (c.Red == backgroundColor.Red && c.Green == backgroundColor.Green && c.Blue == backgroundColor.Blue)
                    continue;

                radarColor.R += c.Red;
                radarColor.G += c.Green;
                radarColor.B += c.Blue;
                radarColor.Count++;
            }
        }

        private void CopyArray(byte[] src, int srcOffset, byte[] dst, int dstOffset, int length, bool isExtra = false, bool isZData = false)
        {
            for (int i = 0; i < length; i++)
            {
                if (isExtra && src[srcOffset + i] == 0)
                    continue;

                if (isZData && (src[srcOffset + i] == 205 || src[srcOffset + i] == 0))
                    continue;

                dst[dstOffset + i] = src[srcOffset + i];
            }
        }

        private int GetCoord(int x, int y, int cx) => x + cx * y;
    }

    internal class TmpTile
    {
        public bool IsValid { get; private set; } = true;
        public int X { get; private set; }
        public int Y { get; private set; }
        public int ExtraDataOffset { get; private set; }
        public int ZDataOffset { get; private set; }
        public int ExtraZDataOffset { get; private set; }
        public int ExtraX { get; private set; }
        public int ExtraY { get; private set; }
        public int ExtraWidth { get; private set; }
        public int ExtraHeight { get; private set; }
        public DataPrecencyFlags DataPrecencyFlags { get; private set; }
        public byte Height { get; private set; }
        public byte TerrainType { get; private set; }
        public byte RampType { get; private set; }
        public byte RadarRedLeft { get; private set; }
        public byte RadarGreenLeft { get; private set; }
        public byte RadarBlueLeft { get; private set; }
        public byte RadarRedRight { get; private set; }
        public byte RadarGreenRight { get; private set; }
        public byte RadarBlueRight { get; private set; }

        private byte[] TileData; // always available
        private byte[] ExtraData; // available if presency flags says so
        private byte[] ZData; // available if presency flags says so
        private byte[] ExtraZData; // available if presency flags says so

        public bool HasExtraData => (DataPrecencyFlags & DataPrecencyFlags.ExtraData) == DataPrecencyFlags.ExtraData;

        public bool HasZData => (DataPrecencyFlags & DataPrecencyFlags.ZData) == DataPrecencyFlags.ZData;

        public bool HasDamagedData => (DataPrecencyFlags & DataPrecencyFlags.DamagedData) == DataPrecencyFlags.DamagedData;

        public TmpTile(bool isValid = true)
        {
            IsValid = isValid;
        }

        public void Read(Tmp tmp, FileStream fs)
        {
            byte[] b = new byte[sizeof(int)];
            fs.Read(b, 0, b.Length);
            X = BitConverter.ToInt32(b, 0);
            fs.Read(b, 0, b.Length);
            Y = BitConverter.ToInt32(b, 0);
            fs.Read(b, 0, b.Length);
            ExtraDataOffset = BitConverter.ToInt32(b, 0);
            fs.Read(b, 0, b.Length);
            ZDataOffset = BitConverter.ToInt32(b, 0);
            fs.Read(b, 0, b.Length);
            ExtraZDataOffset = BitConverter.ToInt32(b, 0);
            fs.Read(b, 0, b.Length);
            ExtraX = BitConverter.ToInt32(b, 0);
            fs.Read(b, 0, b.Length);
            ExtraY = BitConverter.ToInt32(b, 0);
            fs.Read(b, 0, b.Length);
            ExtraWidth = BitConverter.ToInt32(b, 0);
            fs.Read(b, 0, b.Length);
            ExtraHeight = BitConverter.ToInt32(b, 0);
            fs.Read(b, 0, b.Length);
            int test = BitConverter.ToInt32(b, 0);
            DataPrecencyFlags = (DataPrecencyFlags)BitConverter.ToInt32(b, 0);
            Height = (byte)fs.ReadByte();
            TerrainType = (byte)fs.ReadByte();
            RampType = (byte)fs.ReadByte();
            RadarRedLeft = (byte)fs.ReadByte();
            RadarGreenLeft = (byte)fs.ReadByte();
            RadarBlueLeft = (byte)fs.ReadByte();
            RadarRedRight = (byte)fs.ReadByte();
            RadarGreenRight = (byte)fs.ReadByte();
            RadarBlueRight = (byte)fs.ReadByte();
            b = new byte[3];
            fs.Read(b, 0, b.Length);
            b = new byte[tmp.BlockWidth * tmp.BlockHeight / 2];
            fs.Read(b, 0, b.Length);
            TileData = b;

            if (HasZData)
            {
                b = new byte[tmp.BlockWidth * tmp.BlockHeight / 2];
                fs.Read(b, 0, b.Length);
                ZData = b;
            }

            if (HasExtraData)
            {
                b = new byte[Math.Abs(ExtraWidth * ExtraHeight)];
                fs.Read(b, 0, b.Length);
                ExtraData = b;
            }

            if (HasZData && HasExtraData && 0 < ExtraZDataOffset && ExtraZDataOffset < fs.Length)
            {
                b = new byte[Math.Abs(ExtraWidth * ExtraHeight)];
                fs.Read(b, 0, b.Length);
                ExtraZData = b;
            }
        }

        public void ReplaceRadarColors(RadarColor radarColor, double radarColorMultiplier)
        {
            byte R = (byte)(radarColor.R / Math.Max(radarColor.Count, 1));
            byte G = (byte)(radarColor.G / Math.Max(radarColor.Count, 1));
            byte B = (byte)(radarColor.B / Math.Max(radarColor.Count, 1));
            RadarRedLeft = (byte)Math.Min(R * radarColorMultiplier, 255);
            RadarGreenLeft = (byte)Math.Min(G * radarColorMultiplier, 255);
            RadarBlueLeft = (byte)Math.Min(B * radarColorMultiplier, 255);
            RadarRedRight = (byte)Math.Min(R * radarColorMultiplier, 255);
            RadarGreenRight = (byte)Math.Min(G * radarColorMultiplier, 255);
            RadarBlueRight = (byte)Math.Min(B * radarColorMultiplier, 255);
        }

        public byte[] GetTileData() => TileData;

        public byte[] GetExtraData() => ExtraData;

        internal byte[] GetZData() => ZData;

        internal byte[] GetExtraZData() => ExtraZData;
    }
}
