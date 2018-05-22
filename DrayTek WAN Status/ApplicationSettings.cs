using System;
using System.Collections.Generic;    
using System.Text;    

namespace DrayTek_WAN_Status {
    public class ApplicationSettings {

        public ApplicationSettings() {
            DisableConsoleOutput = false;
            OutputRawData = false;
            ListeningPort = 51400;
            DrayTekIp = "192.168.0.1";
            StorageProvider = StorageProvider.InfluxDb;
            InfluxDbVersion = CustomInfluxDbVersion.Latest;
            InfluxDbUrl = "http://192.168.0.5:8086";
            InflucDbUser = "username";
            InfluxDbPassword = "password";
            InfluxDbDatabaseName = "database";
        }
                                           
       public bool DisableConsoleOutput { get; set; }

        public bool OutputRawData { get; set; }

        public int ListeningPort { get; set; }

        public string DrayTekIp { get; set; }

        public StorageProvider StorageProvider { get; set; }

        public CustomInfluxDbVersion InfluxDbVersion { get; set; }

        public string InfluxDbUrl { get; set; }

        public string InflucDbUser { get; set; }

        public string InfluxDbPassword { get; set; }

        public string InfluxDbDatabaseName { get; set; }
    }
}
