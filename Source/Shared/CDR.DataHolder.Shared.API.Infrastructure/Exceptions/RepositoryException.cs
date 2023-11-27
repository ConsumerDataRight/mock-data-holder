using System;

namespace CDR.DataHolder.Shared.API.Infrastructure.Exceptions
{
    public class RepositoryException : Exception
    {
        public RepositoryException(string message) : base($"An error occurred : {message}")
        {
        }

        public RepositoryException(string message, Exception ex) : base($"An error occurred : {message}", ex)
        {
        }
    }

}
