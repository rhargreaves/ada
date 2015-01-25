using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Ada.Interfaces;
using Common.Logging;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;
using Un4seen.Bass.AddOn.Mix;

namespace Ada.Bass
{
    public class BassSource : ISource
    {
        #region Private Fields

        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly SYNCPROC _endedSyncProc;
        private readonly object _stopObj = new object();

        private readonly Dictionary<SyncPoint, GcHandleSyncProcHandlePair> _syncPointHandles =
            new Dictionary<SyncPoint, GcHandleSyncProcHandlePair>();

        private readonly SYNCPROC _syncPointSyncProc;
        private int _decodingStreamBeforeFxHandle;
        private int _decodingStreamHandle;

        #endregion

        #region Public Properties

        public Uri Uri { get; private set; }
        public TimeSpan Length { get; private set; }
        public long LengthBytes { get; private set; }
        public int SampleRate { get; private set; }
        public int Channels { get; private set; }

        public Effects Effects { get; set; }

        public TimeSpan Position
        {
            get
            {
                if (State == BASSActive.BASS_ACTIVE_STOPPED)
                    return TimeSpan.Zero;
                return TimeSpan.FromSeconds(ConvertBytesToSeconds(PositionBytes));
            }
            set
            {
                if (State == BASSActive.BASS_ACTIVE_STOPPED)
                    return;
                PositionBytes = ConvertSecondsToBytes(value.TotalSeconds);
            }
        }

        public long PositionBytes
        {
            get
            {
                if (State == BASSActive.BASS_ACTIVE_STOPPED)
                    return 0;

                long pos = BassMix.BASS_Mixer_ChannelGetPosition(_decodingStreamHandle, BASSMode.BASS_POS_BYTES);
                if (Un4seen.Bass.Bass.BASS_ErrorGetCode() == BASSError.BASS_ERROR_HANDLE)
                    pos = Un4seen.Bass.Bass.BASS_ChannelGetPosition(_decodingStreamHandle, BASSMode.BASS_POS_BYTES);
                BassUtil.ThrowOnBassError();
                return pos;
            }
            set
            {
                if (State == BASSActive.BASS_ACTIVE_STOPPED)
                    return;

                BassMix.BASS_Mixer_ChannelSetPosition(_decodingStreamHandle, value, BASSMode.BASS_POS_BYTES);
                if (Un4seen.Bass.Bass.BASS_ErrorGetCode() == BASSError.BASS_ERROR_HANDLE)
                    Un4seen.Bass.Bass.BASS_ChannelSetPosition(_decodingStreamHandle, value, BASSMode.BASS_POS_BYTES);
                BassUtil.ThrowOnBassError();
            }
        }

        #endregion

        #region Events

        public event EventHandler<SyncPoint> Sync;
        public event EventHandler SourceEnded;

        #endregion

        #region Initialisation

        internal BassSource(Uri uri)
        {
            _syncPointSyncProc = SyncPointSyncProc;
            _endedSyncProc = EndedSyncProc;
            Load(uri);
        }

