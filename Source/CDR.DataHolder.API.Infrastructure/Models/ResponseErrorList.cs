using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CDR.DataHolder.API.Infrastructure.Models
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

        /// <summary>
        /// Add invalid industry error to the response error list
        /// </summary>
        /// <param name="meta"></param>
        //public ResponseErrorList InvalidIndustry()
        //{
        //    Errors.Add(Error.InvalidIndustry());
        //    return this;
        //}

        ///// <summary>
        ///// Add invalid x-v error to the response error list
        ///// </summary>
        ///// <param name="meta"></param>
        //public ResponseErrorList InvalidVersion()
        //{
        //    Errors.Add(Error.InvalidVersion());
        //    return this;
        //}

        ///// <summary>
        ///// Add unsupported x-v error to the response error list.
        ///// </summary>
        ///// <param name="meta"></param>
        //public ResponseErrorList UnsupportedVersion()
        //{
        //    Errors.Add(Error.UnsupportedVersion());
        //    return this;
        //}

        ///// <summary>
        ///// Add invalid header error to the list.
        ///// </summary>
        //public ResponseErrorList InvalidHeader(string headerName = null)
        //{
        //    Errors.Add(Error.InvalidHeader(headerName));
        //    return this;
        //}

        ///// <summary>
        ///// Add missing header error to the list.
        ///// </summary>
        //public ResponseErrorList MissingHeader(string headerName = null)
        //{
        //    Errors.Add(Error.MissingHeader(headerName));
        //    return this;
        //}
    }
}
