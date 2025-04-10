﻿using CDR.DataHolder.Shared.Domain.Extensions;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Shared.Domain.Models
{
    public class Error
    {
        public Error()
            : this(string.Empty, string.Empty, string.Empty, string.Empty)
        {
        }

        public Error(string code, string title, string detail)
            : this(code, title, detail, string.Empty)
        {
        }

        public Error(string code, string title, string detail, string metaUrn)
        {
            Code = code;
            Title = title;
            Detail = detail;
            Meta = metaUrn.IsNullOrWhiteSpace() ? null : new MetaError(metaUrn);
        }

        /// <summary>
        /// Error code.
        /// </summary>
        [Required]
        public string Code { get; set; }

        /// <summary>
        /// Error title.
        /// </summary>
        [Required]
        public string Title { get; set; }

        /// <summary>
        /// Error detail.
        /// </summary>
        [Required]
        public string Detail { get; set; }

        /// <summary>
        /// Optional additional data for specific error types.
        /// </summary>
        public MetaError? Meta { get; set; }
    }
}
