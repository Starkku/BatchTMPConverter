/*
 * Copyright 2016-2022 by Starkku
 * This file is part of BatchTMPConverter, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see LICENSE.txt.
 */

namespace BatchTMPConverter.Utility
{
    struct Settings
    {
        public bool ShowHelp { get; set; }
        public string Filenames { get; set; }
        public string Palette { get; set; }
        public string CustomExtensions { get; set; }
        public bool SupressBackups { get; set; }
        public bool ReplaceRadarColors { get; set; }
        public string RadarColorMultiplier { get; set; }
        public bool OutputImages { get; set; }
        public string ProcessedFilesLogFilename { get; set; }
        public bool LogToFile { get; set; }
        public bool AllowExtraDataBGOverride { get; set; }
        public bool AccurateColorMatching { get; set; }
        public string PreprocessCommands { get; set; }
    }
}
