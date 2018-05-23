using Newtonsoft.Json;
using System;                       
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DrayTek_WAN_Status {

    class Program {
        public static ApplicationSettings Settings { get; set; }
        public static Datastorage _storage;

        static void Main(string[] args) {
            Console.WriteLine($"DrayTek WAN Status (V.{Assembly.GetEntryAssembly().GetName().Version})");
            Console.WriteLine();

            InitConfiguration();     
            _storage = new Datastorage();


           //test
           //var wan = WanStatus.Parse(@"<174>Jan 11 05:34:03 Vigor: ADSL_Status:[Mode=24A States=SHOWTIME UpSpeed=26996000 DownSpeed=59815000 SNR=9 Atten=17 ]");
           //_storage.Write(wan);



            if (Settings.DisableConsoleOutput) {
                Console.WriteLine("- Console output disabled!");
                Console.SetOut(new DisabledConsoleOutput());
            }                          

            var ip = Settings.DrayTekIp;
            var port = Settings.ListeningPort;
            var outputRawData = Settings.OutputRawData;              

            if (IPAddress.TryParse(ip, out var ipAddress)) {
                Run(ipAddress, port, outputRawData);
            }
            else {
                Console.WriteLine("IP address could not be recognized.");
            }

        }

        private static void InitConfiguration() {
            var directory = Directory.GetCurrentDirectory();
            var configName = "app.config";
            var configPath = Path.Combine(directory, "config", configName);

            if (!File.Exists(configPath)) {
                Settings = new ApplicationSettings();
                var content = JsonConvert.SerializeObject(Settings, Formatting.Indented);
                File.WriteAllText(configPath, content);
                Console.WriteLine("No config file found! Creating file: 'app.config'.");       
                Console.WriteLine("Please update config values and restart the application..");
                Console.ReadLine();
                Environment.Exit(0);
            }
            else {
                var content = File.ReadAllText(configPath);
                Settings = JsonConvert.DeserializeObject<ApplicationSettings>(content);
            }
        }

        static void Run(IPAddress ip, int port, bool outputRawData) {
            Console.WriteLine($"Listening for UDP packets from {ip}:{port}");
            Console.WriteLine();

            using (var client = new UdpClient(port)) {
                Task.Factory.StartNew(async () => {
                    while (true) {
                        try {
                            var received = await client.ReceiveAsync();
                            if (received.RemoteEndPoint.Address.Equals(ip) && received.RemoteEndPoint.Port == port) {
                                var content = Encoding.ASCII.GetString(received.Buffer, 0, received.Buffer.Length);
                                var linePosition = 4;

                                if (outputRawData) {
                                    Console.Write($"Recieved Data: {content}");
                                    Console.WriteLine();
                                    linePosition = 6;
                                }

                                var wan = WanStatus.Parse(content);

                                Console.WriteLine($"WAN Connected: {wan.IsConnected}");
                                Console.WriteLine($"Download:      {wan.DownSpeed} {wan.SpeedUnit}");
                                Console.WriteLine($"Upload:        {wan.UpSpeed} {wan.SpeedUnit}");
                                Console.WriteLine($"Timestamp:     {wan.Timestamp.TimeOfDay}");

                                Console.SetCursorPosition(0, Console.CursorTop - linePosition);

                                _storage.Write(wan);
                            }
                        }
                        catch (Exception ex) {
                            Console.WriteLine(ex.Message);
                            _storage.Write(ex);  
                        }
                    }
                });

                while (true) {
                    Console.ReadLine();
                }
            }
        }
    }
}