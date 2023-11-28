using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Shared.API.Infrastructure.Models
{

    public class Error
    {
        public Error()
        {
            this.Meta = new object();
            this.Code = string.Empty;
            this.Title = string.Empty;
            this.Detail = string.Empty;
        }

        public Error(string code, string title, string detail) : this()
        {
            this.Code = code;
            this.Title = title;
            this.Detail = detail;
        }

        /// <summary>
        /// Error code
        /// </summary>
        [Required]
        public string Code { get; set; }

        /// <summary>
        /// Error title
        /// </summary>
        [Required]
        public string Title { get; set; }

        /// <summary>
        /// Error detail
        /// </summary>
        [Required]
        public string Detail { get; set; }

        /// <summary>
        /// Optional additional data for specific error types
        /// </summary>
        public object Meta { get; set; }

        public static Error InvalidDateTime()
        {
            return new Error("urn:au-cds:error:cds-all:Field/InvalidDateTime", "Invalid DateTime", "{0} should be valid DateTimeString");
        }

        public static Error InvalidPageSize()
        {
            return new Error("urn:au-cds:error:cds-all:Field/InvalidPageSize", "Invalid Page Size", "Page size not a positive Integer");
        }

        public static Error PageSizeTooLarge()
        {
            return new Error("urn:au-cds:error:cds-all:Field/InvalidPageSize", "Invalid Page Size", "page-size pagination field is greater than the maximum 1000 allowed");
        }

        public static Error InvalidPage()
        {
            return new Error("urn:au-cds:error:cds-all:Field/InvalidPage", "Invalid Page", "Page not a positive Integer");
        }

        public static Error PageOutOfRange()
        {
            return new Error("urn:au-cds:error:cds-all:Field/InvalidPageOutOfRange", "Page Is Out Of Range", string.Empty);
        }
        public static Error PageOutOfRange(int lastPage)
        {
            return new Error("urn:au-cds:error:cds-all:Field/InvalidPage", "Invalid Page", $"page parameter is out of range.  Maximum page is {(lastPage == 0 ? lastPage + 1 : lastPage)}");
        }

        public static Error DataRecipientParticipationNotActive()
        {
            return new Error()
            {
                Code = "urn:au-cds:error:cds-all:Authorisation/AdrStatusNotActive",
                Title = "ADR Status Is Not Active",
                Detail = "ADR status is Inactive",
                Meta = new object()
            };
        }

        public static Error DataRecipientParticipationRemoved()
        {
            return new Error()
            {
                Code = "urn:au-cds:error:cds-all:Authorisation/AdrStatusNotActive",
                Title = "ADR Status Is Not Active",
                Detail = "ADR status is Removed",
                Meta = new object()
            };
        }

        public static Error DataRecipientParticipationSuspended()
        {
            return new Error()
            {
                Code = "urn:au-cds:error:cds-all:Authorisation/AdrStatusNotActive",
                Title = "ADR Status Is Not Active",
                Detail = "ADR status is Suspended"                
            };
        }

        public static Error DataRecipientParticipationRevoked()
        {
            return new Error()
            {
                Code = "urn:au-cds:error:cds-all:Authorisation/AdrStatusNotActive",
                Title = "ADR Status Is Not Active",
                Detail = "ADR status is Revoked"                
            };
        }

        public static Error DataRecipientParticipationSurrendered()
        {
            return new Error()
            {
                Code = "urn:au-cds:error:cds-all:Authorisation/AdrStatusNotActive",
                Title = "ADR Status Is Not Active",
                Detail = "ADR status is Surrendered"
            };
        }

        public static Error DataRecipientSoftwareProductNotActive()
        {
            return new Error("urn:au-cds:error:cds-all:Authorisation/AdrStatusNotActive", "ADR Status Is Not Active", "Software product status is Inactive");
        }
        
        public static Error DataRecipientSoftwareProductStatusRemoved()
        {                             
            return new Error()
            {
                Code = "urn:au-cds:error:cds-all:Authorisation/AdrStatusNotActive",
                Title = "ADR Status Is Not Active",                
                Detail = "Software product status is Removed"                
            };
        }

        public static Error InvalidOpenStatus()
        {
            return new Error("urn:au-cds:error:cds-all:Field/InvalidOpenStatus", "Invalid Open Status", string.Empty);
        }

        public static Error InvalidProductCategory()
        {
            return new Error("urn:au-cds:error:cds-all:Field/InvalidProductCategory", "Invalid Product Category", string.Empty);
        }

        public static Error InvalidField(string description)
        {
            return new Error("urn:au-cds:error:cds-all:Field/Invalid", "Invalid Field", description);
        }

        public static Error InvalidRequest(string description)
        {
            return new Error("invalid_request", string.Empty, description);
        }

        public static Error InvalidRequestObject(string description)
        {
            return new Error("invalid_request_object", string.Empty, description);
        }

        public static Error MissingOpenIdScope(string description)
        {
            return new Error("invalid_request", string.Empty, description);
        }

        public static Error UnknownClient(string description)
        {
            return new Error("invalid_request", string.Empty, description);
        }

        public static Error InvalidClient(string description)
        {
            return new Error("invalid_client", string.Empty, description);
        }

        public static Error MissingRequiredHeader(string? headerName = null)
        {
            return new Error()
            {
                Code = "urn:au-cds:error:cds-all:Header/Missing",
                Title = "Missing Required Header",
                Detail = headerName ?? string.Empty,
                Meta = new object()
            };
        }

        public static Error MissingRequiredXVHeader()
        {
            return new Error()
            {
                Code = "urn:au-cds:error:cds-all:Header/Missing",
                Title = "Missing Required Header",
                Detail = "An API version X-V Header is required but was not specified",
                Meta = new object()
            };
        }

        public static Error InvalidHeader(string? headerName = null)
        {
            return new Error()
            {
                Code = "urn:au-cds:error:cds-all:Header/Invalid",
                Title = "Invalid Header",
                Detail = headerName ?? string.Empty,
                Meta = new object()
            };
        }

        public static Error UnsupportedXVVersion()
        {
            return new Error()
            {
                Code = "urn:au-cds:error:cds-all:Header/UnsupportedVersion",
                Title = "Unsupported Version",
                Detail = $"Version requested is lower than the minimum version or greater than maximum version.",
                Meta = new object()
            };
        }

        public static Error UnsupportedVersion()
        {
            return new Error()
            {
                Code = "urn:au-cds:error:cds-all:Header/UnsupportedVersion",
                Title = "Unsupported Version",
                Detail = string.Empty,
                Meta = new object()
            };
        }

        public static Error InvalidXVVersion()
        {
            return new Error()
            {
                Code = "urn:au-cds:error:cds-all:Header/InvalidVersion",
                Title = "Invalid Version",
                Detail = "Version is not a positive Integer.",
                Meta = new object()
            };
        }

        public static Error InvalidVersion()
        {
            return new Error()
            {
                Code = "urn:au-cds:error:cds-all:Header/InvalidVersion",
                Title = "Invalid Version",
                Detail = string.Empty,
                Meta = new object()
            };
        }

        public static Error NotFound(string? detail = null)
        {            
            return new Error()
            {
                Code = "urn:au-cds:error:cds-all:Resource/NotFound",
                Title = "Resource Not Found",
                Detail = detail ?? string.Empty
            };
        }

        public static Error ConsentNotFound(string industry)
        {
            return new Error()
            {
                Code = $"urn:au-cds:error:cds-all:Authorisation/Unavailable{industry}Account",
                Title = $"Unavailable {industry} Account",
                Detail = string.Empty
            };
        }

        public static Error UnknownError()
        {
            return new Error("Unknown", "Unknown error", string.Empty);
        }

        public static Error InvalidEnergyAccount(string accountId)
        {
            return new Error()
            {
                Code = "urn:au-cds:error:cds-energy:Authorisation/InvalidEnergyAccount",
                Title = "Invalid Energy Account",
                Detail = $"{accountId}"
            };
        }
    }
}
