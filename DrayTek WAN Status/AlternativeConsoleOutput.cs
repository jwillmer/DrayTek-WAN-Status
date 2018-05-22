using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DrayTek_WAN_Status {
    public class DisabledConsoleOutput : TextWriter {
        public override Encoding Encoding => throw new NotImplementedException();

        public override void Write(char value) {
            // do nothing
        }

        public override void WriteLine() {
            // do nothing
        }
    }
}
