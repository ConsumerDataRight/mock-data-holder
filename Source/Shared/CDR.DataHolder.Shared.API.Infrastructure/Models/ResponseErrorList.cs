using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CDR.DataHolder.Shared.API.Infrastructure.Models
{
    public class ResponseErrorList
    {
        [Required]
        public List<Error> Errors { get; set; }

        public bool HasErrors()
        {
            return Errors != null && Errors.Any();
        }

        public ResponseErrorList()
        {
            this.Errors = new List<Error>();
        }

        public ResponseErrorList(Error error)
        {
            this.Errors = new List<Error>() { error };
        }

        public ResponseErrorList(string errorCode, string errorTitle, string errorDetail)
        {
            var error = new Error(errorCode, errorTitle, errorDetail);
            this.Errors = new List<Error>() { error };
        }

    }
}
