using System;
using System.Runtime.InteropServices;

namespace Nefarius.Devcon
{
    public partial class Difx
    {
        [Flags]
        public enum DifxFlags
        {
            DRIVER_PACKAGE_REPAIR = 0x00000001,
            DRIVER_PACKAGE_SILENT = 0x00000002,
            DRIVER_PACKAGE_FORCE = 0x00000004,
            DRIVER_PACKAGE_ONLY_IF_DEVICE_PRESENT = 0x00000008,
            DRIVER_PACKAGE_LEGACY_MODE = 0x00000010,
            DRIVER_PACKAGE_DELETE_FILES = 0x00000020
        }

        public enum DifxLog
        {
            DIFXAPI_SUCCESS = 0,
            DIFXAPI_INFO = 1,
            DIFXAPI_WARNING = 2,
            DIFXAPI_ERROR = 3
        }

        public delegate void DIFLOGCALLBACK(
            DifxLog eventType,
            int errorCode,
            [MarshalAs(UnmanagedType.LPTStr)] string eventDescription,
            IntPtr callbackContext
        );

        #region P/Invoke

        [DllImport("DIFxAPI.dll", SetLastError = true, CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.Winapi)]
        private static extern uint DriverPackagePreinstall(
            [MarshalAs(UnmanagedType.LPTStr)] string driverPackageInfPath, [MarshalAs(UnmanagedType.U4)] uint flags);

        [DllImport("DIFxAPI.dll", SetLastError = true, CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.Winapi)]
        private static extern uint DriverPackageInstall([MarshalAs(UnmanagedType.LPTStr)] string driverPackageInfPath,
            [MarshalAs(UnmanagedType.U4)] uint flags, IntPtr pInstallerInfo,
            [MarshalAs(UnmanagedType.Bool)] out bool pNeedReboot);

        [DllImport("DIFxAPI.dll", SetLastError = true, CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.Winapi)]
        private static extern uint DriverPackageUninstall([MarshalAs(UnmanagedType.LPTStr)] string driverPackageInfPath,
            [MarshalAs(UnmanagedType.U4)] uint flags, IntPtr pInstallerInfo,
            [MarshalAs(UnmanagedType.Bool)] out bool pNeedReboot);

        [DllImport("DIFxAPI.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SetDifxLogCallback(DIFLOGCALLBACK logCallback, IntPtr callbackContext);

        #endregion
    }
}