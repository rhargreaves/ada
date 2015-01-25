using System;
using System.Net;
using Ada.Interfaces;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Enc;

namespace Ada.Bass
{
    public class BassServer : IServer
    {
        private readonly ENCODECLIENTPROC _encodeClientProc;
        public BassServer()
        {
            _encodeClientProc = new ENCODECLIENTPROC(EncodeClientProc);
        }

        public IEncoder Encoder { get; set; }

        public void StartListening(IPAddress address, int port,
                          int bufferSize)
        {
            if (!(Encoder is BassEncoder))
                throw new InvalidOperationException("Encoder type not BassEncoder");

            string ipAddressAndPort = string.Format("{0}:{1}", address, port);
            BassEncoder bassEncoder = (BassEncoder)Encoder;
            BassEnc.BASS_Encode_ServerInit(bassEncoder.EncoderHandle,
                                           ipAddressAndPort, bufferSize, bufferSize,
                                           BASSEncodeServer.BASS_ENCODE_SERVER_DEFAULT,
                                           _encodeClientProc, IntPtr.Zero);
            BassUtil.ThrowOnBassError();
        }


        public void StopListening()
        {
            // No API exists in BASS to end server (apparently)
            throw new InvalidOperationException();
        }

        private bool EncodeClientProc(int handle, bool connect, string client, IntPtr headers, IntPtr user)
        {
            if (connect)
            {
                // add same-origin policy
                string[] resHeaders = new string[1] {"Access-Control-Allow-Origin:*"};
                Utils.StringToNullTermAnsi(resHeaders, headers, true);
            }
            return true;
        }
    }
}
