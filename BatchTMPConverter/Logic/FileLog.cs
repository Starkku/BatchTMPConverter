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
using System.IO;
using System.Linq;
using System.Text;
using BatchTMPConverter.Utility;

namespace BatchTMPConverter.Logic
{
    public class FileLog
    {
        public string Filename { get;  private set; }

        private Dictionary<string, string> Files = new Dictionary<string, string>();

        public FileLog(string filename)
        {
            Filename = filename;
            Load(Filename);
        }

        private bool Load(string filename)
        {
            try
            {
                foreach (string line in File.ReadLines(filename))
                {
                    string[] tmp = line.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                    if (tmp.Length < 2)
                        continue;
                    Files.Add(tmp[0], tmp[1]);
                }
            }
            catch (Exception e)
            {
                Logger.Warn("Could not read file log. Error message: " + e.Message);
                return false;
            }

            return true;
        }

        public bool Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[FileLog]");
                foreach (KeyValuePair<string, string> file in Files)
                {
                    sb.AppendLine(file.Key + "=" + file.Value);
                }
                File.WriteAllText(Filename, sb.ToString());
            }
            catch (Exception e)
            {
                Logger.Error("Could not save file log. Error message: " + e.Message);
                return false;
            }
            return true;
        }

        public void UpdateOrAddFile(string filename)
        {
            string currentTime = File.GetLastWriteTime(filename).ToFileTimeUtc().ToString();
            if (!Files.ContainsKey(filename))
            {
                Files.Add(filename, currentTime);
                Logger.Info("Added timestamp for file " + filename + " in file log.");
            }
            else
            {
                Files[filename] = currentTime;
                Logger.Info("Updated timestamp for file " + filename + " in file log.");
            }
        }


        public void DeleteFile(string filename, bool ignoreExtension = false)
        {
            if (!ignoreExtension)
            {
                if (Files.ContainsKey(filename))
                    Files.Remove(filename);
            }
            else
            {
                string match = Files.First(x => Path.GetFileNameWithoutExtension(x.Key).Equals(Path.GetFileNameWithoutExtension(filename))).Key;
                if (!string.IsNullOrEmpty(match))
                {
                    Files.Remove(match);
                }
            }
        }

        public bool HasFileBeenModified(string filename)
        {
            if (!Files.ContainsKey(filename))
                return true;

            string logTimeString = Files[filename];

            try
            {
                DateTime timeCurrent = File.GetLastWriteTime(filename).ToUniversalTime();
                DateTime timeLog = DateTime.FromFileTimeUtc(long.Parse(logTimeString));
                int difference = timeLog.CompareTo(timeCurrent);
                if (difference < 0)
                    return true;
            }
            catch (Exception e)
            {
                Logger.Error("Could not parse time from log for file '" + filename + "'. Error message: " + e.Message);
                return true;
            }
            return false;
        }
    }
}