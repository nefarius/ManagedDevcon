using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Nefarius.Devcon
{
    public enum DeviceLocationFlags
    {
        /// <summary>
        ///     The function retrieves the device instance handle for the specified device only if the device is currently
        ///     configured in the device tree.
        /// </summary>
        Normal,

        /// <summary>
        ///     The function retrieves a device instance handle for the specified device if the device is currently configured in
        ///     the device tree or the device is a nonpresent device that is not currently configured in the device tree.
        /// </summary>
        Phantom,

        /// <summary>
        ///     The function retrieves a device instance handle for the specified device if the device is currently configured in
        ///     the device tree or in the process of being removed from the device tree. If the device is in the process of being
        ///     removed, the function cancels the removal of the device.
        /// </summary>
        CancelRemove
    }

    /// <summary>
    ///     Describes an instance of a PNP device.
    /// </summary>
    public partial class Device : IDisposable
    {
        private readonly uint _instanceHandle;
        private bool _disposedValue;

        protected Device(string instanceId, DeviceLocationFlags flags)
        {
            InstanceId = instanceId;
            var iFlags = SetupApiWrapper.CM_LOCATE_DEVNODE_NORMAL;

            switch (flags)
            {
                case DeviceLocationFlags.Normal:
                    iFlags = SetupApiWrapper.CM_LOCATE_DEVNODE_NORMAL;
                    break;
                case DeviceLocationFlags.Phantom:
                    iFlags = SetupApiWrapper.CM_LOCATE_DEVNODE_PHANTOM;
                    break;
                case DeviceLocationFlags.CancelRemove:
                    iFlags = SetupApiWrapper.CM_LOCATE_DEVNODE_CANCELREMOVE;
                    break;
            }

            var ret = SetupApiWrapper.CM_Locate_DevNode(
                ref _instanceHandle,
                instanceId,
                iFlags
            );

            if (ret == SetupApiWrapper.ConfigManagerResult.NoSuchDevinst)
                throw new ArgumentException("The supplied instance wasn't found.", nameof(flags));

            if (ret != SetupApiWrapper.ConfigManagerResult.Success)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            uint nBytes = 0;

            ret = SetupApiWrapper.CM_Get_Device_ID_Size(
                ref nBytes,
                _instanceHandle,
                0
            );

            if (ret != SetupApiWrapper.ConfigManagerResult.Success)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            nBytes += (uint) Marshal.SizeOf(typeof(char));

            var ptrInstanceBuf = Marshal.AllocHGlobal((int) nBytes);

            try
            {
                ret = SetupApiWrapper.CM_Get_Device_ID(
                    _instanceHandle,
                    ptrInstanceBuf,
                    nBytes,
                    0
                );

                if (ret != SetupApiWrapper.ConfigManagerResult.Success)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                DeviceId = (Marshal.PtrToStringAuto(ptrInstanceBuf) ?? string.Empty).ToUpper();
            }
            finally
            {
                Marshal.FreeHGlobal(ptrInstanceBuf);
            }
        }

        /// <summary>
        ///     The instance ID of the device.
        /// </summary>
        public string InstanceId { get; }

        /// <summary>
        ///     The device ID.
        /// </summary>
        public string DeviceId { get; }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [UsedImplicitly]
        public static Device GetDeviceByInstanceId(string instanceId)
        {
            return GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Normal);
        }

        [UsedImplicitly]
        public static Device GetDeviceByInstanceId(string instanceId, DeviceLocationFlags flags)
        {
            return new Device(instanceId, flags);
        }

        /// <summary>
        ///     Return device identified by instance ID/path (symbolic link).
        /// </summary>
        /// <param name="symbolicLink">The device interface path/ID/symbolic link name.</param>
        /// <param name="flags">
        ///     <see cref="DeviceLocationFlags" />
        /// </param>
        /// <returns>A <see cref="Device" />.</returns>
        public static Device GetDeviceByInterfaceId(string symbolicLink, DeviceLocationFlags flags)
        {
            var property = DevicePropertyDevice.InstanceId.ToNativeType();

            var buffer = IntPtr.Zero;
            uint sizeRequired = 0;

            try
            {
                var ret = SetupApiWrapper.CM_Get_Device_Interface_Property(
                    symbolicLink,
                    ref property,
                    out _,
                    IntPtr.Zero,
                    ref sizeRequired,
                    0
                );

                if (ret != SetupApiWrapper.ConfigManagerResult.BufferSmall)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                buffer = Marshal.AllocHGlobal((int) sizeRequired);

                ret = SetupApiWrapper.CM_Get_Device_Interface_Property(
                    symbolicLink,
                    ref property,
                    out _,
                    buffer,
                    ref sizeRequired,
                    0
                );

                if (ret != SetupApiWrapper.ConfigManagerResult.Success)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                var instanceId = Marshal.PtrToStringUni(buffer);

                return GetDeviceByInstanceId(instanceId, flags);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>
        ///     Return device identified by instance ID/path (symbolic link).
        /// </summary>
        /// <param name="symbolicLink">The device interface path/ID/symbolic link name.</param>
        /// <returns>A <see cref="Device" />.</returns>
        public static Device GetDeviceByInterfaceId(string symbolicLink)
        {
            return GetDeviceByInterfaceId(symbolicLink, DeviceLocationFlags.Normal);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        ~Device()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(false);
        }
    }
}