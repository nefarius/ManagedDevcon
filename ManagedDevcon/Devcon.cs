using System;
using System.Runtime.InteropServices;

namespace Nefarius.Devcon
{
    /// <summary>
    ///     Managed wrapper for SetupAPI.
    /// </summary>
    /// <remarks>https://msdn.microsoft.com/en-us/library/windows/hardware/ff550897(v=vs.85).aspx</remarks>
    public static class Devcon
    {
        /// <summary>
        ///     Searches for devices matching the provided class GUID and returns the device path and instance ID.
        /// </summary>
        /// <param name="target">The class GUID to enumerate.</param>
        /// <param name="path">The device path of the enumerated device.</param>
        /// <param name="instanceId">The instance ID of the enumerated device.</param>
        /// <param name="instance">Optional instance ID (zero-based) specifying the device to process on multiple matches.</param>
        /// <returns>True if at least one device was found with the provided class, false otherwise.</returns>
        public static bool Find(Guid target, ref string path, ref string instanceId, int instance = 0)
        {
            var detailDataBuffer = IntPtr.Zero;
            var deviceInfoSet = IntPtr.Zero;

            try
            {
                SP_DEVINFO_DATA deviceInterfaceData = new SP_DEVINFO_DATA(), da = new SP_DEVINFO_DATA();
                int bufferSize = 0, memberIndex = 0;

                deviceInfoSet = SetupDiGetClassDevs(ref target, IntPtr.Zero, IntPtr.Zero,
                    DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

                deviceInterfaceData.cbSize = da.cbSize = Marshal.SizeOf(deviceInterfaceData);

                while (SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref target, memberIndex,
                    ref deviceInterfaceData))
                {
                    SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, IntPtr.Zero, 0,
                        ref bufferSize, ref da);
                    {
                        detailDataBuffer = Marshal.AllocHGlobal(bufferSize);

                        Marshal.WriteInt32(detailDataBuffer,
                            IntPtr.Size == 4 ? 4 + Marshal.SystemDefaultCharSize : 8);

                        if (SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, detailDataBuffer,
                            bufferSize, ref bufferSize, ref da))
                        {
                            var pDevicePathName = detailDataBuffer + 4;

                            path = (Marshal.PtrToStringAuto(pDevicePathName) ?? string.Empty).ToUpper();

                            if (memberIndex == instance)
                            {
                                var nBytes = 256;
                                var ptrInstanceBuf = Marshal.AllocHGlobal(nBytes);

                                CM_Get_Device_ID(da.Flags, ptrInstanceBuf, nBytes, 0);
                                instanceId = (Marshal.PtrToStringAuto(ptrInstanceBuf) ?? string.Empty).ToUpper();

                                Marshal.FreeHGlobal(ptrInstanceBuf);
                                return true;
                            }
                        }
                        else
                        {
                            Marshal.FreeHGlobal(detailDataBuffer);
                        }
                    }

                    memberIndex++;
                }
            }
            finally
            {
                if (deviceInfoSet != IntPtr.Zero)
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return false;
        }

        /// <summary>
        ///     Invokes the installation of a driver via provided .INF file.
        /// </summary>
        /// <param name="fullInfPath">An ansolute path to the .INF file to install.</param>
        /// <param name="rebootRequired">True if a machine reboot is required, false otherwise.</param>
        /// <returns>True on success, false otherwise.</returns>
        public static bool Install(string fullInfPath, ref bool rebootRequired)
        {
            return DiInstallDriver(IntPtr.Zero, fullInfPath, DIIRFLAG_FORCE_INF, ref rebootRequired);
        }

        /// <summary>
        ///     Creates a virtual device node (hardware ID) in the provided device class.
        /// </summary>
        /// <param name="className">The device class name.</param>
        /// <param name="classGuid">The GUID of the device class.</param>
        /// <param name="node">The node path terminated by two null characters.</param>
        /// <returns>True on success, false otherwise.</returns>
        public static bool Create(string className, Guid classGuid, string node)
        {
            var deviceInfoSet = (IntPtr) (-1);
            var deviceInfoData = new SP_DEVINFO_DATA();

            try
            {
                deviceInfoSet = SetupDiCreateDeviceInfoList(ref classGuid, IntPtr.Zero);

                if (deviceInfoSet == (IntPtr) (-1))
                    return false;

                deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);

                if (
                    !SetupDiCreateDeviceInfo(deviceInfoSet, className, ref classGuid, null, IntPtr.Zero,
                        DICD_GENERATE_ID, ref deviceInfoData))
                    return false;

                if (
                    !SetupDiSetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, SPDRP_HARDWAREID, node,
                        node.Length * 2))
                    return false;

