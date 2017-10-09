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
                    var matchUpSpeed = Regex.Match(_rawContent, @"(?<=UpSpeed=)\d*");
                    if (!matchUpSpeed.Success) { return 0; }
                    _upSpeed = Math.Round(Decimal.Parse(matchUpSpeed.Value) / 1024 / 1024, 2);
                }
                return _upSpeed.Value;
            }
        }

        public decimal DownSpeed {
            get {
                if (!_downSpeed.HasValue) {
                    var matchDownSpeed = Regex.Match(_rawContent, @"(?<=DownSpeed=)\d*");
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
                    if (!matchDate.Success) { return DateTime.MinValue; }
                    _timestamp = DateTime.ParseExact(matchDate.Value, "MMM  d HH:mm:ss", CultureInfo.InvariantCulture);
                }
                return _timestamp.Value;
            }
        }

        public bool IsConnected {
            get {
                if (!_isConnected.HasValue) {
                    _isConnected = _rawContent.Contains("States=SHOWTIME");
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



        public static WanStatus Parse(string content) {
            return ContainsWanStatus(content) ? GetStatus(content) : new WanStatus();
        }

        public static bool ContainsWanStatus(string content) {
            return content.Contains("ADSL_Status");
        }

        private static WanStatus GetStatus(string content) {
            return new WanStatus {
                _rawContent = content
            };
        }
    }
}
