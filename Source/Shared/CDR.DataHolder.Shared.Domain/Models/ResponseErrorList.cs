using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace CDR.DataHolder.Shared.Domain.Models
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
            Errors = new List<Error>();
        }

        public ResponseErrorList(Error error)
        {
            Errors = new List<Error>() { error };
        }

        public ResponseErrorList(string errorCode, string errorTitle, string errorDetail)
        {
            var error = new Error(errorCode, errorTitle, errorDetail);
            Errors = new List<Error>() { error };
        }

        /// <summary>
        /// Add unexpected error to the response error list
        /// </summary>
        public ResponseErrorList AddUnexpectedError(string message)
        {
            Errors.Add(new Error(Constants.ErrorCodes.Cds.UnexpectedError, Constants.ErrorTitles.UnexpectedError, message));
            return this;
        }

        /// <summary>
        /// Add invalid industry error to the response error list
        /// </summary>
        public ResponseErrorList AddInvalidIndustry()
        {
            Errors.Add(new Error(Constants.ErrorCodes.Cds.InvalidField, Constants.ErrorTitles.InvalidField, "industry"));
            return this;
        }

        // Return Unsupported Version
        public ResponseErrorList AddInvalidXVUnsupportedVersion()
        {
            Errors.Add(new Error(Constants.ErrorCodes.Cds.UnsupportedVersion, Constants.ErrorTitles.UnsupportedVersion, "Requested version is lower than the minimum version or greater than maximum version."));
            return this;
        }

        // Return Invalid Version
        public ResponseErrorList AddInvalidXVInvalidVersion()
        {
            Errors.Add(new Error(Constants.ErrorCodes.Cds.InvalidVersion, Constants.ErrorTitles.InvalidVersion, "Version is not a positive Integer."));
            return this;
        }

        public ResponseErrorList AddInvalidXVMissingRequiredHeader()
        {
            Errors.Add(new Error(Constants.ErrorCodes.Cds.MissingRequiredHeader, Constants.ErrorTitles.MissingRequiredHeader, "An API version x-v header is required, but was not specified."));
            return this;
        }

        public ResponseErrorList AddUnexpectedError()
        {
            Errors.Add(new Error(Constants.ErrorCodes.Cds.UnexpectedError, Constants.ErrorTitles.UnexpectedError, "An unexpected exception occurred while processing the request."));
            return this;
        }

        public ResponseErrorList AddInvalidConsentArrangement(string arrangementId)
        {
            Errors.Add(new Error(Constants.ErrorCodes.Cds.InvalidConsentArrangement, Constants.ErrorTitles.InvalidConsentArrangement, arrangementId));
            return this;
        }

        public ResponseErrorList AddMissingRequiredHeader(string headerName)
        {
            Errors.Add(new Error(Constants.ErrorCodes.Cds.MissingRequiredHeader, Constants.ErrorTitles.MissingRequiredHeader, headerName));
            return this;
        }

        public ResponseErrorList AddMissingRequiredField(string headerName)
        {
            Errors.Add(new Error(Constants.ErrorCodes.Cds.MissingRequiredField, Constants.ErrorTitles.MissingRequiredField, headerName));
            return this;
        }

        public ResponseErrorList AddInvalidField(string fieldName)
        {
            Errors.Add(new Error(Constants.ErrorCodes.Cds.InvalidField, Constants.ErrorTitles.InvalidField, fieldName));
            return this;
        }

        public ResponseErrorList AddInvalidHeader(string headerName)
        {
            Errors.Add(new Error(Constants.ErrorCodes.Cds.InvalidHeader, Constants.ErrorTitles.InvalidHeader, headerName));
            return this;
        }

        public ResponseErrorList AddInvalidDateTime()
        {
            Errors.Add(new Error(Constants.ErrorCodes.Cds.InvalidDateTime, Constants.ErrorTitles.InvalidDateTime, "{0} should be valid DateTimeString"));
            return this;
        }

        public ResponseErrorList AddPageOutOfRange()
        {
            Errors.Add(new Error(Constants.ErrorCodes.Cds.InvalidField, Constants.ErrorTitles.InvalidField, "Page is out of range"));
            return this;
        }

        public ResponseErrorList AddPageOutOfRange(int lastPage) //This matches CDS better
        {
            Errors.Add(new Error(Constants.ErrorCodes.Cds.InvalidPage, Constants.ErrorTitles.InvalidPage, $"Page parameter is out of range.  Maximum page is {(lastPage == 0 ? lastPage + 1 : lastPage)}"));
            return this;
        }

        public static Error InvalidField(string fieldName)
        {
            return new Error(Constants.ErrorCodes.Cds.InvalidField, Constants.ErrorTitles.InvalidField, fieldName);
        }

        public static Error InvalidDateTime()
        {
            return new Error(Constants.ErrorCodes.Cds.InvalidDateTime, Constants.ErrorTitles.InvalidDateTime, "{0} should be valid DateTimeString");
        }

        public static Error InvalidPageSize()
        {
            return new Error(Constants.ErrorCodes.Cds.InvalidPageSize, Constants.ErrorTitles.InvalidPageSize, "Page size not a positive Integer");
        }

        public static Error PageSizeTooLarge() //TODO: Check CDS/RAAP for consistency with standards
        {
            return new Error(Constants.ErrorCodes.Cds.InvalidField, Constants.ErrorTitles.InvalidField, "Page size too large");
        }

        public static Error PageSizeTooLarge_MDH() //TODO: Check CDS/RAAP for consistency with standards
        {
            return new Error("urn:au-cds:error:cds-all:Field/InvalidPageSize", "Invalid Page Size", "page-size pagination field is greater than the maximum 1000 allowed");
        }

        public static Error InvalidPage()
        {
            return new Error(Constants.ErrorCodes.Cds.InvalidField, Constants.ErrorTitles.InvalidField, "Page not a positive integer");
        }

        public static Error PageOutOfRange()
        {
            return new Error(Constants.ErrorCodes.Cds.InvalidField, Constants.ErrorTitles.InvalidField, "Page is out of range");
        }

        public static Error DataRecipientParticipationNotActive()
        {
            return new Error(Constants.ErrorCodes.Cds.AdrStatusNotActive, Constants.ErrorTitles.ADRStatusNotActive, string.Empty);
        }

        public static Error DataRecipientSoftwareProductNotActive()
        {
            return new Error(Constants.ErrorCodes.Cds.AdrStatusNotActive, Constants.ErrorTitles.ADRStatusNotActive, string.Empty);
        }

        public static Error InvalidResource(string softwareProductId)
        {
            return new Error(Constants.ErrorCodes.Cds.InvalidResource, Constants.ErrorTitles.InvalidResource, softwareProductId);
        }

        public static Error InvalidSoftwareProduct(string softwareProductId)
        {
            return new Error(Constants.ErrorCodes.Cds.InvalidSoftwareProduct, Constants.ErrorTitles.InvalidSoftwareProduct, softwareProductId);
        }

        public static Error NotFound()
        {
            return new Error(Constants.ErrorCodes.Cds.ResourceNotFound, Constants.ErrorTitles.ResourceNotFound, string.Empty);
        }

        public ResponseErrorList AddResourceNotFound(string detail)
        {
            Errors.Add(new Error(Constants.ErrorCodes.Cds.ResourceNotFound, Constants.ErrorTitles.ResourceNotFound, detail));
            return this;
        }

        public ResponseErrorList AddConsentNotFound(string? industry)
        {
            //TODO: Improve this later
            Errors.Add(new Error()
            {
                Code = $"urn:au-cds:error:cds-all:Authorisation/Unavailable{industry}Account",
                Title = $"Unavailable {industry} Account",
                Detail = string.Empty
            });
            return this;
        }

        public ResponseErrorList AddUnknownError()
        {
            //TODO: Improve this later (should be unexpected error)
            Errors.Add(new Error("Unknown", "Unknown error", string.Empty));
            return this;
        }

        public ResponseErrorList AddInvalidEnergyAccount(string accountId)
        {
            //TODO: Improve this later
            Errors.Add(new Error()
            {
                Code = "urn:au-cds:error:cds-energy:Authorisation/InvalidEnergyAccount",
                Title = "Invalid Energy Account",
                Detail = $"{accountId}"
            });
            return this;
        }

        public static Error InvalidOpenStatus() //TODO: Improve later
        {
            return new Error("urn:au-cds:error:cds-all:Field/InvalidOpenStatus", "Invalid Open Status", string.Empty);
        }

        public static Error InvalidProductCategory() //TODO: Improve later
        {
            return new Error("urn:au-cds:error:cds-all:Field/InvalidProductCategory", "Invalid Product Category", string.Empty);
        }
    }
}
