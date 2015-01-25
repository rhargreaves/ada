using System.Configuration;

namespace Ada.Bass
{
    public class BassEngineOptions : EngineOptions
    {
        public string RegistrationEmail { get; set; }
        public string RegistrationKey { get; set; }
        public string PluginDirPath { get; set; }

        public BassEngineOptions()
        {
            RegistrationEmail = ConfigurationManager.AppSettings["BassRegistrationEmail"];
            RegistrationKey =  ConfigurationManager.AppSettings["BassRegistrationKey"];
            PluginDirPath = ConfigurationManager.AppSettings["BassPluginDirPath"];
        }
    }
}