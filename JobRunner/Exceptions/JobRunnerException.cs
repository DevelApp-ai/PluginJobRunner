using System;
using System.Runtime.Serialization;

namespace JobRunner.Exceptions
{
    [Serializable]
    internal class JobRunnerException : Exception
    {
        public JobRunnerException()
        {
        }

        public JobRunnerException(string message) : base(message)
        {
        }

        public JobRunnerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected JobRunnerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}