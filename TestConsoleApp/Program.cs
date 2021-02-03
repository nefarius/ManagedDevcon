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
            var device = Device.GetDeviceByInterfaceId(
                "\\\\?\\BTHPS3BUS#{53f88889-1aaf-4353-a047-556b69ec6da6}#b&4799736&0&AC7A4D2819AC#{399ed672-e0bd-4fb3-ab0c-4955b56fb86a}", DeviceLocationFlags.Phantom);

            //var device = Device.GetDeviceByInstanceId("USB\\VID_0489&PID_E052\\3C77E6CD11E6");

            var hwIds = device.GetProperty<string[]>(DevicePropertyDevice.HardwareIds);
        }
    }
}
