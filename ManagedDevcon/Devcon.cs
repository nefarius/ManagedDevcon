﻿using System;
using System.Runtime.InteropServices;

namespace Nefarius.Devcon
{
    /// <summary>
    ///     Managed wrapper for SetupAPI.
    /// </summary>
    /// <remarks>https://msdn.microsoft.com/en-us/library/windows/hardware/ff550897(v=vs.85).aspx</remarks>
    public static partial class Devcon
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

        /// <summary>
        ///     Given an INF file and a hardware ID, this function installs updated drivers for devices that match the hardware ID.
        /// </summary>
        /// <param name="hardwareId">A string that supplies the hardware identifier to match existing devices on the computer.</param>
        /// <param name="fullInfPath">A string that supplies the full path file name of an INF file.</param>
        /// <param name="rebootRequired">A variable that indicates whether a restart is required and who should prompt for it.</param>
        /// <returns>
        ///     The function returns TRUE if a device was upgraded to the specified driver.
        ///     Otherwise, it returns FALSE and the logged error can be retrieved with a call to GetLastError.
        /// </returns>
        public static bool Update(string hardwareId, string fullInfPath,
            out bool rebootRequired)
        {
            return UpdateDriverForPlugAndPlayDevices(IntPtr.Zero, hardwareId, fullInfPath,
                INSTALLFLAG_FORCE | INSTALLFLAG_NONINTERACTIVE, out rebootRequired);
        }
    }
}