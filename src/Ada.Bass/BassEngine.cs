using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Ada.Interfaces;
using Common.Logging;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Winamp;

namespace Ada.Bass
{
    public class BassEngine : IEngine
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Dictionary<int, string> _plugins;
        private BassMixer _mixer;
        private bool _isInit;

        private readonly List<string> _supportedExtensions = new List<string>();
        private readonly BassEngineOptions _options = new BassEngineOptions();

        EngineOptions IEngine.Options {
            get { return _options; }
        }

        #region Initialisation
        public void Init()
        {
            if (_isInit)
                throw new AudioEngineException("Already initialised.");
            _isInit = true;

            // Register Bass.Net
            _log.Debug("Bass Initialising");
            if (string.IsNullOrEmpty(_options.RegistrationEmail))
                throw new InvalidOperationException("RegistrationEmail is null or empty.");
            if (string.IsNullOrEmpty(_options.RegistrationKey))
                throw new InvalidOperationException("RegistrationKey is null or empty.");
            BassNet.Registration(_options.RegistrationEmail, _options.RegistrationKey);

            // Load DLLs from assembly directory
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Un4seen.Bass.Bass.LoadMe(assemblyDir);

            // Initialise
            if (_options.SampleRate == 0)
                throw new InvalidOperationException("Configured Sample Rate is invalid.");
            Un4seen.Bass.Bass.BASS_Init(_options.DecodeOnly ? 0 : -1,
                _options.SampleRate, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
            BassUtil.ThrowOnBassError();

            // Load plugins & log supported formats
            _supportedExtensions.AddRange(Un4seen.Bass.Bass.SupportedStreamExtensions
                                                 .Split(';')
                                                 .Select(e => e.Substring(1).ToLower())); // omit the * char
            _log.Info(string.Concat("Built-in music formats: ", Un4seen.Bass.Bass.SupportedMusicExtensions));
            _log.Info(string.Concat("Built-in stream formats: ", Un4seen.Bass.Bass.SupportedStreamExtensions));
            string pluginDir = _options.PluginDirPath;
            if (string.IsNullOrEmpty(pluginDir) || !Directory.Exists(pluginDir))
            {
                _log.WarnFormat("Configured plugin directory [{0}] is null, empty or does not exist. Setting location to assembly directory [{1}].",
                    pluginDir, assemblyDir);
                pluginDir = assemblyDir;
            }
            LoadPlugins(pluginDir);
            //LoadWinampPlugins();

            // Create Mixer
            if (_options.Channels == 0)
                throw new InvalidOperationException("Configured Number of Channels is invalid.");
            if (!_options.DecodeOnly)
                _mixer = new BassMixer(_options.SampleRate, _options.Channels, this);

            _log.Info("Initialised");
        }

        private void LoadWinampPlugins()
        {
            string[] waPlugIns = BassWinamp.BASS_WINAMP_FindPlugins(@"C:\Program Files (x86)\Winamp2\Winamp2\Plugins",
                BASSWINAMPFindPlugin.BASS_WINAMP_FIND_INPUT);
            foreach (string waPlugIn in waPlugIns)
            {
                _log.InfoFormat("Winamp plugin: {0}", waPlugIn);
                int pluginHandle = BassWinamp.BASS_WINAMP_LoadPlugin(waPlugIn);
                if (pluginHandle == 0)
                {
                    _log.WarnFormat("Could not load plugin.");
                }
                else
                {
                    var exts = Utils.IntPtrToArrayNullTermAnsi(BassWinamp.BASS_WINAMP_GetExtentions(pluginHandle));

                }
            }
        }

        private void LoadPlugins(string pluginDirPath)
        {
            _plugins =
                Un4seen.Bass.Bass.BASS_PluginLoadDirectory(pluginDirPath);
            BassUtil.ThrowOnBassError();
            foreach (int handle in _plugins.Keys)
            {
                string file;
                _plugins.TryGetValue(handle, out file);
                file = Path.GetFileName(file);
                BASS_PLUGININFO pluginInfo = Un4seen.Bass.Bass.BASS_PluginGetInfo(handle);
                BassUtil.ThrowOnBassError();

                BASS_PLUGINFORM[] pluginFormats = pluginInfo.formats;
                foreach (BASS_PLUGINFORM pluginFormat in pluginFormats)
                {
                    // Supported Extensions: Add Plugin's to List
                    _log.Info(pluginFormat.name + " (" + pluginFormat.ctype + ") supports " +
                                           pluginFormat.exts);
                    foreach (string ext in pluginFormat.exts.Split(';'))
                        _supportedExtensions.Add(ext.Substring(1).ToLower()); // omit the * char
                }
            }
        }
        #endregion

        public BassEngineOptions Options
        {
            get { return _options; }
        }

        public IList<string> SupportedFileExtensions
        {
            get { return _supportedExtensions; }
        }

        public IMixer Mixer
        {
            get { return _mixer; }
        }

        public ISource CreateSource(Uri uri)
        {
            return new BassSource(uri);
        }

        public int BufferSizeMs
        {
            get
            {
                int bufferSizeMs = Un4seen.Bass.Bass.BASS_GetConfig(BASSConfig.BASS_CONFIG_BUFFER);
                BassUtil.ThrowOnBassError();
                return bufferSizeMs;
            }
            set
            {
                Un4seen.Bass.Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, value);
                BassUtil.ThrowOnBassError();
            }
        }

        public int SampleRate
        {
            get { return _options.SampleRate; }
        }

        public void Dispose()
        {
            if (_mixer != null)
            {
                _mixer.Dispose();
                _mixer = null;
            }
            Un4seen.Bass.Bass.BASS_Free();
            Un4seen.Bass.Bass.FreeMe();
            GC.SuppressFinalize(this);
        }

        ~BassEngine()
        {
            Dispose();
        }
    }
}
