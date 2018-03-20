using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace DetectorServerHost.Model
{
    public class EIOExtensionProtocol
    {
        public byte[] PhysicAddress { get; set; }
        public int length { get; set; }
        public bool IsExtensionData { get; set; }
        public byte Reserved { get; set; }
        public EIOStandardFrame EIOFrame { get; set; }
        public IPAddress IPAddress{ get; set; }
        public int Port { get; set; }
    }
    public class EIOStandardFrame
    {
        public EIOCommandEnum EIOCommand { get; set; }
        public EIOStatusEnum EIOStatus { get; set; }
        public byte[] EIOData { get; set; }
    }
    public enum EIOCommandEnum
    {
        Refresh = 0x00,
        Changed = 0x01,
        SetEIOLongOutput = 0x10,
        SetEIOShortOutput = 0x11,
        GetEIOOutput = 0x20,
        Temperature = 0x40,
    }
    public enum EIOStatusEnum
    {
        Valid = 0x00,
        InValid
    }
}
