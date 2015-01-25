using System;
using Ada.Bass.Encoders;
using Ada.Interfaces;

namespace Ada.Bass
{
    public class BassEncoderFactory : IEncoderFactory
    {
        public IEncoder Create(EncoderFormat format)
        {
            switch (format)
            {
                case EncoderFormat.Mp3:
                    return new LameBassEncoder();
                case EncoderFormat.Pcm:
                    return new PcmBassEncoder();
                default:
                    throw new InvalidOperationException(
                        string.Format("{0} not currently supported as a BASS encoder format.",
                                      format));
            }
        }
    }
}
