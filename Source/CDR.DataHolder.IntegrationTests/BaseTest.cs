using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using CDR.DataHolder.API.Infrastructure.IdPermanence;
using CDR.DataHolder.IntegrationTests.Extensions;
using CDR.DataHolder.IntegrationTests.Infrastructure;
using CDR.DataHolder.IntegrationTests.Infrastructure.API2;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Xunit;

#nullable enable

namespace CDR.DataHolder.IntegrationTests
{
    public static class TokenTypeExtensions
    {
        public static string UserId(this BaseTest.TokenType tokenType)
        {
            return tokenType switch
            {
                BaseTest.TokenType.JANE_WILSON => BaseTest.USERID_JANEWILSON,
                BaseTest.TokenType.STEVE_KENNEDY => BaseTest.USERID_STEVEKENNEDY,
                BaseTest.TokenType.DEWAYNE_STEVE => BaseTest.USERID_DEWAYNESTEVE,
                BaseTest.TokenType.BUSINESS_1 => BaseTest.USERID_BUSINESS1,
                BaseTest.TokenType.BUSINESS_2 => BaseTest.USERID_BUSINESS2,
                BaseTest.TokenType.BEVERAGE => BaseTest.USERID_BEVERAGE,
                BaseTest.TokenType.KAMILLA_SMITH => BaseTest.USERID_KAMILLASMITH,
                _ => throw new ArgumentException($"{nameof(UserId)}")
            };
        }

        public static string AllAccountIds(this BaseTest.TokenType tokenType)
        {
            return tokenType switch
            {
                BaseTest.TokenType.JANE_WILSON => BaseTest.ACCOUNTIDS_ALL_JANE_WILSON,
                BaseTest.TokenType.STEVE_KENNEDY => BaseTest.ACCOUNTIDS_ALL_STEVE_KENNEDY,
                BaseTest.TokenType.DEWAYNE_STEVE => BaseTest.ACCOUNTIDS_ALL_DEWAYNE_STEVE,
                BaseTest.TokenType.BUSINESS_1 => BaseTest.ACCOUNTIDS_ALL_BUSINESS1,
                BaseTest.TokenType.BUSINESS_2 => BaseTest.ACCOUNTIDS_ALL_BUSINESS2,
                BaseTest.TokenType.BEVERAGE => BaseTest.ACCOUNTIDS_ALL_BEVERAGE,
                BaseTest.TokenType.KAMILLA_SMITH => BaseTest.ACCOUNTIDS_ALL_KAMILLA_SMITH,
                _ => throw new ArgumentException($"{nameof(AllAccountIds)}")
            };
        }
    }

    // Put all tests in same collection because we need them to run sequentially since some tests are mutating DB.
    [Collection("IntegrationTests")]
    [TestCaseOrderer("CDR.DataHolder.IntegrationTests.XUnit.Orderers.AlphabeticalOrderer", "CDR.DataHolder.IntegrationTests")]
    abstract public class BaseTest
    {
        // public const int TOKEN_EXPIRY_SECONDS = 300;
        public static int TOKEN_EXPIRY_SECONDS => Int32.Parse(Configuration["AccessTokenLifetimeSeconds"]);

