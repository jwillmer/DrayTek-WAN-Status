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
            var app = new CommandLineApplication();
            app.Name = "DrayTek WAN Status";
            app.Description = "The app collects the WAN status message that is send via UDP from a DrayTek modem to your machine.";
            app.HelpOption("-?|-h|--help");
            // var optionHide = app.Option("-hide", "Start application hidden.", CommandOptionType.NoValue);
            var optionIp = app.Option("-ip <ip>", "The DrayTek router IP. Must be a IP address. Default is 192.168.1.1", CommandOptionType.SingleValue);
            var optionPort = app.Option("-port <port>", "The DrayTek router port. Must be a number. Default is 514", CommandOptionType.SingleValue);


            app.OnExecute(() => {
                var ip = optionIp.HasValue() ? optionIp.Value() : "192.168.1.1";
                var port = optionPort.HasValue() ? optionPort.Value() : "514";
                // var hideWindow = optionHide.HasValue();

                if(IPAddress.TryParse(ip, out var ipAddress) && Int32.TryParse(port, out var portNumber)) {
                    Run(ipAddress, portNumber);
                } else {
                    Console.WriteLine("IP address or port could not be recognized.");
                }                    
             
                return 0;
            });


            app.Execute(args);           
        }

        static void Run(IPAddress ip, int port) {
            Console.WriteLine($"Listening for UDP packets from {ip}:{port}");
            Console.WriteLine();

            using (var client = new UdpClient(port)) {


                Task.Factory.StartNew(async () => {
                    while (true) {
                        try {
                            var received = await client.ReceiveAsync();
                            if (received.RemoteEndPoint.Address.Equals(ip) && received.RemoteEndPoint.Port == port) {
                                var content = Encoding.ASCII.GetString(received.Buffer, 0, received.Buffer.Length);
                                var wan = WanStatus.Parse(content);

                                Console.WriteLine($"WAN Connected: {wan.IsConnected}");
                                Console.WriteLine($"Download:      {wan.DownSpeed} {wan.SpeedUnit}");
                                Console.WriteLine($"Upload:        {wan.UpSpeed} {wan.SpeedUnit}");
                                Console.WriteLine($"Timestamp:     {wan.Timestamp.TimeOfDay}");

                                Console.SetCursorPosition(0, Console.CursorTop - 4);

                                WriteToDisk(wan);
                            }
                        }
                        catch (Exception ex) {
                            Console.WriteLine(ex.Message);
                        }
                    }
                });


                while (true) {
                    Console.ReadLine();
                }

            }
        }

        static string FilePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\DrayTek-WAN-Status.csv";

        static void WriteToDisk(WanStatus wan) {
            if (!File.Exists(FilePath)) {
                var header = $"Date; Time; Is Connected; Upload Speed ({wan.SpeedUnit}); Download Speed ({wan.SpeedUnit})";
                File.WriteAllText(FilePath, header + Environment.NewLine);
            }

            var line = $"{wan.Timestamp.Date.ToString("yyyy-MM-dd")}; {wan.Timestamp.TimeOfDay}; {wan.IsConnected}; {wan.UpSpeed}; {wan.DownSpeed}";
            File.AppendAllText(FilePath, line + Environment.NewLine);
        }
    }
}