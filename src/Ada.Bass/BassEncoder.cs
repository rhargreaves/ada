using System;
using System.Reflection;
using Ada.Interfaces;
using Common.Logging;
using Un4seen.Bass.AddOn.Enc;

namespace Ada.Bass
{
    public abstract class BassEncoder : IEncoder
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public EncoderQuality Quality { get; set; }
        public string OutputFilename { get; set; }

        internal int EncoderHandle { get; private set; }

        public void AttachToMixer(IMixer mixer)
        {
            // Get BassMixer reference; get mixing sample rate
            BassMixer bassMixer = mixer as BassMixer;
            if (bassMixer == null)
                throw new ArgumentException("BassMixer not provided.", "mixer");
            int sampleRate = ((BassEngine)bassMixer.Engine).SampleRate;
            string encoderCmdLine;

            // Get encoder command line from overriden implementation
            _log.DebugFormat("Sample rate is {0} Hz", sampleRate);
            OnMixerAttached(sampleRate, out encoderCmdLine);
            if (encoderCmdLine != null)
            {
                _log.DebugFormat("External encoder will be ran with command line {0}",
                               encoderCmdLine);
            }
            else
            {
                _log.DebugFormat("No external encoder. Will use PCM.");
            }

            // Start encoder
             _log.Info("Starting encoder...");
            BASSEncode flags = BASSEncode.BASS_ENCODE_NOHEAD | BASSEncode.BASS_ENCODE_FP_16BIT;
            if (encoderCmdLine == null)
                flags |= BASSEncode.BASS_ENCODE_PCM;
            EncoderHandle = BassEnc.BASS_Encode_Start(bassMixer.MixerStreamHandle,
                encoderCmdLine, flags, null, IntPtr.Zero);
            BassUtil.ThrowOnBassError();
            _log.Info("Encoder started.");
        }

        public abstract void OnMixerAttached(int sampleRate, out string encoderCmdLine);

        public void DettachFromMixer(IMixer mixer)
        {
            _log.Info("Stopping encoder...");
            BassEnc.BASS_Encode_Stop(EncoderHandle);
            BassUtil.ThrowOnBassError();
            _log.Info("Encoder stopped.");
        }
    }
}