        // VSCode slows on excessively long lines, splitting string constant into smaller lines.
        public const string DATAHOLDER_ACCESSTOKEN_EXPIRED =
            @"eyJhbGciOiJQUzI1NiIsImtpZCI6IjczQUVGQ0FGODA3NjUyQTQ2RTMzMTZEQjQ3RTkwNUU3QjcyNjUyQjIiLCJ0eXAiOiJhdCtqd3QiLCJ4NXQiOiJjNjc4cjRCMlVxUnVNeGJiUi1rRjU3Y21VckkifQ.eyJuYmYiOjE2MjM4MjAyNjEsImV4cCI6MTYyMzgyMDU2MSwiaXNzIjoiaHR0cHM6Ly9sb2NhbG" +
            @"hvc3Q6ODAwMSIsImNsaWVudF9pZCI6ImM2MzI3Zjg3LTY4N2EtNDM2OS05OWE0LWVhYWNkM2JiODIxMCIsImNsaWVudF9wb2xpY3lfdXJpIjoiaHR0cHM6Ly9tb2Nrc29mdHdhcmUvbXlidWRnZXRhcHAvcG9saWN5IiwiY2xpZW50X3Rva2VuX2VuZHBvaW50X2F1dGhfbWV0aG9kIjoicHJpdmF0ZV9rZXlfa" +
            @"nd0IiwiY2xpZW50X2xvZ29fdXJpIjoiaHR0cHM6Ly9tb2Nrc29mdHdhcmUvbXlidWRnZXRhcHAvaW1nL2xvZ28ucG5nIiwiY2xpZW50X3NvZnR3YXJlX3N0YXRlbWVudCI6ImV5SmhiR2NpT2lKUVV6STFOaUlzSW10cFpDSTZJalUwTWtFNVFqa3hOakF3TkRnNE1EZzRRMFEwUkRneE5qa3hOa0U1UmpRME9E" +
            @"aEVSREkyTlRFaUxDSjBlWEFpT2lKS1YxUWlmUS5ld29nSUNKeVpXTnBjR2xsYm5SZlltRnpaVjkxY21raU9pQWlhSFIwY0hNNkx5OWhjR2t1Ylc5amEzTnZablIzWVhKbEwyMTVZblZrWjJWMFlYQndJaXdLSUNBaWJHVm5ZV3hmWlc1MGFYUjVYMmxrSWpvZ0lqRTRZamMxWVRjMkxUVTRNakV0TkdNNVpTMWl" +
            @"ORFkxTFRRM01Ea3lPVEZqWmpCbU5DSXNDaUFnSW14bFoyRnNYMlZ1ZEdsMGVWOXVZVzFsSWpvZ0lrMXZZMnNnVTI5bWRIZGhjbVVnUTI5dGNHRnVlU0lzQ2lBZ0ltbHpjeUk2SUNKalpISXRjbVZuYVhOMFpYSWlMQW9nSUNKcFlYUWlPaUF4TmpJek9ESXdNalExTEFvZ0lDSmxlSEFpT2lBeE5qSXpPREl3T0" +
            @"RRMUxBb2dJQ0pxZEdraU9pQWlaRFkyWXpFM1pqSXhOMlkxTkdJMlpEazVaVGd5WVdOak5tUTNOVEprTnpnaUxBb2dJQ0p2Y21kZmFXUWlPaUFpWm1aaU1XTTRZbUV0TWpjNVpTMDBOR1E0TFRrMlpqQXRNV0pqTXpSaE5tSTBNelptSWl3S0lDQWliM0puWDI1aGJXVWlPaUFpVFc5amF5QkdhVzVoYm1ObElGU" +
            @"nZiMnh6SWl3S0lDQWlZMnhwWlc1MFgyNWhiV1VpT2lBaVRYbENkV1JuWlhSSVpXeHdaWElpTEFvZ0lDSmpiR2xsYm5SZlpHVnpZM0pwY0hScGIyNGlPaUFpUVNCd2NtOWtkV04wSUhSdklHaGxiSEFnZVc5MUlHMWhibUZuWlNCNWIzVnlJR0oxWkdkbGRDSXNDaUFnSW1Oc2FXVnVkRjkxY21raU9pQWlhSFIw" +
            @"Y0hNNkx5OXRiMk5yYzI5bWRIZGhjbVV2YlhsaWRXUm5aWFJoY0hBaUxBb2dJQ0p5WldScGNtVmpkRjkxY21seklqb2dXd29nSUNBZ0ltaDBkSEJ6T2k4dllYQnBMbTF2WTJ0emIyWjBkMkZ5WlM5dGVXSjFaR2RsZEdGd2NDOWpZV3hzWW1GamF5SXNDaUFnSUNBaWFIUjBjSE02THk5aGNHa3ViVzlqYTNOdlp" +
            @"uUjNZWEpsTDIxNVluVmtaMlYwWVhCd0wzSmxkSFZ5YmlJS0lDQmRMQW9nSUNKc2IyZHZYM1Z5YVNJNklDSm9kSFJ3Y3pvdkwyMXZZMnR6YjJaMGQyRnlaUzl0ZVdKMVpHZGxkR0Z3Y0M5cGJXY3ZiRzluYnk1d2JtY2lMQW9nSUNKMGIzTmZkWEpwSWpvZ0ltaDBkSEJ6T2k4dmJXOWphM052Wm5SM1lYSmxMMj" +
            @"E1WW5Wa1oyVjBZWEJ3TDNSbGNtMXpJaXdLSUNBaWNHOXNhV041WDNWeWFTSTZJQ0pvZEhSd2N6b3ZMMjF2WTJ0emIyWjBkMkZ5WlM5dGVXSjFaR2RsZEdGd2NDOXdiMnhwWTNraUxBb2dJQ0pxZDJ0elgzVnlhU0k2SUNKb2RIUndjem92TDJ4dlkyRnNhRzl6ZERvM01EQTJMMnh2YjNCaVlXTnJMMDF2WTJ0R" +
            @"VlYUmhVbVZqYVhCcFpXNTBTbmRyY3lJc0NpQWdJbkpsZG05allYUnBiMjVmZFhKcElqb2dJbWgwZEhCek9pOHZZWEJwTG0xdlkydHpiMlowZDJGeVpTOXRlV0oxWkdkbGRHRndjQzl5WlhadmEyVWlMQW9nSUNKemIyWjBkMkZ5WlY5cFpDSTZJQ0pqTmpNeU4yWTROeTAyT0RkaExUUXpOamt0T1RsaE5DMWxZ" +
            @"V0ZqWkROaVlqZ3lNVEFpTEFvZ0lDSnpiMlowZDJGeVpWOXliMnhsY3lJNklDSmtZWFJoTFhKbFkybHdhV1Z1ZEMxemIyWjBkMkZ5WlMxd2NtOWtkV04wSWl3S0lDQWljMk52Y0dVaU9pQWliM0JsYm1sa0lIQnliMlpwYkdVZ1ltRnVhenBoWTJOdmRXNTBjeTVpWVhOcFl6cHlaV0ZrSUdKaGJtczZZV05qYjN" +
            @"WdWRITXVaR1YwWVdsc09uSmxZV1FnWW1GdWF6cDBjbUZ1YzJGamRHbHZibk02Y21WaFpDQmlZVzVyT25CaGVXVmxjenB5WldGa0lHSmhibXM2Y21WbmRXeGhjbDl3WVhsdFpXNTBjenB5WldGa0lHTnZiVzF2YmpwamRYTjBiMjFsY2k1aVlYTnBZenB5WldGa0lHTnZiVzF2YmpwamRYTjBiMjFsY2k1a1pYUm" +
            @"hhV3c2Y21WaFpDQmpaSEk2Y21WbmFYTjBjbUYwYVc5dUlncDkuYzZqLTVxZkVvQmR2eWh5cV9tcDlNcUk1eXF0NjR6dFA3cHdyeDNacm5XRFlvanM1d0V2YnpVWk95c2pGQjFYZFRFNkRqUzlaNnBNdGI1bHQwc195ZGdOX3BsaUtjR1RyOXpOS3FUVFpOTThwTnRWa1lzZ3RMd1pjd0xiZHBzd0N6S2xjcDhFW" +
            @"m5QYk5mWTVxV08wV28wb0Njci1LQXI2ekloSmN4ODFCalpIZFpHOUIwZ09PSDJoTHNqcmhtTFpDNW9takJ2VTRfSHdYSkJLNDhBS2V3S1lxdnVaZjgtaDBvOEo2SjFCalN3VWNYc0xZdU43SkhRMFNkSFV3cWVmYVlBdFlUUFJ2eHpmR0Q2MEF3Tm5rSkgyRi1OVUkxZ2F1eWgxVmtfQVFYXy1EQ2tWSXhIeE9f" +
            @"MjNSVEpLUUg5VHM5ckw5R2JSeFlNanh0cF8tVlpDQzJBIiwiY2xpZW50X3NvZnR3YXJlX2lkIjoiYzYzMjdmODctNjg3YS00MzY5LTk5YTQtZWFhY2QzYmI4MjEwIiwiY2xpZW50X2FwcGxpY2F0aW9uX3R5cGUiOiJ3ZWIiLCJjbGllbnRfY2xpZW50X2lkX2lzc3VlZF9hdCI6IjE2MjM4MjAyNTYiLCJjbGl" +
            @"lbnRfdG9rZW5fZW5kcG9pbnRfYXV0aF9zaWduaW5nX2FsZyI6IlBTMjU2IiwiY2xpZW50X2lkX3Rva2VuX2VuY3J5cHRlZF9yZXNwb25zZV9hbGciOiJSU0EtT0FFUCIsImNsaWVudF9pZF90b2tlbl9lbmNyeXB0ZWRfcmVzcG9uc2VfZW5jIjoiQTI1NkdDTSIsImNsaWVudF9pZF90b2tlbl9zaWduZWRfcm" +
            @"VzcG9uc2VfYWxnIjoiUFMyNTYiLCJjbGllbnRfbGVnYWxfZW50aXR5X2lkIjoiMThiNzVhNzYtNTgyMS00YzllLWI0NjUtNDcwOTI5MWNmMGY0IiwiY2xpZW50X2xlZ2FsX2VudGl0eV9uYW1lIjoiTW9jayBTb2Z0d2FyZSBDb21wYW55IiwiY2xpZW50X29yZ19pZCI6ImZmYjFjOGJhLTI3OWUtNDRkOC05N" +
            @"mYwLTFiYzM0YTZiNDM2ZiIsImNsaWVudF9vcmdfbmFtZSI6Ik1vY2sgRmluYW5jZSBUb29scyIsImNsaWVudF9yZXZvY2F0aW9uX3VyaSI6Imh0dHBzOi8vYXBpLm1vY2tzb2Z0d2FyZS9teWJ1ZGdldGFwcC9yZXZva2UiLCJjbGllbnRfcmVjaXBpZW50X2Jhc2VfdXJpIjoiaHR0cHM6Ly9hcGkubW9ja3Nv" +
            @"ZnR3YXJlL215YnVkZ2V0YXBwIiwiY2xpZW50X3NlY3Rvcl9pZGVudGlmaWVyX3VyaSI6ImFwaS5tb2Nrc29mdHdhcmUiLCJjbGllbnRfdG9zX3VyaSI6Imh0dHBzOi8vbW9ja3NvZnR3YXJlL215YnVkZ2V0YXBwL3Rlcm1zIiwiY2xpZW50X2p3a3NfdXJpIjoiaHR0cHM6Ly9sb2NhbGhvc3Q6NzAwNi9sb29" +
            @"wYmFjay9Nb2NrRGF0YVJlY2lwaWVudEp3a3MiLCJqdGkiOiJXbmlqVmw4MEVXZFNYNGFWbnJWUHdnIiwic2NvcGUiOlsiY2RyOnJlZ2lzdHJhdGlvbiJdLCJjbmYiOnsieDV0I1MyNTYiOiI1OEQ3NkY3QTYxQ0Q3MjZEQTFDNTRGNjg5OEU4RTY5RUE0Qzg4MDYwIn19.0ixpz-EZIRjcCnYRxgYxXn0QMZaTA" +
            @"uwBFtdfC0wI0NjR2_QuIoSVjR4CKq2Enk-zDgAxE-tfl4VvK3pim3At_gYmUDk7E6Meo7bkwV3Jpj6oZUOwEZyfJg6SQu7G1gvIkxt1YU5TrvCFKUQuHXxaJaNY0HZus5jkFrgTtxMubVOCn6b1VxEQqA1F_sBwIFq-BGx0QoGy-2LK8Dnw-z68E-xw4M3zYbSXOoXt7W6KTboF_A8C5M-68ER55ng9ecpRe3tG" +
            @"al9u-4vd64fDdwU3C4nwMCPVpMgmnVYEdZ-EKpWzrFJ1Clbq-WSYFgfFyeMG_ryObVM-IOwuxN_4ABDYPQ";

