using System;
using Un4seen.Bass;

namespace Ada.Bass
{
    public static class BassUtil
    {
        internal static void ThrowOnBassError()
        {
            var basserr = Un4seen.Bass.Bass.BASS_ErrorGetCode();
            if (basserr != BASSError.BASS_OK)
            {
                throw new AudioEngineException(Enum.GetName(typeof(BASSError), basserr));
            }
        }
    }
}
