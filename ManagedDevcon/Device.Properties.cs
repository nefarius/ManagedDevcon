using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace Nefarius.Devcon
{
    public partial class Device
    {
        private static readonly IDictionary<SetupApiWrapper.DevPropType, Type> NativeToManagedTypeMap =
            new Dictionary<SetupApiWrapper.DevPropType, Type>
            {
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_SBYTE, typeof(sbyte)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_BYTE, typeof(byte)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_INT16, typeof(short)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_UINT16, typeof(ushort)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_INT32, typeof(int)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_UINT32, typeof(uint)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_INT64, typeof(long)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_UINT64, typeof(ulong)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_FLOAT, typeof(float)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_DOUBLE, typeof(double)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_DECIMAL, typeof(decimal)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_GUID, typeof(Guid)},
                // DEVPROP_TYPE_CURRENCY
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_DATE, typeof(DateTime)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_FILETIME, typeof(DateTimeOffset)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_BOOLEAN, typeof(bool)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_STRING, typeof(string)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_STRING_LIST, typeof(string[])},
                // DEVPROP_TYPE_SECURITY_DESCRIPTOR
                // DEVPROP_TYPE_SECURITY_DESCRIPTOR_STRING
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_DEVPROPKEY, typeof(SetupApiWrapper.DEVPROPKEY)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_DEVPROPTYPE, typeof(SetupApiWrapper.DevPropType)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_BINARY, typeof(byte[])},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_ERROR, typeof(int)},
                {SetupApiWrapper.DevPropType.DEVPROP_TYPE_NTSTATUS, typeof(int)}
                // DEVPROP_TYPE_STRING_INDIRECT
            };

        /// <summary>
        ///     Returns a device instance property identified by <see cref="DevicePropertyKey" />.
        /// </summary>
        /// <typeparam name="T">The managed type of the fetched porperty value.</typeparam>
        /// <param name="propertyKey">The <see cref="DevicePropertyKey" /> to query for.</param>
        /// <returns>On success, the value of the queried property.</returns>
        public T GetProperty<T>(DevicePropertyKey propertyKey)
        {
            if (typeof(T) != propertyKey.PropertyType)
                throw new ArgumentException(
                    "The supplied object type doesn't match the porperty type.",
                    nameof(propertyKey)
                );

            var buffer = IntPtr.Zero;

            try
            {
                if (GetProperty(
                    propertyKey.ToNativeType(),
                    out var propertyType,
                    out buffer,
                    out var size
                ) != SetupApiWrapper.ConfigManagerResult.Success)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                if (!NativeToManagedTypeMap.TryGetValue(propertyType, out var managedType))
                    throw new ArgumentException(
                        "Unknown porperty type.",
                        nameof(propertyKey)
                    );

                if (typeof(T) != managedType)
                    throw new ArgumentException(
                        "The supplied object type doesn't match the porperty type.",
                        nameof(propertyKey)
                    );

                #region Don't look, nasty trickery

                /*
                 * Handle some native to managed conversions
                 */

                // Regular strings
                if (managedType == typeof(string))
                {
                    var value = Marshal.PtrToStringAuto(buffer);
                    return (T) Convert.ChangeType(value, typeof(T));
                }

                // Double-null-terminated string to list
                if (managedType == typeof(string[]))
                {
                    var value = Marshal.PtrToStringAuto(buffer, (int) size / 2).TrimEnd('\0').Split('\0').ToArray();
                    return (T) Convert.ChangeType(value, typeof(T));
                }

                // Byte & SByte
                if (managedType == typeof(sbyte)
                    || managedType == typeof(byte))
                {
                    var value = Marshal.ReadByte(buffer);
                    return (T) Convert.ChangeType(value, typeof(T));
                }

                // (U)Int16
                if (managedType == typeof(Int16)
                    || managedType == typeof(UInt16))
                {
                    var value = Marshal.ReadInt16(buffer);
                    return (T) Convert.ChangeType(value, typeof(T));
                }

                // (U)Int32
                if (managedType == typeof(Int32)
                    || managedType == typeof(UInt32))
                {
                    var value = Marshal.ReadInt32(buffer);
                    return (T) Convert.ChangeType(value, typeof(T));
                }

                // (U)Int64
                if (managedType == typeof(Int64)
                    || managedType == typeof(UInt64))
                {
                    var value = Marshal.ReadInt64(buffer);
                    return (T) Convert.ChangeType(value, typeof(T));
                }

                // FILETIME/DateTimeOffset
                if (managedType == typeof(DateTimeOffset))
                {
                    var value = DateTimeOffset.FromFileTime(Marshal.ReadInt64(buffer));
                    return (T) Convert.ChangeType(value, typeof(T));
                }

                #endregion

                throw new NotImplementedException("Type not supported.");
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static bool IsNumericType(Type t)
        {
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }
}