        // VSCode slows on excessively long lines, splitting string constant into smaller lines.
        public const string EXPIRED_CONSUMER_ACCESS_TOKEN =
            @"eyJhbGciOiJQUzI1NiIsImtpZCI6IjczQUVGQ0FGODA3NjUyQTQ2RTMzMTZEQjQ3RTkwNUU3QjcyNjUyQjIiLCJ0eXAiOiJhdCtqd3QiLCJ4" +
            @"NXQiOiJjNjc4cjRCMlVxUnVNeGJiUi1rRjU3Y21VckkifQ.eyJuYmYiOjE2MjQ5NDU3MDUsImV4cCI6MTYyNDk0NjAwNSwiaXNzIjoiaHR0c" +
            @"HM6Ly9sb2NhbGhvc3Q6ODAwMSIsImF1ZCI6ImNkcy1hdSIsImNsaWVudF9pZCI6IjM1NDAzNGNiLWUwNzQtNDc4NS04N2NjLWQ5ZDNhZjUxN" +
            @"ThjMCIsImF1dGhfdGltZSI6MTYyNDk0NTY5MSwiaWRwIjoibG9jYWwiLCJzaGFyaW5nX2V4cGlyZXNfYXQiOjE2MzI3MjE2OTcsImp0aSI6I" +
            @"jBVbU9oMzQ3UlBJMUk3SU9FdVd6SVEiLCJzb2Z0d2FyZV9pZCI6ImM2MzI3Zjg3LTY4N2EtNDM2OS05OWE0LWVhYWNkM2JiODIxMCIsInNlY" +
            @"3Rvcl9pZGVudGlmaWVyX3VyaSI6ImxvY2FsaG9zdCIsInN1YiI6IlZ5Ris3NnlXallubi9LbnNiaDM5YWo4NmdsWTJKNnN5V1VZTVhXcVZ5N" +
            @"ThKWGpPRWgvNVU1eG5UaExBNCtFRy8iLCJzY29wZSI6WyJvcGVuaWQiLCJwcm9maWxlIiwiYmFuazphY2NvdW50cy5iYXNpYzpyZWFkIiwiY" +
            @"mFuazp0cmFuc2FjdGlvbnM6cmVhZCIsImNvbW1vbjpjdXN0b21lci5iYXNpYzpyZWFkIiwiYmFuazphY2NvdW50cy5kZXRhaWw6cmVhZCJdL" +
            @"CJhbXIiOlsicHdkIl0sImNuZiI6eyJ4NXQjUzI1NiI6IjU4RDc2RjdBNjFDRDcyNkRBMUM1NEY2ODk4RThFNjlFQTRDODgwNjAifX0.kO9zZ" +
            @"Q2SwfUL9up0X5X0FzPW0z2SswFIGVNwe9Zwojk3BqUnKwI4U2S8bC71qKq70Ufp1UfrKgGiHRn5hvoXx6ha7foNcNRndAVfwFofii9Y5stKS" +
            @"GjVSDD1qvdVt5I__2UBmWUtSnZ9WoqsGkqptjMI2iv3RHQSuBarMprfemGrVaohKXOjJC-U-OmOYGIISk-67HBlGR2PkJoiWNCJf86opjAvZ" +
            @"Q62rr9-SGPLqZoSJNhhoPd7s3toJu4EHnHr-fh3XeSliE5KL_X8feab51Kc6e3E9k-4s6TVEAp24_72e9vTHG-75mcbRShzMeTbGCgCBX5-m" +
            @"pVVp9wbglw4hw";


