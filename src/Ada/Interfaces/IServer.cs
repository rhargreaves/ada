using System.Net;

namespace Ada.Interfaces
{
    public interface IServer
    {
        IEncoder Encoder { get; set; }
        void StartListening(IPAddress address, int port, int bufferSize);
        void StopListening();
    }
}
