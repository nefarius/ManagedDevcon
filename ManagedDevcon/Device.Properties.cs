using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Nefarius.Devcon
{
    using DevPropType = SetupApiWrapper.DevPropType;

    public partial class Device
    {
        private static readonly IDictionary<DevPropType, Type> NativeToManagedTypeMap = new Dictionary<DevPropType, Type>
        {
            {DevPropType.DEVPROP_TYPE_SBYTE, typeof(sbyte) },
            {DevPropType.DEVPROP_TYPE_BYTE, typeof(byte) },
            {DevPropType.DEVPROP_TYPE_INT16, typeof(Int16) },
            {DevPropType.DEVPROP_TYPE_UINT16, typeof(UInt16) },
            {DevPropType.DEVPROP_TYPE_INT32, typeof(Int32) },
            {DevPropType.DEVPROP_TYPE_UINT32, typeof(UInt32) },
            {DevPropType.DEVPROP_TYPE_INT64, typeof(Int64) },
            {DevPropType.DEVPROP_TYPE_UINT64, typeof(UInt64) },
            {DevPropType.DEVPROP_TYPE_FLOAT, typeof(float) },
            {DevPropType.DEVPROP_TYPE_DOUBLE, typeof(double) },
            {DevPropType.DEVPROP_TYPE_DECIMAL, typeof(decimal) },
            {DevPropType.DEVPROP_TYPE_GUID, typeof(Guid) },
            // DEVPROP_TYPE_CURRENCY
            {DevPropType.DEVPROP_TYPE_DATE, typeof(DateTime) },
            {DevPropType.DEVPROP_TYPE_FILETIME, typeof(DateTimeOffset) },
            {DevPropType.DEVPROP_TYPE_BOOLEAN, typeof(bool) },
            {DevPropType.DEVPROP_TYPE_STRING, typeof(string) },
            {DevPropType.DEVPROP_TYPE_STRING_LIST, typeof(string[]) },
            // DEVPROP_TYPE_SECURITY_DESCRIPTOR
            // DEVPROP_TYPE_SECURITY_DESCRIPTOR_STRING
            {DevPropType.DEVPROP_TYPE_DEVPROPKEY, typeof(SetupApiWrapper.DEVPROPKEY) },
            {DevPropType.DEVPROP_TYPE_DEVPROPTYPE, typeof(DevPropType) },
            {DevPropType.DEVPROP_TYPE_BINARY, typeof(byte[]) },
            {DevPropType.DEVPROP_TYPE_ERROR, typeof(int) },
            {DevPropType.DEVPROP_TYPE_NTSTATUS, typeof(int) },
            // DEVPROP_TYPE_STRING_INDIRECT
        };

        /// <summary>
        ///     Returns a device instance property identified by <see cref="DevicePropertyKey"/>.
        /// </summary>
        /// <typeparam name="T">The managed type of the fetched porperty value.</typeparam>
        /// <param name="propertyKey">The <see cref="DevicePropertyKey"/> to query for.</param>
        /// <returns>On success, the value of the queried property.</returns>
        public T GetProperty<T>(DevicePropertyKey propertyKey)
        {
            if (typeof(T) != propertyKey.PropertyType)
            {
                throw new ArgumentException(
                    "The supplied object type doesn't match the porperty type.",
                    nameof(propertyKey)
                );
            }

            IntPtr buffer = IntPtr.Zero;

            try
            {
                if (GetProperty(
                    propertyKey.ToNativeType(),
                    out var propertyType,
                    out buffer,
                    out var size
                ) != SetupApiWrapper.ConfigManagerResult.Success)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (!NativeToManagedTypeMap.TryGetValue(propertyType, out var managedType))
                {
                    throw new ArgumentException(
                        "Unknown porperty type.",
                        nameof(propertyKey)
                    );
                }

                if (typeof(T) != managedType)
                {
                    throw new ArgumentException(
                        "The supplied object type doesn't match the porperty type.",
                        nameof(propertyKey)
                    );
                }

                #region Don't look, nasty trickery

                /*
                 * Handle some native to managed conversions
                 */

                // Regular strings
                if (managedType == typeof(string))
                {
                    var value = Marshal.PtrToStringAuto(buffer);
                    return (T)Convert.ChangeType(value, typeof(T));
                }

                // Double-null-terminated string to list
                if (managedType == typeof(string[]))
                {
                    var value = Marshal.PtrToStringAuto(buffer, (int)size / 2).TrimEnd('\0').Split('\0').ToArray();
                    return (T)Convert.ChangeType(value, typeof(T));
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
