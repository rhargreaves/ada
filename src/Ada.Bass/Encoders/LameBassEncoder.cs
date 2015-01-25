using System;
using System.Configuration;
using System.Reflection;
using Common.Logging;

namespace Ada.Bass.Encoders
{
    public class LameBassEncoder : BassEncoder
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override void OnMixerAttached(int sampleRateHz, out string encoderCmdLine)
        {
            string lameExePath = ConfigurationManager.AppSettings["LameExePath"];
            if (string.IsNullOrEmpty(lameExePath))
                throw new InvalidOperationException("LameExePath missing from appSettings");
            _log.DebugFormat("LameExePath is {0}. Quality specified is {1}",
                lameExePath, Quality);
            encoderCmdLine = GenerateLameCommandLine(lameExePath, sampleRateHz, Quality);
        }

        private string GenerateLameCommandLine(string lameExePath, int sampleRateHz, EncoderQuality quality)
        {
            // Select bitrate
            int bitRate;
            switch (quality)
            {
                case EncoderQuality.Lowest:
                    bitRate = 96;
                    break;
                case EncoderQuality.Low:
                    bitRate = 128;
                    break;
                case EncoderQuality.Medium:
                    bitRate = 192;
                    break;
                case EncoderQuality.High:
                    bitRate = 256;
                    break;
                case EncoderQuality.Highest:
                    bitRate = 320;
                    break;
                default:
                    throw new InvalidOperationException("Invalid Quality value");
            }
            _log.DebugFormat("Bitrate will be {0} kbps", bitRate);
            string outputArgument = OutputFilename ?? "-";
            string cmdLine = string.Format("\"{3}\" -r -s {2} -b {0} {1}",
                bitRate, outputArgument, (double) sampleRateHz/1000d, lameExePath);
            return cmdLine;
        }
    }
}