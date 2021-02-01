using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

namespace Nefarius.Devcon
{
    public class Device
    {
        private uint _instanceHandle;
        
        public string InstanceId { get; }

        protected Device(string instanceId)
        {
            InstanceId = instanceId;

            var ret = SetupApiWrapper.CM_Locate_DevNode(
                ref _instanceHandle,
                instanceId,
                SetupApiWrapper.CM_LOCATE_DEVNODE_NORMAL
            );
            
        }

        [UsedImplicitly]
        public static Device GetDeviceByInstanceId(string instanceId)
        {
            return new Device(instanceId);
        }
    }
}
