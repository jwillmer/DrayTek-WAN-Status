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
        readonly StorageProviderOption _storageOption = Program.Settings.StorageProvider.StorageProviderOption;

        public void Write(WanStatus wan) {
            if (_storageOption == StorageProviderOption.CSV) {
                WriteToDisk(wan);
            }
            else if (_storageOption == StorageProviderOption.InfluxDb) {
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

        static string DirectoryPath = Directory.GetCurrentDirectory();
        static string LogPath = Path.Combine(DirectoryPath, "data", "DrayTek-WAN-Status.csv");
        static string ExceptionLogPath = Path.Combine(DirectoryPath, "data", "exception.log");

        void WriteToDisk(WanStatus wan) {
            var path = Path.Combine(DirectoryPath, "data");
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            var delimiter = Program.Settings.StorageProvider.Csv.Delimiter;
            var header = $"Date{delimiter} Time{delimiter} Is Connected{delimiter} Upload Speed ({wan.SpeedUnit}){delimiter} Download Speed ({wan.SpeedUnit})";
            var content = $"{wan.Timestamp.Date.ToString("yyyy-MM-dd")}{delimiter} {wan.Timestamp.TimeOfDay}{delimiter} {wan.IsConnected}{delimiter} {wan.UpSpeed}{delimiter} {wan.DownSpeed}";
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
            var settings = Program.Settings.StorageProvider.InfluxDb;
            var influxDbClient = new InfluxDbClient(settings.Url,
                                                    settings.User,
                                                    settings.Password,
                                                    (InfluxDbVersion)settings.Version);

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

            var response = influxDbClient.Client.WriteAsync(pointToWrite, settings.DatabaseName);
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
    public enum StorageProviderOption {
        CSV = 0,
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
