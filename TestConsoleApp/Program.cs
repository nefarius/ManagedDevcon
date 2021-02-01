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
            var device = Device.GetDeviceByInstanceId("USB\\VID_0A12&PID_0001\\8&1203F332&0&6");
        }
    }
}
