using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DrayTek_WAN_Status {
    public class WanStatus {
        public decimal UpSpeed {
            get {
                if (!_upSpeed.HasValue) {
                    var matchUpSpeed = Regex.Match(_rawContent, @"((?<=UpSpeed=)|(?<=UP Speed:))\d*");
                    if (!matchUpSpeed.Success) { return 0; }
                    _upSpeed = Math.Round(Decimal.Parse(matchUpSpeed.Value) / 1024 / 1024, 2);
                }
                return _upSpeed.Value;
            }
        }

        public decimal DownSpeed {
            get {
                if (!_downSpeed.HasValue) {
                    var matchDownSpeed = Regex.Match(_rawContent, @"((?<=DownSpeed=)|(?<=Down Speed:))\d*");
                    if (!matchDownSpeed.Success) { return 0; }
                    _downSpeed = Math.Round(Decimal.Parse(matchDownSpeed.Value) / 1024 / 1024, 2);
                }
                return _downSpeed.Value;
            }
        }

        public DateTime Timestamp {
            get {
                if (!_timestamp.HasValue) {
                    var matchDate = Regex.Match(_rawContent, "(?<=>).*(?= Vigor)");
                    if (!matchDate.Success) { return DateTime.Now; }
                    var value = matchDate.Value.Replace("  ", " "); // days smaller then 10 will create an additional space
                    _timestamp = DateTime.ParseExact(value, "MMM d HH:mm:ss", CultureInfo.InvariantCulture);
                }
                return _timestamp.Value;
            }
        }

        public bool IsConnected {
            get {
                if (!_isConnected.HasValue) {
                    _isConnected = _rawContent.Contains("SHOWTIME");
                }
                return _isConnected.Value;
            }
        }
  
        public string SpeedUnit { get { return "Mbps"; } }

        private bool? _isConnected = null;
        private DateTime? _timestamp = null;
        private decimal? _downSpeed = null;
        private decimal? _upSpeed = null;

        private string _rawContent;

        // UDP Data:
        // <174>Jan 11 05:34:03 Vigor: ADSL_Status:[Mode=24A States=SHOWTIME UpSpeed=26996000 DownSpeed=59815000 SNR=9 Atten=17 ]
        
        // Telnet Data
        // VDSL Information:      VDSL Firmware Version:05-06-07-06-01-07
        // Mode:17A State:SHOWTIME TX Block:0     RX Block:0
        // Corrected Blocks:31    Uncorrected Blocks:0
        // UP Speed:26996000   Down Speed:59815000   SNR Margin:9   Loop Att.:17

        public static WanStatus Parse(string content) {
            return ContainsWanStatus(content) ? GetStatus(content) : new WanStatus();
        }

        public static bool ContainsWanStatus(string content) {
            return content.Contains("ADSL_Status") || content.Contains("VDSL Information");
        }

        private static WanStatus GetStatus(string content) {
            return new WanStatus {
                _rawContent = content
            };
        }
    }
}