        private void Load(Uri uri)
        {
            // Load MOD/Waveform Respectively.
            if (Un4seen.Bass.Bass.SupportedMusicExtensions.IndexOf(
                Path.GetExtension(uri.AbsolutePath), StringComparison.CurrentCultureIgnoreCase) != -1)
            {
                _decodingStreamBeforeFxHandle = Un4seen.Bass.Bass.BASS_MusicLoad(uri.OriginalString, 0, 0,
                    BASSFlag.BASS_MUSIC_DECODE |
                    BASSFlag.BASS_MUSIC_PRESCAN, 0);
                // open 1st source
            }
            else
            {
                if (uri.IsFile)
                    _decodingStreamBeforeFxHandle = Un4seen.Bass.Bass.BASS_StreamCreateFile(uri.LocalPath, 0, 0,
                        BASSFlag.BASS_STREAM_DECODE |
                        BASSFlag.BASS_SAMPLE_FLOAT);
                else
                    _decodingStreamBeforeFxHandle = Un4seen.Bass.Bass.BASS_StreamCreateURL(uri.AbsoluteUri, 0,
                        BASSFlag.BASS_STREAM_DECODE |
                        BASSFlag.BASS_SAMPLE_FLOAT, null, IntPtr.Zero);
                // open 1st source
            }
            BassUtil.ThrowOnBassError();

            // Attach FX channel
            _decodingStreamHandle = BassFx.BASS_FX_TempoCreate(_decodingStreamBeforeFxHandle,
                BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT);
            BassUtil.ThrowOnBassError();

            // Set info
            BASS_CHANNELINFO ci = Un4seen.Bass.Bass.BASS_ChannelGetInfo(_decodingStreamHandle);
            BassUtil.ThrowOnBassError();
            Channels = ci.chans;
            SampleRate = ci.freq;
            Uri = uri;
            LengthBytes = Un4seen.Bass.Bass.BASS_ChannelGetLength(_decodingStreamHandle, BASSMode.BASS_POS_BYTES);
            BassUtil.ThrowOnBassError();
            double seconds = Un4seen.Bass.Bass.BASS_ChannelBytes2Seconds(_decodingStreamHandle, LengthBytes);
            Length = TimeSpan.FromSeconds(seconds);
            BassUtil.ThrowOnBassError();

            // add end event handler
            Un4seen.Bass.Bass.BASS_ChannelSetSync(_decodingStreamHandle,
                BASSSync.BASS_SYNC_MIXTIME | BASSSync.BASS_SYNC_END,
                0, _endedSyncProc, IntPtr.Zero);
            BassUtil.ThrowOnBassError();
        }

        #endregion

        #region Effects

        public void Resume()
        {
            BassMix.BASS_Mixer_ChannelFlags(_decodingStreamHandle, 0, BASSFlag.BASS_MIXER_PAUSE);
            BassUtil.ThrowOnBassError();
        }

        public void ApplyEffects()
        {
            float pitch;
            float rate;
            float virtualPitch = Effects.Pitch;
            float virtualTempo = Effects.TempoMultiplier;

            if (Effects.ScalingPitch && Effects.ScalingTempo)
            {
                rate = virtualTempo;
                pitch = virtualPitch*(1/virtualTempo);
            }
            else if (Effects.ScalingPitch && !Effects.ScalingTempo)
            {
                rate = virtualPitch;
                pitch = 1.0f;
            }
            else if (Effects.ScalingTempo && !Effects.ScalingPitch)
            {
                rate = virtualTempo;
                pitch = 1.0f;
            }
            else
            {
                rate = 1.0f;
                pitch = 1.0f;
            }

            SetPitch(pitch);
            SetSamplePlaybackRate(rate);

            _log.InfoFormat("Applied FX: VirtualPitch={0} VirtualTempo={1} ActualRate={2}",
                virtualPitch, virtualTempo, rate);
        }

        private void SetSamplePlaybackRate(float rate)
        {
            BASS_CHANNELINFO info = Un4seen.Bass.Bass.BASS_ChannelGetInfo(_decodingStreamHandle);
            BassUtil.ThrowOnBassError();
            if (info != null)
            {
                float newFreq = info.freq*rate;
                if (info.freq != newFreq)
                    _log.InfoFormat("Rate change: {0} -> {1}", info.freq, newFreq);
                Un4seen.Bass.Bass.BASS_ChannelSetAttribute(_decodingStreamHandle,
                    BASSAttribute.BASS_ATTRIB_TEMPO_FREQ,
                    newFreq);
                BassUtil.ThrowOnBassError();
            }
        }

