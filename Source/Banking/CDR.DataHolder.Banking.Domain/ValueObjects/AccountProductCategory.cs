using CDR.DataHolder.Shared.Domain.ValueObjects;
using System.Collections.Generic;

namespace CDR.DataHolder.Banking.Domain.ValueObjects
{
    public class AccountProductCategory : ReferenceType<ProductCategory, string>
    {
        public ProductCategory Id { get; set; }

        public string? Code { get; set; }

        public static IDictionary<ProductCategory, string> Values
        {
            get
            {
                return new Dictionary<ProductCategory, string>
                {
                    { ProductCategory.BusinessLoans, "BUSINESS_LOANS" },
                    { ProductCategory.CredAndChrgCards, "CRED_AND_CHRG_CARDS" },
                    { ProductCategory.Leases, "LEASES" },
                    { ProductCategory.MarginLoans, "MARGIN_LOANS" },
                    { ProductCategory.Overdrafts, "OVERDRAFTS" },
                    { ProductCategory.PersLoans, "PERS_LOANS" },
                    { ProductCategory.RegulatedTrustAccounts, "REGULATED_TRUST_ACCOUNTS" },
                    { ProductCategory.ResidentialMortgages, "RESIDENTIAL_MORTGAGES" },
                    { ProductCategory.TermDeposits, "TERM_DEPOSITS" },
                    { ProductCategory.TradeFinance, "TRADE_FINANCE" },
                    { ProductCategory.TransAndSavingsAccounts, "TRANS_AND_SAVINGS_ACCOUNTS" },
                    { ProductCategory.TravelCards, "TRAVEL_CARDS" },
                };
            }
        }
    }

    public enum ProductCategory
    {
        Unknown = 0,
        BusinessLoans = 1,
        CredAndChrgCards = 2,
        Leases = 3,
        MarginLoans = 4,
        Overdrafts = 5,
        PersLoans = 6,
        RegulatedTrustAccounts = 7,
        ResidentialMortgages = 8,
        TermDeposits = 9,
        TradeFinance = 10,
        TransAndSavingsAccounts = 11,
        TravelCards = 12
    }
}
