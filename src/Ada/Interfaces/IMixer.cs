using System;
using System.Collections.Generic;

namespace Ada.Interfaces
{
    public interface IMixer : IDisposable
    {
        IEngine Engine { get; }
        State State { get; }
        float Volume { get; set; }

        ICollection<ISource> Sources { get; }
        void AddSource(ISource source, bool pause);
        void RemoveSource(ISource source);

        event EventHandler PlaybackEnded;

        void Play();
        void Pause();
        void Resume();
        void Stop();
        void Update();
    }
}
