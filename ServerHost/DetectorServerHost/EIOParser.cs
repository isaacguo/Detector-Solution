using DetectorServerHost.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DetectorServerHost
{
    public class EIOParser
    {
        public static bool TryParseEIOExtension(byte[] data, out EIOExtensionProtocol eioExtensionFrame)
        {
            if (data.Length != 16)
            {
                throw new ArgumentException("data is not a well-formated EIOExtensionProtocol, length of the data should be 16");
            }

            EIOExtensionProtocol frame = new EIOExtensionProtocol();
            frame.PhysicAddress = new byte[6];
            Array.Copy(data, 0, frame.PhysicAddress, 0, 6);
	    frame.length=16;
            frame.IsExtensionData = false;
            frame.Reserved = 0x00;
            EIOStandardFrame standardFrame = new EIOStandardFrame();

            standardFrame.EIOCommand = (EIOCommandEnum)data[9];
            standardFrame.EIOStatus = (EIOStatusEnum)data[10];
            standardFrame.EIOData = new byte[5];
            Array.Copy(data, 11, standardFrame.EIOData, 0, 5);
	    

            frame.EIOFrame = standardFrame;

            eioExtensionFrame = frame;
            return true;
        }
    }
}
