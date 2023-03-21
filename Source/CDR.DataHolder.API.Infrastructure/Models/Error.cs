using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.API.Infrastructure.Models
{

    public class Error
    {
        public Error()
        {
            this.Meta = new object();
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
            return new Error("Field/InvalidDateTime", "Invalid DateTime", "{0} should be valid DateTimeString");
        }

        public static Error InvalidPageSize()
        {
            return new Error("Field/InvalidPageSize", "Invalid Page Size", "Page size not a positive Integer");
        }

        public static Error PageSizeTooLarge()
        {
            return new Error("Field/InvalidPageSizeTooLarge", "Page Size Too Large", string.Empty);
        }

        public static Error InvalidPage()
        {
            return new Error("Field/InvalidPage", "Invalid Page", "Page not a positive Integer");
        }

        public static Error PageOutOfRange()
        {
            return new Error("Field/InvalidPageOutOfRange", "Page Is Out Of Range", string.Empty);
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
            return new Error("Field/InvalidOpenStatus", "Invalid Open Status", string.Empty);
        }

        public static Error InvalidProductCategory()
        {
            return new Error("Field/InvalidProductCategory", "Invalid Product Category", string.Empty);
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

        public static Error MissingHeader(string headerName = null)
        {
            return new Error()
            {
                Code = "urn:au-cds:error:cds-all:Header/Missing",
                Title = "Missing Required Header",
                Detail = headerName == null ? "" : $"The header '{headerName}' is missing.",
                Meta = new object()
            };
        }

        public static Error InvalidHeader(string headerName = null)
        {
            return new Error()
            {
                Code = "urn:au-cds:error:cds-all:Header/Invalid",
                Title = "Invalid Header",
                Detail = headerName == null ? "" : $"The header '{headerName}' is invalid.",
                Meta = new object()
            };
        }

        public static Error UnsupportedXVVersion(int minVersion = 1, int maxVersion = 1)
        {
            return new Error()
            {
                Code = "urn:au-cds:error:cds-all:Header/UnsupportedVersion",
                Title = "Unsupported Version",
                Detail = $"The minimum supported version is {minVersion}. The maximum supported version is {maxVersion}.",
                Meta = new object()
            };
        }

        public static Error UnsupportedVersion()
        {
            return new Error()
            {
                Code = "urn:au-cds:error:cds-all:Header/UnsupportedVersion",
                Title = "Unsupported Version",
                Detail = "",
                Meta = new object()
            };
        }

        public static Error InvalidXVVersion()
        {
            return new Error()
            {
                Code = "urn:au-cds:error:cds-all:Header/InvalidVersion",
                Title = "Invalid Version",
                Detail = "Version header must be a positive integer",
                Meta = new object()
            };
        }

        public static Error InvalidVersion()
        {
            return new Error()
            {
                Code = "urn:au-cds:error:cds-all:Header/InvalidVersion",
                Title = "Invalid Version",
                Detail = "",
                Meta = new object()
            };
        }

        public static Error NotFound(string detail = null)
        {            
            return new Error()
            {
                Code = "urn:au-cds:error:cds-all:Resource/NotFound",
                Title = "Resource Not Found",
                Detail = detail
            };
        }

        public static Error ConsentNotFound()
        {
            return new Error()
            {
                Code = "urn:au-cds:error:cds-all:Authorisation/UnavailableBankingAccount",
                Title = "Unavailable Banking Account",
                Detail = ""
            };
        }

        public static Error UnknownError()
        {
            return new Error("Unknown", "Unknown error", string.Empty);
        }
    }
}
