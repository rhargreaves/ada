using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ada.Interfaces;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Mix;

namespace Ada.Bass
{
    public class BassMixer : IMixer
    {
        private readonly BassEngine _engine;
        private readonly List<BassSource> _sources = new List<BassSource>();
        private readonly int _mixerStreamHandle;

        public event EventHandler PlaybackEnded;

        internal BassMixer(int sampleRate,
                           int audioChannels,
                           BassEngine engine)
        {
            _engine = engine;

            // Create a never-ending mixer channel
            _mixerStreamHandle = BassMix.BASS_Mixer_StreamCreate(sampleRate,
                                                                 audioChannels,
                                                                 BASSFlag.BASS_SAMPLE_FLOAT);
            BassUtil.ThrowOnBassError();
        }

        public void Dispose()
        {
        }

        public IEngine Engine
        {
            get { return _engine; }
        }

        public State State {
            get
            {
                BASSActive status = Un4seen.Bass.Bass.BASS_ChannelIsActive(_mixerStreamHandle);
                BassUtil.ThrowOnBassError();
                switch (status)
                {
                    case BASSActive.BASS_ACTIVE_PLAYING:
                    case BASSActive.BASS_ACTIVE_STALLED:
                        return State.Playing;
                    case BASSActive.BASS_ACTIVE_PAUSED:
                        return State.Paused;
                    default:
                        return State.Stopped;
                }
            }
        }

        public float Volume
        {
            get
            {
                float vol = 1.0f;
                Un4seen.Bass.Bass.BASS_ChannelGetAttribute(_mixerStreamHandle, BASSAttribute.BASS_ATTRIB_VOL, ref vol);
                BassUtil.ThrowOnBassError();
                return vol;
            }
            set
            {
                Un4seen.Bass.Bass.BASS_ChannelSetAttribute(_mixerStreamHandle, BASSAttribute.BASS_ATTRIB_VOL, value);
                BassUtil.ThrowOnBassError();
            }
        }

        public ICollection<ISource> Sources
        {
            get
            {
                return _sources
                    .Select(o => (ISource) o)
                    .ToArray();
            }
        }

        public void AddSource(ISource source, bool pause)
        {
            var bassSource = (BassSource) source;
            bassSource.AddToMixer(_mixerStreamHandle, pause);
            bassSource.SourceEnded += SourceEnded;
            _sources.Add(bassSource);
        }

        void SourceEnded(object sender, EventArgs e)
        {
            RemoveSourceInternal((BassSource)sender);
            ThreadPool.UnsafeQueueUserWorkItem(o =>
            {
                // Raise PlaybackEnded event.
                // Mixer AddSource will be called for new track
                var handler = PlaybackEnded;
                if (handler != null)
                    handler(this, EventArgs.Empty);
            }, null);
        }

        private void RemoveSourceInternal(BassSource source)
        {
            source.SourceEnded -= SourceEnded;
            _sources.Remove(source);
        }

        public void RemoveSource(ISource source)
        {
            var bassSource = (BassSource)source;
            RemoveSourceInternal(bassSource);
            bassSource.RemoveFromMixer();
        }

        public void Play()
        {
            // Stop mixer playback (if applicable)
            Un4seen.Bass.Bass.BASS_ChannelStop(_mixerStreamHandle);
            BassUtil.ThrowOnBassError();

            // Seek sources to start.
            foreach (BassSource src in _sources.Where(o =>
                                                         o.State != BASSActive.BASS_ACTIVE_STOPPED))
            {
                src.PositionBytes = 0;
            }

            // Start playback from beginning.
            Un4seen.Bass.Bass.BASS_ChannelPlay(_mixerStreamHandle, true);
            BassUtil.ThrowOnBassError();
        }

        private void RestartMixerPlayback()
        {
            Un4seen.Bass.Bass.BASS_ChannelSetPosition(_mixerStreamHandle, 0, BASSMode.BASS_POS_BYTES);
            BassUtil.ThrowOnBassError();
        }

        /// <summary>
        /// Flushes the mixer's buffers to effect immediate changes to playback
        /// </summary>
        public void Update()
        {
            RestartMixerPlayback();
        }

        public void Pause()
        {
            Un4seen.Bass.Bass.BASS_ChannelPause(_mixerStreamHandle);
            BassUtil.ThrowOnBassError();
        }

        public void Resume()
        {
            Un4seen.Bass.Bass.BASS_ChannelPlay(_mixerStreamHandle, false);
            BassUtil.ThrowOnBassError();
        }

        public void Stop()
        {
            // Stop mixer
            if (_mixerStreamHandle != 0)
            {
                Un4seen.Bass.Bass.BASS_ChannelStop(_mixerStreamHandle);
                BassUtil.ThrowOnBassError();
                //Bass.BASS_ChannelSetPosition(_bassMixerStreamHandle, 0);
                //AudioEngine.BASS_CheckErr();
            }

            // For every channel...
            foreach (BassSource chan in _sources.ToArray())
            {
                // Stop decoding stream and remove from mixer
                if (chan.State != BASSActive.BASS_ACTIVE_STOPPED)
                {
                    RemoveSource(chan);
                    chan.Dispose();
                }
            }
        }

        internal int MixerStreamHandle
        {
            get { return _mixerStreamHandle; }
        }
    }
}
