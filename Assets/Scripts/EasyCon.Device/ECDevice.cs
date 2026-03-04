using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace EasyDevice
{
public sealed class ECDevice
{
    public static List<string> GetPortNames() => new List<string>(SerialPort.GetPortNames());
}
}

