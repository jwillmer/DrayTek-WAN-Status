using Microsoft.Extensions.CommandLineUtils;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DrayTek_WAN_Status {

    class Program {
        static void Main(string[] args) {
            var app = new CommandLineApplication() {
                Name = "DrayTek WAN Status",
                Description = "The app collects the WAN status message that is send via UDP from a DrayTek modem to your machine."
            };
            app.HelpOption("-?|-h|--help");
            var optionIp = app.Option("-ip <ip>", "The DrayTek router IP. Must be a IP address. Default is 192.168.1.1", CommandOptionType.SingleValue);
            var optionPort = app.Option("-port <port>", "The DrayTek router port. Must be a number. Default is 514", CommandOptionType.SingleValue);
            var optionRaw = app.Option("-raw", "Will output the recieved UDP message.", CommandOptionType.NoValue);


            app.OnExecute(() => {
                var ip = optionIp.HasValue() ? optionIp.Value() : "192.168.1.1";
                var port = optionPort.HasValue() ? optionPort.Value() : "514";
                var outputRawData = optionRaw.HasValue();

                Console.WriteLine($"{app.Name} (V.{Assembly.GetEntryAssembly().GetName().Version})");
                Console.WriteLine();

                if (IPAddress.TryParse(ip, out var ipAddress) && Int32.TryParse(port, out var portNumber)) {
                    Run(ipAddress, portNumber, outputRawData);
                }
                else {
                    Console.WriteLine("IP address or port could not be recognized.");
                }

                return 0;
            });


            app.Execute(args);
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

                                WriteToDisk(wan);
                            }
                        }
                        catch (Exception ex) {
                            Console.WriteLine(ex.Message);
                            WriteToDisk(ex);
                        }
                    }
                });
                
                while (true) {
                    Console.ReadLine();
                }
            }
        }

        static string DirectoryPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        static string LogPath = DirectoryPath + "\\DrayTek-WAN-Status.csv";
        static string ExceptionLogPath = DirectoryPath + "\\output.txt";

        static void WriteToDisk(WanStatus wan) {
            var header = $"Date; Time; Is Connected; Upload Speed ({wan.SpeedUnit}); Download Speed ({wan.SpeedUnit})";
            var content = $"{wan.Timestamp.Date.ToString("yyyy-MM-dd")}; {wan.Timestamp.TimeOfDay}; {wan.IsConnected}; {wan.UpSpeed}; {wan.DownSpeed}";
            WriteToDisk(LogPath, content, header);
        }

        static void WriteToDisk(Exception ex) {
            WriteToDisk(ExceptionLogPath, DateTime.Now.ToString());
            WriteToDisk(ExceptionLogPath, ex.Message);
            WriteToDisk(ExceptionLogPath, ex.StackTrace + Environment.NewLine);
        }

        static void WriteToDisk(string path, string content, string header = null) {
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
    }
}