using System;

namespace Ada.Interfaces
{
    public interface ISource : IDisposable
    {
        Uri Uri { get; }
        long PositionBytes { get; set; }
        TimeSpan Position { get; set; }
        long LengthBytes { get; }
        TimeSpan Length { get; }
        Effects Effects { get; set; }
        int SampleRate { get; }
        int Channels { get; }

        event EventHandler<SyncPoint> Sync;
        event EventHandler SourceEnded;

        void Resume();
        void ApplyEffects();
        SyncPoint AddSyncPoint(long bytes, object tag, bool whenHeard);
        SyncPoint AddSyncPoint(int samples, object tag, bool whenHeard);
        SyncPoint AddSyncPoint(TimeSpan pos, object tag, bool whenHeard);
        void RemoveSyncPoint(SyncPoint syncPoint);
        int GetSampleData(float[] buffer);
        int GetSampleData(int bytesToRetrieve, float[] buffer);

        void FadeIn(TimeSpan duration);
        void FadeOut(TimeSpan duration);
    }
}