        private void SetPitch(float pitch)
        {
            BASS_CHANNELINFO info = Un4seen.Bass.Bass.BASS_ChannelGetInfo(_decodingStreamHandle);
            BassUtil.ThrowOnBassError();
            if (info != null)
            {
                float newPitch = Effects.PitchFloatToCents(pitch)/100.0f;
                Un4seen.Bass.Bass.BASS_ChannelSetAttribute(_decodingStreamHandle,
                    BASSAttribute.BASS_ATTRIB_TEMPO_PITCH,
                    newPitch);
                BassUtil.ThrowOnBassError();
            }
        }

        #endregion

        #region Sample Data Retrieval

        /// <summary>
        ///     Retrieves floating point sample data.
        /// </summary>
        /// <param name="bytesToRetrieve"></param>
        /// <param name="data"></param>
        /// <returns>The number of bytes set in the data array.</returns>
        public int GetSampleData(int bytesToRetrieve, float[] data)
        {
            int bytesReturned = Un4seen.Bass.Bass.BASS_ChannelGetData(_decodingStreamHandle, data, bytesToRetrieve);
            return bytesReturned;
        }

        /// <summary>
        ///     Retrieves floating point sample data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>The number of samples set in the data array.</returns>
        public int GetSampleData(float[] data)
        {
            int bytesReturned = Un4seen.Bass.Bass.BASS_ChannelGetData(_decodingStreamHandle, data,
                data.Length*sizeof (float));
            return bytesReturned/sizeof (float);
        }

        public void FadeIn(TimeSpan duration)
        {
            Fade(duration, 0f, 1f);
        }

        public void FadeOut(TimeSpan duration)
        {
            Fade(duration, 1f, 0f);

            // Set channel to stop after fade out
            AddSyncPoint(Position.Add(duration), _stopObj, false);
        }

        private void Fade(TimeSpan duration, float startVol, float endVol)
        {
            // Set initial volume
            Un4seen.Bass.Bass.BASS_ChannelSlideAttribute(_decodingStreamHandle,
                BASSAttribute.BASS_ATTRIB_VOL, startVol, 0);
            BassUtil.ThrowOnBassError();
            // Set slider automation
            Un4seen.Bass.Bass.BASS_ChannelSlideAttribute(_decodingStreamHandle,
                BASSAttribute.BASS_ATTRIB_VOL, endVol, (int) duration.TotalMilliseconds);
            BassUtil.ThrowOnBassError();
        }

        #endregion

        #region Sync Points

        public SyncPoint AddSyncPoint(long bytes, object tag, bool whenHeard)
        {
            var syncPoint = new SyncPoint(bytes, tag);
            GCHandle hGc = GCHandle.Alloc(syncPoint);
            IntPtr gcHandlePtr = GCHandle.ToIntPtr(hGc);
            int syncProcHandle;
            if (whenHeard)
                syncProcHandle = BassMix.BASS_Mixer_ChannelSetSync(_decodingStreamHandle,
                    BASSSync.BASS_SYNC_POS, syncPoint.BytePosition,
                    _syncPointSyncProc, gcHandlePtr);
            else
                syncProcHandle = Un4seen.Bass.Bass.BASS_ChannelSetSync(_decodingStreamHandle,
                    BASSSync.BASS_SYNC_POS, syncPoint.BytePosition, _syncPointSyncProc,
                    gcHandlePtr);
            BassUtil.ThrowOnBassError();
            _syncPointHandles.Add(syncPoint, new GcHandleSyncProcHandlePair(syncProcHandle, hGc));
            return syncPoint;
        }

        public SyncPoint AddSyncPoint(int samples, object tag, bool whenHeard)
        {
            long bytes = ConvertSamplesToBytes(samples);
            return AddSyncPoint(bytes, tag, whenHeard);
        }

        public SyncPoint AddSyncPoint(TimeSpan pos, object tag, bool whenHeard)
        {
            long bytes = ConvertSecondsToBytes(pos.TotalSeconds);
            return AddSyncPoint(bytes, tag, whenHeard);
        }

