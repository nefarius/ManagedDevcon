using System;
using JetBrains.Annotations;

namespace Nefarius.Devcon
{
    public partial class Difx
    {
        public Difx(DIFLOGCALLBACK logCallback)
        {
            SetDifxLogCallback(logCallback, IntPtr.Zero);
        }

        [UsedImplicitly]
        public uint Preinstall(string infPath, DifxFlags flags)
        {
            return DriverPackagePreinstall(infPath, (uint)flags);
        }

        [UsedImplicitly]
        public uint Install(string infPath, DifxFlags flags, out bool rebootRequired)
        {
            return DriverPackageInstall(infPath, (uint)flags, (IntPtr)0, out rebootRequired);
        }

        [UsedImplicitly]
        public uint Uninstall(string infPath, DifxFlags flags, out bool rebootRequired)
        {
            return DriverPackageUninstall(infPath, (uint)flags, (IntPtr)0, out rebootRequired);
        }
    }
}
