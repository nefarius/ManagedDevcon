using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Nefarius.Devcon
{
    public enum DeviceLocationFlags
    {
        /// <summary>
        ///     The function retrieves the device instance handle for the specified device only if the device is currently configured in the device tree.
        /// </summary>
        Normal,
        /// <summary>
        ///     The function retrieves a device instance handle for the specified device if the device is currently configured in the device tree or the device is a nonpresent device that is not currently configured in the device tree.
        /// </summary>
        Phantom,
        /// <summary>
        ///     The function retrieves a device instance handle for the specified device if the device is currently configured in the device tree or in the process of being removed from the device tree. If the device is in the process of being removed, the function cancels the removal of the device.
        /// </summary>
        CancelRemove
    }

    /// <summary>
    ///     Describes an instance of a PNP device.
    /// </summary>
    public partial class Device : IDisposable
    {
        private uint _instanceHandle;
        private bool disposedValue;

        public string InstanceId { get; }

        public string DeviceId { get; }

        protected Device(string instanceId, DeviceLocationFlags flags)
        {
            InstanceId = instanceId;
            int iFlags = SetupApiWrapper.CM_LOCATE_DEVNODE_NORMAL;

            switch(flags)
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

            if (ret != SetupApiWrapper.ConfigManagerResult.Success)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            uint nBytes = 0;

            ret = SetupApiWrapper.CM_Get_Device_ID_Size(
                ref nBytes,
                _instanceHandle,
                0
            );

            if (ret != SetupApiWrapper.ConfigManagerResult.Success)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            nBytes += (uint)Marshal.SizeOf(typeof(char));

            var ptrInstanceBuf = Marshal.AllocHGlobal((int)nBytes);

            try
            {
                ret = SetupApiWrapper.CM_Get_Device_ID(
                    _instanceHandle, 
                    ptrInstanceBuf, 
                    nBytes, 
                    0
                );

                if (ret != SetupApiWrapper.ConfigManagerResult.Success)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                DeviceId = (Marshal.PtrToStringAuto(ptrInstanceBuf) ?? string.Empty).ToUpper();
            }
            finally
            {
                Marshal.FreeHGlobal(ptrInstanceBuf);
            }
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

        [UsedImplicitly]
        private SetupApiWrapper.ConfigManagerResult GetProperty(
            SetupApiWrapper.DEVPROPKEY propertyKey,
            out SetupApiWrapper.DevPropType propertyType,
            out IntPtr valueBuffer,
            out uint valueBufferSize
            )
        {
            valueBuffer = IntPtr.Zero;
            valueBufferSize = 0;
            propertyType = SetupApiWrapper.DevPropType.DEVPROP_TYPE_EMPTY;

            var ret = SetupApiWrapper.CM_Get_DevNode_Property(
                _instanceHandle,
                ref propertyKey,
                out _,
                IntPtr.Zero,
                ref valueBufferSize,
                0
            );

            if (ret != SetupApiWrapper.ConfigManagerResult.BufferSmall
                && ret != SetupApiWrapper.ConfigManagerResult.Success)
            {
                return ret;
            }

            valueBuffer = Marshal.AllocHGlobal((int)valueBufferSize);

            ret = SetupApiWrapper.CM_Get_DevNode_Property(
                _instanceHandle,
                ref propertyKey,
                out propertyType,
                valueBuffer,
                ref valueBufferSize,
                0
            );

            if (ret != SetupApiWrapper.ConfigManagerResult.Success)
            {
                Marshal.FreeHGlobal(valueBuffer);
                valueBuffer = IntPtr.Zero;
                return ret;
            }

            return ret;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        ~Device()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
