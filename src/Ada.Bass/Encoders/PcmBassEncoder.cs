using System.Reflection;
using Common.Logging;

namespace Ada.Bass.Encoders
{
    public class PcmBassEncoder : BassEncoder
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override void OnMixerAttached(int sampleRate, out string encoderCmdLine)
        {
            encoderCmdLine = null; // Tells BASS to use PCM format.
            _log.Debug("PCM encoding selected.");
        }
    }
}