        // Certificate used by DataRecipient for the MTLS connection with Dataholder
        public const string DH_MTLS_CERTIFICATE_FILENAME = "certificates\\server.pfx";
        public const string DH_MTLS_CERTIFICATE_PASSWORD = "#M0ckDataHolder#";

        // Certificate used by DataRecipient to sign client assertions
        public const string CERTIFICATE_FILENAME = "certificates\\client.pfx";
        public const string CERTIFICATE_PASSWORD = "#M0ckDataRecipient#";

        public const string JWT_CERTIFICATE_FILENAME = "certificates\\mdr\\jwks.pfx";
        public const string JWT_CERTIFICATE_PASSWORD = "#M0ckDataRecipient#";

        // An additional certificate used by DataRecipient to sign client assertions
        protected const string ADDITIONAL_CERTIFICATE_FILENAME = "certificates\\client-additional.pfx";
        protected const string ADDITIONAL_CERTIFICATE_PASSWORD = CERTIFICATE_PASSWORD;

        protected const string INVALID_CERTIFICATE_FILENAME = "certificates\\client-invalid.pfx";
        protected const string INVALID_CERTIFICATE_PASSWORD = "#M0ckDataRecipient#";
        //protected const string SSA_CERTIFICATE_FILENAME = "certificates\\ssa.pfx";
        //protected const string SSA_CERTIFICATE_PASSWORD = "#M0ckRegister#";
        public const string DATAHOLDER_CERTIFICATE_FILENAME = "certificates\\mock-data-holder.pfx";
        public const string DATAHOLDER_CERTIFICATE_PASSWORD = "#M0ckDataHolder#";

        public const string DH_MTLS_GATEWAY_URL = "https://localhost:8002"; // MTLS gateway url
        public const string DH_MTLS_IDENTITYSERVER_TOKEN_URL = DH_MTLS_GATEWAY_URL + "/idp/connect/token"; // DH IdentityServer Token API
        public const string DH_TLS_IDENTITYSERVER_BASE_URL = "https://localhost:8001";  // DH Identity Server API base url
        protected const string DH_TLS_PUBLIC_BASE_URL = "https://localhost:8000"; // DH Public API base url
        public const string REGISTER_MTLS_TOKEN_URL = "https://localhost:7001/idp/connect/token"; // Register Token API

        public const string REGISTRATION_AUDIENCE_URI = "https://localhost:8001";

        // Connection strings
        static public string DATAHOLDER_CONNECTIONSTRING => Configuration["ConnectionStrings:DataHolder"]
            ?? throw new Exception($"{nameof(DATAHOLDER_CONNECTIONSTRING)} - configuration setting not found");
        static public string IDENTITYSERVER_CONNECTIONSTRING => Configuration["ConnectionStrings:IdentityServer"]
            ?? throw new Exception($"{nameof(IDENTITYSERVER_CONNECTIONSTRING)} - configuration setting not found");
        static public string REGISTER_CONNECTIONSTRING => Configuration["ConnectionStrings:Register"]
            ?? throw new Exception($"{nameof(REGISTER_CONNECTIONSTRING)} - configuration setting not found");

        // Seed-data offset
        static public bool SEEDDATA_OFFSETDATES => Configuration["SeedData:OffsetDates"] == "true";

        static private IConfigurationRoot? configuration;
        static public IConfigurationRoot Configuration
        {
            get
            {
                if (configuration == null)
                {
                    configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
                        .Build();
                }

                return configuration;
            }
        }

        // Legal Entity
        protected const string LEGALENTITYID = "18B75A76-5821-4C9E-B465-4709291CF0F4";

        // Brand
        public const string BRANDID = "FFB1C8BA-279E-44D8-96F0-1BC34A6B436F";

        // Software Product
        public const string SOFTWAREPRODUCT_ID = "C6327F87-687A-4369-99A4-EAACD3BB8210";
        public const string SOFTWAREPRODUCT_ID_INVALID = "f00f00f0-f00f-f00f-f00f-f00f00f00f00";
        // public const string SOFTWAREPRODUCT_REDIRECT_URI = "https://api.mocksoftware/mybudgetapp/callback";
        // public const string SOFTWAREPRODUCT_REDIRECT_URI = "https://localhost:9001/consent/callback";
        public const string SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS = "https://localhost:9999/consent/callback";
        public const string SOFTWAREPRODUCT_JWKS_URI_FOR_INTEGRATION_TESTS = "https://localhost:9998/jwks";
        public const string SOFTWAREPRODUCT_SECTOR_IDENTIFIER_URI = "api.mocksoftware";

