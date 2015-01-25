namespace Ada
{
    public class SyncPoint
    {
        public SyncPoint(long bytePos, object tag)
        {
            BytePosition = bytePos;
            Tag = tag;
        }

        public long BytePosition { get; private set; }
        public object Tag { get; private set; }
    }
}
