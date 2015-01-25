using System.Runtime.InteropServices;

namespace Ada.Bass
{
    public class GcHandleSyncProcHandlePair
    {
        public GcHandleSyncProcHandlePair(int syncProcHandle, GCHandle gcHandle)
        {
            this.SyncProcHandle = syncProcHandle;
            this.GCHandle = gcHandle;
        }

        public int SyncProcHandle { get; private set; }
        public GCHandle GCHandle { get; private set; }
    }
}