        // Additional brand/software product
        public const string ADDITIONAL_BRAND_ID = "20C0864B-CEEF-4DE0-8944-EB0962F825EB";
        public const string ADDITIONAL_SOFTWAREPRODUCT_ID = "9381DAD2-6B68-4879-B496-C1319D7DFBC9";
        // public const string ADDITIONAL_BRAND_ID2 = "8A3441AA-1242-493A-B466-DCBFFFE5A441";
        // public const string ADDITIONAL_SOFTWAREPRODUCT_ID2 = "25EE528F-35AC-4A66-A67C-6166602C9322";
        public const string ADDITIONAL_SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS = "https://localhost:9997/consent/callback";
        public const string ADDITIONAL_SOFTWAREPRODUCT_JWKS_URI_FOR_INTEGRATION_TESTS = "https://localhost:9996/jwks";
        public const string ADDITIONAL_JWKS_CERTIFICATE_FILENAME = ADDITIONAL_CERTIFICATE_FILENAME;  // FIXME - generate new certificate just for jwks
        public const string ADDITIONAL_JWKS_CERTIFICATE_PASSWORD = ADDITIONAL_CERTIFICATE_PASSWORD;

        // Scope
        // public const string SCOPE = "common:customer.basic:read bank:accounts.basic:read bank:transactions:read";
        public const string SCOPE = "openid profile common:customer.basic:read bank:accounts.basic:read bank:transactions:read";
        public const string SCOPE_WITHOUT_OPENID = "profile common:customer.basic:read bank:accounts.basic:read bank:transactions:read";
        public const string SCOPE_REGISTRATION = "cdr:registration";

        // IdPermanence
        public const string IDPERMANENCE_PRIVATEKEY = "90733A75F19347118B3BE0030AB590A8";

        public const string CLIENTASSERTIONTYPE = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";

        /// <summary>
        /// Assert response content and expectedJson are equivalent
        /// </summary>
        /// <param name="expectedJson">The expected json</param>
        /// <param name="content">The response content</param>
        public static async Task Assert_HasContent_Json(string? expectedJson, HttpContent? content)
        {
            // content.Should().NotBeNull();
            content.Should().NotBeNull(expectedJson ?? "");
            if (content == null)
            {
                return;
            }

            var actualJson = await content.ReadAsStringAsync();
            Assert_Json(expectedJson, actualJson);
        }

        /// <summary>
        /// Assert response content is empty
        /// </summary>
        /// <param name="content">The response content</param>
        public static async Task Assert_HasNoContent(HttpContent? content, string? because = null)
        {
            content.Should().NotBeNull();
            if (content == null)
            {
                return;
            }

            var actualJson = await content.ReadAsStringAsync();
            actualJson.Should().BeNullOrEmpty(because);
        }

        /// <summary>
        /// Assert_HasNoContent because "No detail about response content in AC, check that API does not actually return any response content"
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static async Task Assert_HasNoContent2(HttpContent? content)
        {
            // Assert - No detail about response content in AC, check that API does not actually return any response content
            await Assert_HasNoContent(content, "(Assert_HasNoContent2) AC does not specify response content and yet content is returned by API. Either AC needs to specify expected response content or API needs to return no content.");
        }

        /// <summary>
        /// Assert actual json is equivalent to expected json
        /// </summary>
        /// <param name="expectedJson">The expected json</param>
        /// <param name="actualJson">The actual json</param>
        public static void Assert_Json(string? expectedJson, string actualJson)
        {
            static object? Deserialize(string name, string json)
            {
                try
                {
                    return JsonConvert.DeserializeObject<object>(json);
                }
                catch
                {
                    // throw new Exception($@"Error deserialising {name} - ""{json}""");
                    return null;
                }
            }

            expectedJson.Should().NotBeNullOrEmpty();
            // actualJson.Should().NotBeNullOrEmpty();
            actualJson.Should().NotBeNullOrEmpty(expectedJson == null ? "" : $"expected {expectedJson}");

            if (string.IsNullOrEmpty(expectedJson) || string.IsNullOrEmpty(actualJson))
            {
                return;
            }

            object? expectedObject = Deserialize(nameof(expectedJson), expectedJson);
            expectedObject.Should().NotBeNull($"Error deserializing expected json - '{expectedJson}'");

            object? actualObject = Deserialize(nameof(actualJson), actualJson);
            actualObject.Should().NotBeNull($"Error deserializing actual json - '{actualJson}'");

            var expectedJsonNormalised = JsonConvert.SerializeObject(expectedObject);
            var actualJsonNormalised = JsonConvert.SerializeObject(actualObject);

            actualJson?.JsonCompare(expectedJson).Should().BeTrue(
                $"\r\nExpected json:\r\n{expectedJsonNormalised}\r\nActual Json:\r\n{actualJsonNormalised}\r\n"
            );
        }

