using System;

namespace JobRunner.Exceptions
{
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
    }
}