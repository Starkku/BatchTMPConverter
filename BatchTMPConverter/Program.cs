using System;
using System.IO;
using NDesk.Options;
using BatchTMPConverter.Utility;
using System.Globalization;

namespace BatchTMPConverter
{
    class Program
    {
        private static Settings settings = new Settings();
        private static OptionSet options;

        static void Main(string[] args)
        {

            options = new OptionSet
            {
            {"h|help", "Show help.", v => settings.ShowHelp = true},
            {"i|files=", "A comma-separated list of input file(s) and/or directory/directories.", v => settings.Filenames = v},
            {"p|palette=", "Palette file to use for conversion.", v => settings.Palette = v},
            {"o|output-images", "Output template data as images instead of converting images to templates.", v => settings.OutputImages = true},
            {"e|extensions-override=", "Comma-separated list of file extensions (including the .) to use instead of built-in defaults.", v => settings.CustomExtensions = v},
            {"r|replace-radarcolor", "Alter tile radar colors based on new image & palette data.", v => settings.ReplaceRadarColors = true},
            {"m|radarcolor-multiplier=", "Multiplier to radar color RGB values, if they are altered.", v => settings.RadarColorMultiplier = v},
            {"x|extraimage-bg-override", "Allow overwriting background color pixels on existing extra images.", v => settings.AllowExtraDataBGOverride = true},
            {"c|accurate-color-matching", "Enables slower but more accurate palette color matching.", v => settings.AccurateColorMatching = true},
            {"d=|preprocess-commands", "List of commands to use to preprocess images before conversion. Comma-separated list of commands consisting of executable and arguments separated by semicolon.", v => settings.PreprocessCommands = v},
            {"b|no-backups", "Disable backing up the edited files with same name using file extension .old.", v => settings.SupressBackups = true},
            {"f=|processed-files-log", "Filename to write timestamps of processed files to. Files with matching filenames and unchanged timestamps will not be processed again.", v => settings.ProcessedFilesLogFilename = v},
            {"l|log-to-file", "Write log info to file as well as console.", v => settings.LogToFile = true}
            };
            options.Parse(args);

            if (settings.LogToFile)
                Logger.EnableWriteToFile();

            bool error = false;

            if (settings.ShowHelp)
            {
                ShowHelp();
                return;
            }

            if (args.Length < 1)
            {
                Logger.Error("No parameters specified.");
                ShowHelp();
                return;
            }

            if (string.IsNullOrWhiteSpace(settings.Filenames) || string.IsNullOrWhiteSpace(settings.Palette))
            {
                Logger.Error("Not enough parameters.");
                ShowHelp();
                return;
            }

            if (error)
                return;

            double radarColorMult = 1.0;

            if (settings.ReplaceRadarColors && !string.IsNullOrWhiteSpace(settings.RadarColorMultiplier))
            {
                try
                {
                    radarColorMult = Math.Abs(double.Parse(settings.RadarColorMultiplier, CultureInfo.InvariantCulture));
                }
                catch (Exception)
                {
                    Logger.Warn("Radar color multiplier not a valid positive floating point value. Defaulting to no multiplier.");
                    radarColorMult = 1.0;
                }
            }

            BatchTMPConverter batchTool = new BatchTMPConverter(settings.Filenames, settings.Palette, settings.CustomExtensions,
                settings.ReplaceRadarColors, radarColorMult, settings.AllowExtraDataBGOverride, settings.AccurateColorMatching, settings.PreprocessCommands, settings.ProcessedFilesLogFilename, settings.SupressBackups);
            
            if (!settings.OutputImages)
                batchTool.ConvertImagesToTMP();
            else
                batchTool.OutputTMPImageData();
        }

        private static void ShowHelp()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("Usage: ");
            Console.WriteLine("");
            var sb = new System.Text.StringBuilder();
            var sw = new StringWriter(sb);
            options.WriteOptionDescriptions(sw);
            Console.WriteLine(sb.ToString());
        }
    }
}
