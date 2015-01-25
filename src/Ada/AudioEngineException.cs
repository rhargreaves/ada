using System;
using System.Runtime.Serialization;

namespace Ada
{
    [Serializable]
    public class AudioEngineException : Exception
    {
        public AudioEngineException()
        {
        }

        public AudioEngineException(string message) : base(message)
        {
        }

        public AudioEngineException(string message, Exception inner) : base(message, inner)
        {
        }

        protected AudioEngineException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
