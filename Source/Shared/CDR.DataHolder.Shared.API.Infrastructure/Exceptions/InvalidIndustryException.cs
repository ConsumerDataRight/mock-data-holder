using System;

namespace CDR.DataHolder.Shared.API.Infrastructure.Exceptions
{
    public class InvalidIndustryException : Exception
    {
        public InvalidIndustryException()
            : base($"Industry is either empty or not supported")
        {
        }

        public InvalidIndustryException(Exception ex)
            : base("Industry is either empty or not supported", ex)
        {
        }
    }
}
