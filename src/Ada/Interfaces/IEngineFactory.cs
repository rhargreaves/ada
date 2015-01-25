namespace Ada.Interfaces
{
    public interface IEngineFactory
    {
        IEngine Get(EngineOptions options);
    }
}