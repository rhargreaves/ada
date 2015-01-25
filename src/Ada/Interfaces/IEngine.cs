using System;
using System.Collections.Generic;

namespace Ada.Interfaces
{
    public interface IEngine : IDisposable
    {
        IMixer Mixer { get; }
        ISource CreateSource(Uri uri);
        IList<string> SupportedFileExtensions { get; }
        EngineOptions Options { get; }
        void Init();
        int BufferSizeMs { get; set; }
    }
}