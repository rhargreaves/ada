namespace Ada.Interfaces
{
    public interface IEncoderFactory
    {
        IEncoder Create(EncoderFormat format);
    }
}