        public void RemoveSyncPoint(SyncPoint syncPoint)
        {
            GcHandleSyncProcHandlePair handlePair = _syncPointHandles[syncPoint];
            Un4seen.Bass.Bass.BASS_ChannelRemoveSync(_decodingStreamHandle, handlePair.SyncProcHandle);
            BassUtil.ThrowOnBassError();
            _syncPointHandles.Remove(syncPoint);
            handlePair.GCHandle.Free();
        }

        private void RemoveAllSyncPoints()
        {
            foreach (SyncPoint sp in _syncPointHandles.Keys.ToArray())
            {
                RemoveSyncPoint(sp);
            }
        }

        private void SyncPointSyncProc(int handle, int channel, int data, IntPtr user)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                GCHandle hGc = GCHandle.FromIntPtr(user);
                var sp = (SyncPoint) hGc.Target;
                if (sp.Tag == _stopObj)
                {
                    _log.InfoFormat("Stopping source.");
                    Un4seen.Bass.Bass.BASS_ChannelStop(channel);
                    BassUtil.ThrowOnBassError();
                }
                else
                {
                    EventHandler<SyncPoint> handler = Sync;
                    if (handler != null)
                        handler(this, sp);
                }
            }, null);
        }

        private void EndedSyncProc(int handle, int channel, int data, IntPtr user)
        {
            ThreadPool.UnsafeQueueUserWorkItem(o =>
            {
                _log.InfoFormat("Source ended/freed.");
                EventHandler handler = SourceEnded;
                if (handler != null)
                    handler(this, EventArgs.Empty);
            }, null);
        }

        #endregion

        #region Unit Conversation

        private long ConvertSamplesToBytes(int samples)
        {
            return samples*sizeof (float)*Channels;
        }

        private long ConvertSecondsToBytes(double seconds)
        {
            long bytes = Un4seen.Bass.Bass.BASS_ChannelSeconds2Bytes(_decodingStreamHandle, seconds);
            BassUtil.ThrowOnBassError();
            return bytes;
        }

        private double ConvertBytesToSeconds(long bytes)
        {
            double secs = Un4seen.Bass.Bass.BASS_ChannelBytes2Seconds(_decodingStreamHandle, bytes);
            BassUtil.ThrowOnBassError();
            return secs;
        }

        #endregion

        # region Internal Bass Mixer Co-operation Code

        internal BASSActive State
        {
            get
            {
                BASSActive status = Un4seen.Bass.Bass.BASS_ChannelIsActive(_decodingStreamHandle);
                BassUtil.ThrowOnBassError();
                return status;
            }
        }

        internal int DecodingStreamHandle
        {
            get { return _decodingStreamHandle; }
        }

        internal int DecodingStreamBeforeFxHandle
        {
            get { return _decodingStreamBeforeFxHandle; }
        }

        internal void RemoveFromMixer()
        {
            BassMix.BASS_Mixer_ChannelRemove(_decodingStreamHandle);
            BassUtil.ThrowOnBassError();
        }

        internal void AddToMixer(int mixerStreamHandle, bool pause)
        {
            BASSFlag flags = BASSFlag.BASS_MIXER_NORAMPIN | BASSFlag.BASS_STREAM_AUTOFREE;
            if (pause)
                flags |= BASSFlag.BASS_MIXER_PAUSE;
            BassMix.BASS_Mixer_StreamAddChannel(mixerStreamHandle, _decodingStreamHandle, flags);
            BassUtil.ThrowOnBassError();
        }

        #endregion

        #region IDispose

        public void Dispose()
        {
            // Free all sync points
            RemoveAllSyncPoints();

            // Free FX and decoding streams
            if (_decodingStreamBeforeFxHandle != 0)
            {
                Un4seen.Bass.Bass.BASS_StreamFree(_decodingStreamBeforeFxHandle);
                BassUtil.ThrowOnBassError();
                _decodingStreamBeforeFxHandle = 0;
                _decodingStreamHandle = 0; // the FX stream is freed when the decoding stream is freed.
            }
        }

        #endregion
    }
}