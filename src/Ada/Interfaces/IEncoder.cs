namespace Ada.Interfaces
{
    public interface IEncoder
    {
        EncoderQuality Quality { get; set; }
        string OutputFilename { get; set; }
        void AttachToMixer(IMixer mixer);
        void DettachFromMixer(IMixer mixer);
    }
}
