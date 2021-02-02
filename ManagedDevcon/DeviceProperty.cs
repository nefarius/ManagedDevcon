using System;
using JetBrains.Annotations;

namespace Nefarius.Devcon
{
    /// <summary>
    ///     Describes a unified device property.
    /// </summary>
    /// <remarks>https://docs.microsoft.com/en-us/windows-hardware/drivers/install/unified-device-property-model--windows-vista-and-later-</remarks>
    public abstract class DeviceProperty : IComparable
    {
        protected DeviceProperty(Guid categoryGuid, uint propertyIdentifier)
        {
            CategoryGuid = categoryGuid;
            PropertyIdentifier = propertyIdentifier;
        }

        public Guid CategoryGuid { get; }

        public uint PropertyIdentifier { get; }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }

        internal SetupApiWrapper.DEVPROPKEY ToNativeType()
        {
            return new SetupApiWrapper.DEVPROPKEY(CategoryGuid, PropertyIdentifier);
        }
    }

    /// <summary>
    ///     Describes a custom device property.
    /// </summary>
    public class CustomDeviceProperty : DeviceProperty
    {
        protected CustomDeviceProperty(Guid categoryGuid, uint propertyIdentifier) : base(categoryGuid,
            propertyIdentifier)
        {
        }

        [UsedImplicitly]
        public static DeviceProperty CreateCustomDeviceProperty(Guid categoryGuid, uint propertyIdentifier)
        {
            return new CustomDeviceProperty(categoryGuid, propertyIdentifier);
        }
    }

    public abstract class DevicePropertyDevice : DeviceProperty
    {
        public static DeviceProperty DeviceDesc = new DevicePropertyDeviceDeviceDesc();
        public static DeviceProperty HardwareIds = new DevicePropertyDeviceHardwareIds();
        public static DeviceProperty CompatibleIds = new DevicePropertyDeviceCompatibleIds();

        private DevicePropertyDevice(uint propertyIdentifier) : this(
            Guid.Parse("{0xa45c254e, 0xdf1c, 0x4efd, {0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0}}"), 
            propertyIdentifier
            )
        {
        }

        protected DevicePropertyDevice(Guid categoryGuid, uint propertyIdentifier) : base(categoryGuid, propertyIdentifier)
        {
        }

        private class DevicePropertyDeviceDeviceDesc : DevicePropertyDevice
        {
            public DevicePropertyDeviceDeviceDesc() : base(2)
            {
            }
        }

        private class DevicePropertyDeviceHardwareIds : DevicePropertyDevice
        {
            public DevicePropertyDeviceHardwareIds() : base(3)
            {
            }
        }

        private class DevicePropertyDeviceCompatibleIds : DevicePropertyDevice
        {
            public DevicePropertyDeviceCompatibleIds() : base(4)
            {
            }
        }
    }
}