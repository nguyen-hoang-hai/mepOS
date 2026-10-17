using System;
using System.IO;
using System.Web.Script.Serialization;

namespace mepOS.Core.Engines
{
    /// <summary>
    /// Configuration model for Connect To operations with local persistence.
    /// </summary>
    public class ConnectToConfig
    {
        public double MaxAlignOffsetMm { get; set; }
        public bool AllowAlignMove { get; set; }
        public string PreferredMode { get; set; }

        public ConnectToConfig()
        {
            MaxAlignOffsetMm = 100.0;
            AllowAlignMove = true;
            PreferredMode = "Auto";
        }

        private static string ConfigPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "mepOS",
                    "connect_to_config.json"
                );
            }
        }

        public static ConnectToConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var serializer = new JavaScriptSerializer();
                    ConnectToConfig config = serializer.Deserialize<ConnectToConfig>(json);
                    if (config != null) return config;
                }
            }
            catch
            {
                // Fallback to default on any deserialization failure
            }
            return new ConnectToConfig();
        }

        public void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(this);
                File.WriteAllText(ConfigPath, json);
            }
            catch
            {
                // Silently ignore save errors
            }
        }
    }
}