        /// <summary>
        /// Assert headers has a single header with the expected value.
        /// If expectedValue then just check for the existence of the header (and not it's value)
        /// </summary>
        /// <param name="expectedValue">The expected header value</param>
        /// <param name="headers">The headers to check</param>
        /// <param name="name">Name of header to check</param>
        public static void Assert_HasHeader(string? expectedValue, HttpHeaders headers, string name, bool startsWith = false)
        {
            headers.Should().NotBeNull();
            if (headers != null)
            {
                headers.Contains(name).Should().BeTrue($"name={name}");
                if (headers.Contains(name))
                {
                    var headerValues = headers.GetValues(name);
                    headerValues.Should().ContainSingle(name, $"name={name}");

                    if (expectedValue != null)
                    {
                        string headerValue = headerValues.First();

                        if (startsWith)
                        {
                            headerValue.Should().StartWith(expectedValue, $"name={name}");
                        }
                        else
                        {
                            headerValue.Should().Be(expectedValue, $"name={name}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Assert header has content type of ApplicationJson
        /// </summary>
        /// <param name="content"></param>
        public static void Assert_HasContentType_ApplicationJson(HttpContent content)
        {
            content.Should().NotBeNull();
            content?.Headers.Should().NotBeNull();
            content?.Headers?.ContentType.Should().NotBeNull();
            content?.Headers?.ContentType?.ToString().Should().StartWith("application/json");
        }

        /// <summary>
        /// Assert claim exists
        /// </summary>
        public static void AssertClaim(IEnumerable<Claim> claims, string claimType, string claimValue)
        {
            claims.Should().NotBeNull();
            if (claims != null)
            {
                claims.FirstOrDefault(claim => claim.Type == claimType && claim.Value == claimValue).Should().NotBeNull($"Expected {claimType}={claimValue}");
            }
        }

        public const string AUTHORISE_OTP = "000789";

        public const string USERID_JANEWILSON = "jwilson";
        public const string USERID_STEVEKENNEDY = "sken";
        public const string USERID_DEWAYNESTEVE = "dsteve";
        public const string USERID_BUSINESS1 = "bis1";
        public const string USERID_BUSINESS2 = "bis2";
        public const string USERID_BEVERAGE = "bev";
        public const string USERID_KAMILLASMITH = "ksmith";

        public const string CUSTOMERID_JANEWILSON = "BFB689FB-7745-45B9-BBAA-B21E00072447";
        // public const string CUSTOMERID_STEVEKENNEDY = "4705EBA7-DAAE-4DCA-A1F9-12AA785A702F";
        // public const string CUSTOMERID_STEVECURRY = "688E36CF-BFC7-46BB-855B-C70893DAA800";
        public const string CUSTOMERID_BUSINESS1 = "A97BA8D9-C89D-4126-A3B1-5AAA50F8DC5F";
        // public const string CUSTOMERID_BUSINESS2 = "6EDAF322-A685-41C0-A3B6-AEA8A235C34E";
        // public const string CUSTOMERID_BEVERAGE = "89111E7F-A08B-468D-AA8C-0AC3AE0FE559";
        // public const string CUSTOMERID_LILYWANG = "AA649633-EED2-4E56-B97D-7DF150360942";

        public const string ACCOUNTID_JOHN_SMITH = "1122334455";
        public const string ACCOUNTID_JANE_WILSON = "98765988";
        // public const string ACCOUNTID_INVALID = "000000";
        // public const string ACCOUNTID_BUSINESS1 = "54676423";

        // public const string ACCOUNTIDS_ALL_JOHN_SMITH = "1122334455";
        public const string ACCOUNTIDS_ALL_JANE_WILSON = "98765988,98765987";
        public const string ACCOUNTIDS_ALL_BUSINESS1 = "54676423";
        public const string ACCOUNTIDS_ALL_BUSINESS2 = "";
        public const string ACCOUNTIDS_ALL_BEVERAGE = "835672345";
        public const string ACCOUNTIDS_ALL_KAMILLA_SMITH = "0000001,0000002,0000003,0000004,0000005,0000006,0000007,0000008,0000009,0000010,1000001,1000002,1000003,1000004,1000005,1000006,1000007,1000008,1000009,1000010,2000001,2000002,2000003,2000004,2000005,2000006,2000007,2000008,2000009,2000010";
        public const string ACCOUNTIDS_ALL_DEWAYNE_STEVE = "96565987,1100002,96534987";
        public const string ACCOUNTIDS_ALL_STEVE_KENNEDY = "";

        public enum TokenType
        {
            JANE_WILSON, STEVE_KENNEDY, DEWAYNE_STEVE, BUSINESS_1, BEVERAGE, INVALID_FOO, INVALID_EMPTY, INVALID_OMIT, KAMILLA_SMITH, BUSINESS_2
        }

        // public static string? CreateAccessToken(TokenType tokenType, string scope = SCOPE, bool expired = false)
        // {
        //     static string CreateAccessToken(string sub, string name, string client_id, string scope, bool expired)
        //     {
        //         var subject = new Dictionary<string, object>();

        //         if (sub != null)
        //         {
        //             subject.Add("sub", sub);
        //         }

        //         if (name != null)
        //         {
        //             subject.Add("name", name);
        //         }

        //         if (client_id != null)
        //         {
        //             subject.Add("client_id", client_id);
        //         }

        //         if (scope != null)
        //         {
        //             subject.Add("scope", scope);
        //         }

        //         return JWT.CreateJWT(
        //             DATAHOLDER_CERTIFICATE_FILENAME,
        //             DATAHOLDER_CERTIFICATE_PASSWORD,
        //             null,
        //             null,
        //             expired,
        //             subject,
        //             JWT.SecurityAlgorithm.HmacSha256
        //          );
        //     }

        //     return tokenType switch
        //     {
        //         // TokenType.JANE_WILSON => CreateAccessToken("05BBBE80-D858-4884-AAEA-5D21175A9B16", "Jane Wilson", SOFTWAREPRODUCT_ID, scope, expired),
        //         // TokenType.STEVE_KENNEDY => CreateAccessToken("0706279E-69CF-47B6-9954-B0FBA367F97C", "Steve Kennedy", SOFTWAREPRODUCT_ID, scope, expired),
        //         // TokenType.STEVE_CURRY => CreateAccessToken("274A61B2-BD11-4B8A-A421-C580851E836B", "Steve Curry", SOFTWAREPRODUCT_ID, scope, expired),
        //         TokenType.BUSINESS_1 => CreateAccessToken(CUSTOMERID_BUSINESS1, "Business 1", SOFTWAREPRODUCT_ID, scope, expired),
        //         // TokenType.BEVERAGE => CreateAccessToken("01219DA0-FCF5-4D12-BD32-4F84D7700799", "Beverage", SOFTWAREPRODUCT_ID, scope, expired),
        //         // TokenType.LILY_WANG => CreateAccessToken("72A45CB4-636D-4BF4-9E21-7EF0E97B3A17", "Lily Wang", SOFTWAREPRODUCT_ID, scope, expired),
        //         TokenType.INVALID_FOO => "foo",
        //         TokenType.INVALID_EMPTY => "",
        //         TokenType.INVALID_OMIT => null,
        //         _ => throw new ArgumentOutOfRangeException(nameof(tokenType))
        //     };
        // }

        // /// <summary>
        // /// Use authCode to get access token
        // /// </summary>
        // protected static async Task<string?> CreateAccessToken_UsingAuthCode(TokenType tokenType, string scope = SCOPE, bool expired = false, string accountId = "")
        // {
        //     // Faked authorise/consent flow
        //     return await CreateAccessToken_UsingAuthCode_v1(tokenType, scope, expired, accountId);

        //     // E2E authorise/consent flow
        //     // return await CreateAccessToken_UsingAuthCode_v2(tokenType, scope, expired, accountId);  // DEBUG - 28/07
        // }

        // public static async Task<string?> CreateAccessToken_UsingAuthCode(TokenType tokenType, string scope = SCOPE, bool expired = false, string accountId = "")
        // {
        //     switch (tokenType)
        //     {
        //         case TokenType.JANE_WILSON:
        //         case TokenType.STEVE_KENNEDY:
        //         case TokenType.STEVE_CURRY:
        //         case TokenType.BUSINESS_1:
        //         case TokenType.BUSINESS_2:
        //         case TokenType.BEVERAGE:
        //         case TokenType.LILY_WANG:
        //             {
        //                 const int SHORTLIVEDLIFETIMESECONDS = 10;

        //                 var accountIdsToGenerateCode = string.IsNullOrEmpty(accountId) ?
        //                      tokenType switch
        //                      {
        //                          TokenType.JANE_WILSON => ACCOUNTIDS_ALL_JANE_WILSON,
        //                          TokenType.BUSINESS_1 => ACCOUNTIDS_ALL_BUSINESS1,
        //                          TokenType.LILY_WANG => ACCOUNTIDS_ALL_LILY_WANG,
        //                          TokenType.STEVE_CURRY => ACCOUNTIDS_ALL_STEVE_CURRY,
        //                          _ => ACCOUNTID_INVALID,
        //                      } :
        //                      accountId;

        //                 // Get authcode
        //                 (var authCode, _) = DataHolder_Authorise_API.Authorise(
        //                     tokenType switch
        //                     {
        //                         TokenType.JANE_WILSON => CUSTOMERID_JANEWILSON,
        //                         TokenType.STEVE_KENNEDY => CUSTOMERID_STEVEKENNEDY,
        //                         TokenType.STEVE_CURRY => CUSTOMERID_STEVECURRY,
        //                         TokenType.BUSINESS_1 => CUSTOMERID_BUSINESS1,
        //                         TokenType.BUSINESS_2 => CUSTOMERID_BUSINESS2,
        //                         TokenType.BEVERAGE => CUSTOMERID_BEVERAGE,
        //                         TokenType.LILY_WANG => CUSTOMERID_LILYWANG,
        //                         _ => throw new ArgumentException(nameof(TokenType)),
        //                     },
        //                     scope,
        //                     lifetimeSeconds: expired ? SHORTLIVEDLIFETIMESECONDS : 600,
        //                     accountIds: accountIdsToGenerateCode.Split(","));

        //                 // Get access token using authcode
        //                 var accessToken = await DataHolder_Token_API.GetAccessToken(authCode);

        //                 // If getting an expired access token wait until the SHORTLIVELIFETIME has passed so that the token will have expired
        //                 if (expired)
        //                 {
        //                     await Task.Delay((SHORTLIVEDLIFETIMESECONDS + 60) * 1000);
        //                 }

        //                 return accessToken;
        //             }
        //         case TokenType.INVALID_FOO: return "foo";
        //         case TokenType.INVALID_EMPTY: return "";
        //         case TokenType.INVALID_OMIT: return null;
        //         default:
        //             throw new ArgumentException(nameof(TokenType));
        //     }
        // }

        /// <summary>
        /// Perform auth/consent flow, get access token and return it.
        /// </summary>
        public static async Task<string?> GetAccessToken(
            TokenType tokenType,
            string scope = SCOPE,
            bool expired = false,
            string accountId = "")
        {
            if (expired) { throw new NotImplementedException(); }

            switch (tokenType)
            {
                case TokenType.JANE_WILSON:
                case TokenType.STEVE_KENNEDY:
                case TokenType.DEWAYNE_STEVE:
                case TokenType.BUSINESS_1:
                case TokenType.BUSINESS_2:
                case TokenType.BEVERAGE:
                case TokenType.KAMILLA_SMITH:
                    {
                        (var authCode, _) = await new DataHolder_Authorise_APIv2
                        {
                            // UserId = tokenType switch
                            // {
                            //     TokenType.JANE_WILSON => USERID_JANEWILSON,
                            //     TokenType.STEVE_KENNEDY => USERID_STEVEKENNEDY,
                            //     TokenType.STEVE_CURRY => USERID_STEVECURRY,
                            //     TokenType.BUSINESS_1 => USERID_BUSINESS1,
                            //     TokenType.BUSINESS_2 => USERID_BUSINESS2,
                            //     TokenType.BEVERAGE => USERID_BEVERAGE,
                            //     TokenType.LILY_WANG => USERID_LILYWANG,
                            //     _ => throw new ArgumentException($"{nameof(DataHolder_Authorise_APIv2.UserId)} - {nameof(tokenType)} = {tokenType}")
                            // },
                            UserId = tokenType.UserId(),

                            OTP = AUTHORISE_OTP,

                            // SelectedAccountIds = tokenType switch
                            // {
                            //     TokenType.JANE_WILSON => ACCOUNTIDS_ALL_JANE_WILSON,
                            //     TokenType.STEVE_KENNEDY => ACCOUNTIDS_ALL_STEVE_KENNEDY,
                            //     TokenType.STEVE_CURRY => ACCOUNTIDS_ALL_STEVE_CURRY,
                            //     TokenType.BUSINESS_1 => ACCOUNTIDS_ALL_BUSINESS1,
                            //     TokenType.BUSINESS_2 => ACCOUNTIDS_ALL_BUSINESS2,
                            //     TokenType.BEVERAGE => ACCOUNTIDS_ALL_BEVERAGE,
                            //     TokenType.LILY_WANG => ACCOUNTIDS_ALL_LILY_WANG,
                            //     _ => throw new ArgumentException($"{nameof(DataHolder_Authorise_APIv2.SelectedAccountIds)} - {nameof(tokenType)} = {tokenType}")
                            // },
                            SelectedAccountIds = tokenType.AllAccountIds(),

                            Scope = scope
                        }.Authorise();

                        var accessToken = await DataHolder_Token_API.GetAccessToken(authCode);

                        return accessToken;
                    }

                case TokenType.INVALID_FOO:
                    return "foo";
                case TokenType.INVALID_EMPTY:
                    return "";
                case TokenType.INVALID_OMIT:
                    return null;

                default:
                    throw new ArgumentException($"{nameof(tokenType)} = {tokenType}");
            }
        }

        // public static async Task<string?> CreateAccessToken_UsingAuthCode_v2(TokenType tokenType, string scope = SCOPE, bool expired = false, string accountId = "")
        // {
        //     // DEBUG - 28/07

        //     if (tokenType != TokenType.JANE_WILSON)
        //     {
        //         throw new Exception("only Jane Wilson supported - MSDEBUG"); // DEBUG
        //     }

        //     // Perform authorise and consent flow and get authCode
        //     (var authCode, _) = await new DataHolder_Authorise_APIv2
        //     {
        //         UserId = "jwilson",
        //         OTP = "000789",
        //         SelectedAccountIds = new string[] { "98765987" },
        //         Scope = scope
        //     }.Authorise();

        //     // User authCode to get access token
        //     var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);
        //     if (tokenResponse == null) throw new Exception($"{nameof(CreateAccessToken_UsingAuthCode_v2)} - TokenResponse is null");
        //     if (tokenResponse.AccessToken == null) throw new Exception($"{nameof(CreateAccessToken_UsingAuthCode_v2)} - Access token is null");

        //     // DEBUG - if expired need to wait until expired

        //     // Return access token
        //     return tokenResponse.AccessToken;
        // }

        public static async Task<DataHolder_Token_API.Response> GetToken(TokenType tokenType,
            int tokenLifetime = 14400,
            int sharingDuration = 7776000)
        {
            // Perform authorise and consent flow and get authCode
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = tokenType switch
                {
                    TokenType.JANE_WILSON => BaseTest.USERID_JANEWILSON,
                    _ => throw new Exception($"{nameof(GetToken)} - Unsupported token type - {tokenType}")
                },
                SelectedAccountIds = tokenType switch
                {
                    TokenType.JANE_WILSON => ACCOUNTIDS_ALL_JANE_WILSON,
                    _ => throw new Exception($"{nameof(GetToken)} - Unsupported token type - {tokenType}")
                },
                OTP = BaseTest.AUTHORISE_OTP,
                Scope = SCOPE,
                TokenLifetime = tokenLifetime,
                SharingDuration = sharingDuration
            }.Authorise();

            // User authCode to get tokens
            var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);
            if (tokenResponse == null) throw new Exception($"{nameof(GetToken)} - TokenResponse is null");
            if (tokenResponse.IdToken == null) throw new Exception($"{nameof(GetToken)} - Id token is null");
            if (tokenResponse.AccessToken == null) throw new Exception($"{nameof(GetToken)} - Access token is null");
            if (tokenResponse.RefreshToken == null) throw new Exception($"{nameof(GetToken)} - Refresh token is null");
            if (tokenResponse.CdrArrangementId == null) throw new Exception($"{nameof(GetToken)} - CdrArrangementId is null");

            // Return access token
            return tokenResponse;
        }

        protected enum Table { LEGALENTITY, BRAND, SOFTWAREPRODUCT }

        static protected string GetStatus(Table table, string id)
        {
            using var connection = new SqliteConnection(DATAHOLDER_CONNECTIONSTRING);
            connection.Open();

            using var selectCommand = new SqliteCommand($"select status from {table} where {table}ID = @id", connection);
            selectCommand.Parameters.AddWithValue("@id", id);

            return selectCommand.ExecuteScalarString();
        }

        static protected void SetStatus(Table table, string id, string status)
        {
            using var connection = new SqliteConnection(DATAHOLDER_CONNECTIONSTRING);
            connection.Open();

            using var updateCommand = new SqliteCommand($"update {table} set status = @status where {table}ID = @id", connection);
            updateCommand.Parameters.AddWithValue("@id", id);
            updateCommand.Parameters.AddWithValue("@status", status);
            updateCommand.ExecuteNonQuery();

            if (GetStatus(table, id) != status)
            {
                throw new Exception("Status not updated");
            }
        }

        static protected string? GetDate(string XFapiAuthDate)
        {
            return XFapiAuthDate switch
            {
                "DateTime.UtcNow" => DateTime.UtcNow.ToString(),
                "DateTime.UtcNow+1" => DateTime.UtcNow.AddDays(1).ToString(),
                "DateTime.Now.RFC1123" => DateTime.Now.ToUniversalTime().ToString("r"),
                "DateTime.Now.RFC1123+1" => DateTime.Now.AddDays(1).ToUniversalTime().ToString("r"),
                "foo" => XFapiAuthDate,
                null => XFapiAuthDate,
                _ => throw new ArgumentOutOfRangeException(nameof(XFapiAuthDate))
            };
        }

        /// <summary>
        /// IdPermanence encryption
        /// </summary>
        static protected string IdPermanenceEncrypt(string plainText, string customerId, string softwareProductId)
        {
            customerId = customerId.ToLower();
            softwareProductId = softwareProductId.ToLower();

            var idParameters = new IdPermanenceParameters
            {
                SoftwareProductId = softwareProductId,
                CustomerId = customerId,
            };

            var encrypted = IdPermanenceHelper.EncryptId(plainText, idParameters, IDPERMANENCE_PRIVATEKEY);

            return encrypted;
        }

        /// <summary>
        /// Extract customerId (by decrypting "sub" claim).
        /// Also extract "client-id", "software_id" and "sector_identifier_uri" claims
        /// </summary>
        static protected void ExtractClaimsFromToken(string? accessToken, out string customerId, out string softwareProductId)
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

            // var clientId = jwt.Claim("client_id").Value;
            softwareProductId = jwt.Claim("software_id").Value;
            var sectorIdentifierUri = jwt.Claim("sector_identifier_uri").Value;

            // Decrypt sub to extract customerId
            var sub = jwt.Claim("sub").Value;
            customerId = IdPermanenceHelper.DecryptSub(
                sub,
                new SubPermanenceParameters
                {
                    SoftwareProductId = softwareProductId,
                    SectorIdentifierUri = sectorIdentifierUri
                },
                BaseTest.IDPERMANENCE_PRIVATEKEY
            );
        }

        static public void WriteStringToFile(string filename, string? str)
        {
            File.WriteAllText(filename, str);
        }

        static public void WriteJWTtoFile(string filename, string jwt)
        {
            var decodedJWT = new JwtSecurityTokenHandler().ReadJwtToken(jwt);
            File.WriteAllText(filename, decodedJWT.ToJson());
        }

        static public void WriteJsonToFile(string filename, string json)
        {
            var jsonObj = JsonConvert.DeserializeObject(json);
            var jsonStr = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
            File.WriteAllText(filename, jsonStr);
        }
    }
}
