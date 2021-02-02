using System;
using JetBrains.Annotations;

namespace Nefarius.Devcon
{
    /// <summary>
    ///     Describes a unified device property.
    /// </summary>
    /// <remarks>https://docs.microsoft.com/en-us/windows-hardware/drivers/install/unified-device-property-model--windows-vista-and-later-</remarks>
    public abstract class DevicePropertyKey : IComparable
    {
        protected DevicePropertyKey(
            Guid categoryGuid,
            uint propertyIdentifier,
            Type propertyType
        )
        {
            CategoryGuid = categoryGuid;
            PropertyIdentifier = propertyIdentifier;
            PropertyType = propertyType;
        }

        public Guid CategoryGuid { get; }

        public uint PropertyIdentifier { get; }

        public Type PropertyType { get; }

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
    public class CustomDeviceProperty : DevicePropertyKey
    {
        protected CustomDeviceProperty(Guid categoryGuid, uint propertyIdentifier, Type propertyType)
            : base(categoryGuid, propertyIdentifier, propertyType)
        {
        }

        [UsedImplicitly]
        public static DevicePropertyKey CreateCustomDeviceProperty(
            Guid categoryGuid,
            uint propertyIdentifier,
            Type propertyType
        )
        {
            return new CustomDeviceProperty(categoryGuid, propertyIdentifier, propertyType);
        }
    }

    public abstract class DevicePropertyDevice : DevicePropertyKey
    {
        [UsedImplicitly]
        public static DevicePropertyKey DeviceDesc = new DevicePropertyDeviceDeviceDesc();
        
        [UsedImplicitly]
        public static DevicePropertyKey HardwareIds = new DevicePropertyDeviceHardwareIds();
        
        [UsedImplicitly]
        public static DevicePropertyKey CompatibleIds = new DevicePropertyDeviceCompatibleIds();

        private DevicePropertyDevice(uint propertyIdentifier, Type propertyType) : this(
            Guid.Parse("{0xa45c254e, 0xdf1c, 0x4efd, {0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0}}"),
            propertyIdentifier,
            propertyType
        )
        {
        }

        protected DevicePropertyDevice(Guid categoryGuid, uint propertyIdentifier, Type propertyType)
            : base(categoryGuid, propertyIdentifier, propertyType)
        {
        }

        private class DevicePropertyDeviceDeviceDesc : DevicePropertyDevice
        {
            public DevicePropertyDeviceDeviceDesc() : base(2, typeof(string))
            {
            }
        }

        private class DevicePropertyDeviceHardwareIds : DevicePropertyDevice
        {
            public DevicePropertyDeviceHardwareIds() : base(3, typeof(string[]))
            {
            }
        }

        private class DevicePropertyDeviceCompatibleIds : DevicePropertyDevice
        {
            public DevicePropertyDeviceCompatibleIds() : base(4, typeof(string[]))
            {
            }
        }
    }
}