                if (!SetupDiCallClassInstaller(DIF_REGISTERDEVICE, deviceInfoSet, ref deviceInfoData))
                    return false;
            }
            finally
            {
                if (deviceInfoSet != (IntPtr) (-1))
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return true;
        }

        /// <summary>
        ///     Removed a device node identified by class GUID, path and instance ID.
        /// </summary>
        /// <param name="classGuid">The device class GUID.</param>
        /// <param name="path">The device node path.</param>
        /// <param name="instanceId">The instance ID.</param>
        /// <returns>True on success, false otherwise.</returns>
        public static bool Remove(Guid classGuid, string path, string instanceId)
        {
            var deviceInfoSet = IntPtr.Zero;

            try
            {
                var deviceInterfaceData = new SP_DEVINFO_DATA();

                deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);
                deviceInfoSet = SetupDiGetClassDevs(ref classGuid, IntPtr.Zero, IntPtr.Zero,
                    DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

                if (SetupDiOpenDeviceInfo(deviceInfoSet, instanceId, IntPtr.Zero, 0, ref deviceInterfaceData))
                {
                    var props = new SP_REMOVEDEVICE_PARAMS();

                    props.ClassInstallHeader = new SP_CLASSINSTALL_HEADER();
                    props.ClassInstallHeader.cbSize = Marshal.SizeOf(props.ClassInstallHeader);
                    props.ClassInstallHeader.InstallFunction = DIF_REMOVE;

                    props.Scope = DI_REMOVEDEVICE_GLOBAL;
                    props.HwProfile = 0x00;

                    if (SetupDiSetClassInstallParams(deviceInfoSet, ref deviceInterfaceData, ref props,
                        Marshal.SizeOf(props)))
                        return SetupDiCallClassInstaller(DIF_REMOVE, deviceInfoSet, ref deviceInterfaceData);
                }
            }
            finally
            {
                if (deviceInfoSet != IntPtr.Zero)
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return false;
        }

        /// <summary>
        ///     Instructs the system to re-enumerate hardware devices.
        /// </summary>
        /// <returns>True on success, false otherwise.</returns>
        public static bool Refresh()
        {
            uint devRoot;

            if (CM_Locate_DevNode_Ex(out devRoot, IntPtr.Zero, 0, IntPtr.Zero) != CR_SUCCESS) return false;
            return CM_Reenumerate_DevNode_Ex(devRoot, 0, IntPtr.Zero) == CR_SUCCESS;
        }

        #region Constant and Structure Definitions

        private const int DIGCF_PRESENT = 0x0002;
        private const int DIGCF_DEVICEINTERFACE = 0x0010;

        private const int DICD_GENERATE_ID = 0x0001;
        private const int SPDRP_HARDWAREID = 0x0001;

        private const int DIF_REMOVE = 0x0005;
        private const int DIF_REGISTERDEVICE = 0x0019;

        private const int DI_REMOVEDEVICE_GLOBAL = 0x0001;

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVINFO_DATA
        {
            internal int cbSize;
            internal readonly Guid ClassGuid;
            internal readonly int Flags;
            internal readonly IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_CLASSINSTALL_HEADER
        {
            internal int cbSize;
            internal int InstallFunction;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_REMOVEDEVICE_PARAMS
        {
            internal SP_CLASSINSTALL_HEADER ClassInstallHeader;
            internal int Scope;
            internal int HwProfile;
        }

        private const uint CM_REENUMERATE_NORMAL = 0x00000000;

        private const uint CM_REENUMERATE_SYNCHRONOUS = 0x00000001;

        // XP and later versions 
        private const uint CM_REENUMERATE_RETRY_INSTALLATION = 0x00000002;

        private const uint CM_REENUMERATE_ASYNCHRONOUS = 0x00000004;

        private const uint CR_SUCCESS = 0x00000000;

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DRVINFO_DATA
        {
            internal readonly uint cbSize;
            internal readonly uint DriverType;
            internal readonly IntPtr Reserved;
            internal readonly string Description;
            internal readonly string MfgName;
            internal readonly string ProviderName;
            internal readonly DateTime DriverDate;
            internal readonly ulong DriverVersion;
        }

        [Flags]
        private enum DiFlags : uint
        {
            DIIDFLAG_SHOWSEARCHUI = 1,
            DIIDFLAG_NOFINISHINSTALLUI = 2,
            DIIDFLAG_INSTALLNULLDRIVER = 3
        }

        private const uint DIIRFLAG_FORCE_INF = 0x00000002;

        #endregion

        #region Interop Definitions

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SetupDiCreateDeviceInfoList(ref Guid ClassGuid, IntPtr hwndParent);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiCreateDeviceInfo(IntPtr DeviceInfoSet, string DeviceName, ref Guid ClassGuid,
            string DeviceDescription, IntPtr hwndParent, int CreationFlags, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiSetDeviceRegistryProperty(IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData, int Property, [MarshalAs(UnmanagedType.LPWStr)] string PropertyBuffer,
            int PropertyBufferSize);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiCallClassInstaller(int InstallFunction, IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent,
            int Flags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData,
            ref Guid InterfaceClassGuid, int MemberIndex, ref SP_DEVINFO_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData,
            int DeviceInterfaceDetailDataSize,
            ref int RequiredSize, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int CM_Get_Device_ID(int DevInst, IntPtr Buffer, int BufferLen, int Flags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiOpenDeviceInfo(IntPtr DeviceInfoSet, string DeviceInstanceId,
            IntPtr hwndParent, int Flags, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiSetClassInstallParams(IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInterfaceData, ref SP_REMOVEDEVICE_PARAMS ClassInstallParams,
            int ClassInstallParamsSize);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint CM_Locate_DevNode_Ex(out uint pdnDevInst, IntPtr pDeviceID, uint ulFlags,
            IntPtr hMachine);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint CM_Reenumerate_DevNode_Ex(uint dnDevInst, uint ulFlags, IntPtr hMachine);

        [DllImport("newdev.dll", SetLastError = true)]
        private static extern bool DiInstallDevice(
            IntPtr hParent,
            IntPtr lpInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            ref SP_DRVINFO_DATA DriverInfoData,
            DiFlags Flags,
            ref bool NeedReboot);

        [DllImport("newdev.dll", SetLastError = true)]
        private static extern bool DiInstallDriver(
            IntPtr hwndParent,
            string FullInfPath,
            uint Flags,
            ref bool NeedReboot);

        #endregion
    }
}