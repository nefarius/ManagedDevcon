using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nefarius.Devcon;

namespace TestConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var device = Device.GetDeviceByInstanceId("USB\\VID_0489&PID_E052\\3C77E6CD11E6");
        }
    }
}
