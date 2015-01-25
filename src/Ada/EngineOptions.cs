using System.Configuration;

namespace Ada
{
    public class EngineOptions
    {
        public bool DecodeOnly { get; set; }
        public int SampleRate { get; set; }
        public int Channels { get; set; }
        public int BufferSizeMs { get; set; }

        public EngineOptions()
        {
            int sampleRate;
            if(int.TryParse(ConfigurationManager.AppSettings["AudioSampleRate"], out sampleRate))
                SampleRate = sampleRate;

            int channels;
            if(int.TryParse(ConfigurationManager.AppSettings["AudioChannels"], out channels)) 
                Channels = channels;
        }
    }
}