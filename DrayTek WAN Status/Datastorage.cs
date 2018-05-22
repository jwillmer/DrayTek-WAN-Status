using InfluxData.Net.Common.Enums;
using InfluxData.Net.InfluxDb;
using InfluxData.Net.InfluxDb.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DrayTek_WAN_Status {
    public class Datastorage {
        readonly StorageProvider _provider = Program.Settings.StorageProvider;

        public void Write(WanStatus wan) {
            if (_provider == StorageProvider.Disk) {
                WriteToDisk(wan);
            }
            else if (_provider == StorageProvider.InfluxDb) {
                WriteToInfluxDbAsync(wan);
            }
            else {
                WriteToInfluxDbAsync(wan);
            }
        }

        public void Write(Exception ex) {
            WriteToDisk(ex);
        }

        #region Disk storage provider

        static string DirectoryPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        static string LogPath = Path.Combine(DirectoryPath + "DrayTek-WAN-Status.csv");
        static string ExceptionLogPath = Path.Combine(DirectoryPath + "exception.log");

        void WriteToDisk(WanStatus wan) {
            var header = $"Date; Time; Is Connected; Upload Speed ({wan.SpeedUnit}); Download Speed ({wan.SpeedUnit})";
            var content = $"{wan.Timestamp.Date.ToString("yyyy-MM-dd")}; {wan.Timestamp.TimeOfDay}; {wan.IsConnected}; {wan.UpSpeed}; {wan.DownSpeed}";
            WriteToDisk(LogPath, content, header);
        }

        void WriteToDisk(Exception ex) {
            WriteToDisk(ExceptionLogPath, DateTime.Now.ToString());
            WriteToDisk(ExceptionLogPath, ex.Message);
            WriteToDisk(ExceptionLogPath, ex.StackTrace + Environment.NewLine);
            Environment.Exit(0);
        }

        void WriteToDisk(string path, string content, string header = null) {
            if (!File.Exists(path)) {
                if (header != null) {
                    File.WriteAllText(path, header + Environment.NewLine);
                }
                else {
                    File.Create(path);
                }
            }

            File.AppendAllText(path, content + Environment.NewLine);
        }

        #endregion

        #region InfluxDB

        void WriteToInfluxDbAsync(WanStatus wan) {
            var settings = Program.Settings;
            var influxDbClient = new InfluxDbClient(settings.InfluxDbUrl,
                                                    settings.InflucDbUser,
                                                    settings.InfluxDbPassword,
                                                    (InfluxDbVersion)settings.InfluxDbVersion);

            var pointToWrite = new Point() {
                Name = "syslog", // serie/measurement/table to write into
                Tags = new Dictionary<string, object>()
                {
                    { "SpeedUnit", wan.SpeedUnit },
                    { "status_type", "ADSL_Status" },
                    { "sender_name", "Vigor" }
                },
                Fields = new Dictionary<string, object>()
                {
                    { "DownSpeed", wan.DownSpeed },
                    { "UpSpeed", wan.UpSpeed },
                    { "IsConnected", wan.IsConnected }
                },
                Timestamp = wan.Timestamp // optional (can be set to any DateTime moment)
            };

            var response = influxDbClient.Client.WriteAsync(pointToWrite, settings.InfluxDbDatabaseName);
            try {
                response.Wait();
            }
            catch (Exception ex) {
                Write(ex);
            }
        }
    }

    #endregion

    [JsonConverter(typeof(StringEnumConverter))]
    public enum StorageProvider {
        Disk = 0,
        InfluxDb = 1
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CustomInfluxDbVersion {
        Latest = 0,
        v_1_3 = 1,
        v_1_0_0 = 2,
        v_0_9_6 = 3,
        v_0_9_5 = 4,
        v_0_9_2 = 5,
        v_0_8_x = 6
    }
}
