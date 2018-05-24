using Newtonsoft.Json;
using PrimS.Telnet;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
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

            if (Settings.DisableConsoleOutput) {
                Console.WriteLine("- Console output disabled!");
                Console.SetOut(new DisabledConsoleOutput());
            }

            Run();
        }

        private static void InitConfiguration() {
            var directory = Directory.GetCurrentDirectory();
            var configName = "app.config";
            var configPath = Path.Combine(directory, "config", configName);

            var path = Path.Combine(directory, "config");
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            if (!File.Exists(configPath)) {
                Settings = new ApplicationSettings();
                var content = JsonConvert.SerializeObject(Settings, Formatting.Indented);
                File.WriteAllText(configPath, content);
                Console.WriteLine("No config file found! Creating file: 'app.config'.");
                Console.WriteLine("Please update config values and restart the application..");
            }
            else {
                var content = File.ReadAllText(configPath);
                Settings = JsonConvert.DeserializeObject<ApplicationSettings>(content);
            }
        }

        static void Run() {
            if (Settings.QueryOptions.Option == QueryOption.UDP) {
                Run(Settings.QueryOptions.Udp, Settings.OutputRawData);
            }
            else {
                Run(Settings.QueryOptions.Telnet, Settings.OutputRawData);
            }
        }

        private static void Run(TelnetOptions options, bool outputRawData) {
            Task.Factory.StartNew(() => {
                while (true) {                       
                    try {
                        using (Client client = new Client(options.Ip, 23, new CancellationToken())) {
                            client.TryLoginAsync(options.User, options.Password, 5000).Wait();
                            client.WriteLine("show status");
                            var outputRequest = client.TerminatedReadAsync(">", TimeSpan.FromMilliseconds(1000));
                            outputRequest.Wait();

                            if (outputRequest.IsCompletedSuccessfully) { 
                                var output = outputRequest.Result;
                                var startIndex = output.IndexOf("VDSL Information:");
                                var content = output.Substring(startIndex);
                                content = content.Remove(content.LastIndexOf(Environment.NewLine)).TrimEnd();

                                var linePosition = 0;

                                if (outputRawData) {
                                    Console.WriteLine("Recieved Data:");
                                    Console.WriteLine(content);
                                    Console.WriteLine();
                                    linePosition = 6;
                                }

                                ParseContent(content, linePosition);
                            }
                        }
                    }
                    catch (Exception ex) {
                        Console.WriteLine(ex.Message);
                        _storage.Write(ex);
                    }
                    Thread.Sleep(options.QueryIntervalSeconds * 1000);
                }
            });

            while (true) {
                Console.ReadLine();
            }
        }

        static void Run(UdpOptions options, bool outputRawData) {
            Console.WriteLine($"Listening for UDP packets from {options.Ip}:{options.ListeningPort}");
            Console.WriteLine();

            using (var client = new UdpClient(options.ListeningPort)) {
                Task.Factory.StartNew(async () => {
                    while (true) {
                        try {
                            var received = await client.ReceiveAsync();
                            if (received.RemoteEndPoint.Address.Equals(options.Ip) && received.RemoteEndPoint.Port == options.ListeningPort) {
                                var content = Encoding.ASCII.GetString(received.Buffer, 0, received.Buffer.Length);
                                var linePosition = 0;

                                if (outputRawData) {
                                    Console.Write($"Recieved Data: {content}");
                                    Console.WriteLine();
                                    linePosition = 2;
                                }

                                ParseContent(content, linePosition);
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

        static void ParseContent(string content, int linePosition = 0) {
            linePosition +=4;
            var wan = WanStatus.Parse(content);

            Console.WriteLine($"WAN Connected: {wan.IsConnected}");
            Console.WriteLine($"Download:      {wan.DownSpeed} {wan.SpeedUnit}");
            Console.WriteLine($"Upload:        {wan.UpSpeed} {wan.SpeedUnit}");
            Console.WriteLine($"Timestamp:     {wan.Timestamp.TimeOfDay}");

            // Console.CursorTop is always 0 in docker
            int top = (Console.CursorTop - linePosition) >= 0 ? Console.CursorTop - linePosition : 0;
            Console.SetCursorPosition(0, top);

            _storage.Write(wan);
        }
    }
}