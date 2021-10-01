using System.Collections.Generic;
using System.Linq;

namespace CDR.DataHolder.Domain.ValueObjects
{
	public class AccountProductCategory : ReferenceType<AccountProductCategoryEnum, string>
	{
		public AccountProductCategoryEnum Id { get; set; }
		public string Code { get; set; }

		public static IDictionary<AccountProductCategoryEnum, string> Values
		{
			get
			{
				return new Dictionary<AccountProductCategoryEnum, string>
				{
					{AccountProductCategoryEnum.BusinessLoans, "BUSINESS_LOANS" },
					{AccountProductCategoryEnum.CredAndChrgCards, "CRED_AND_CHRG_CARDS" },
					{AccountProductCategoryEnum.Leases, "LEASES" },
					{AccountProductCategoryEnum.MarginLoans, "MARGIN_LOANS" },
					{AccountProductCategoryEnum.Overdrafts, "OVERDRAFTS" },
					{AccountProductCategoryEnum.PersLoans, "PERS_LOANS" },
					{AccountProductCategoryEnum.RegulatedTrustAccounts, "REGULATED_TRUST_ACCOUNTS" },
					{AccountProductCategoryEnum.ResidentialMortgages, "RESIDENTIAL_MORTGAGES" },
					{AccountProductCategoryEnum.TermDeposits, "TERM_DEPOSITS" },
					{AccountProductCategoryEnum.TradeFinance, "TRADE_FINANCE" },
					{AccountProductCategoryEnum.TransAndSavingsAccounts, "TRANS_AND_SAVINGS_ACCOUNTS" },
					{AccountProductCategoryEnum.TravelCards, "TRAVEL_CARDS" },
				};
			}
		}
	}

	public enum AccountProductCategoryEnum
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