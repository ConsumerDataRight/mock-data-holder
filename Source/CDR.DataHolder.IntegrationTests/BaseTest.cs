using CDR.DataHolder.API.Infrastructure.IdPermanence;
using CDR.DataHolder.IntegrationTests.Extensions;
using CDR.DataHolder.IntegrationTests.Infrastructure.API2;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

#nullable enable

namespace CDR.DataHolder.IntegrationTests
{
    class DisplayTestMethodNameAttribute : BeforeAfterTestAttribute
    {
        static int count = 0;

        public override void Before(MethodInfo methodUnderTest)
        {
            Console.WriteLine($"Test #{++count} - {methodUnderTest.DeclaringType?.Name}.{methodUnderTest.Name}");
        }

        public override void After(MethodInfo methodUnderTest)
        {
        }
    }

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
    [DisplayTestMethodName]
    abstract public class BaseTest
    {
        public static int TOKEN_EXPIRY_SECONDS => Int32.Parse(Configuration["AccessTokenLifetimeSeconds"]);

        // VSCode slows on excessively long lines, splitting string constant into smaller lines.
        public const string DATAHOLDER_ACCESSTOKEN_EXPIRED =
            @"eyJhbGciOiJQUzI1NiIsImtpZCI6IjdDNTcxNjU1M0U5QjEzMkVGMzI1QzQ5Q0EyMDc5NzM3MTk2QzAzREIiLCJ4NXQiOiJmRmNXVlQ2YkV5N3pKY1Njb2dlWE54bHNBOXMiLCJ0eXAiOi" +
            @"JhdCtqd3QifQ.eyJuYmYiOjE2NTI2Njk5NDMsImV4cCI6MTY1MjY3MDI0MywiaXNzIjoiaHR0cHM6Ly9sb2NhbGhvc3Q6ODEwMSIsImF1ZCI6ImNkcy1hdSIsImNsaWVudF9pZCI6ImM2M" +
            @"zI3Zjg3LTY4N2EtNDM2OS05OWE0LWVhYWNkM2JiODIxMCIsImNsaWVudF9zb2Z0d2FyZV9pZCI6ImM2MzI3Zjg3LTY4N2EtNDM2OS05OWE0LWVhYWNkM2JiODIxMCIsImNsaWVudF9zb2Z" +
            @"0d2FyZV9zdGF0ZW1lbnQiOiJleUpoYkdjaU9pSlFVekkxTmlJc0ltdHBaQ0k2SWpVME1rRTVRamt4TmpBd05EZzRNRGc0UTBRMFJEZ3hOamt4TmtFNVJqUTBPRGhFUkRJMk5URWlMQ0owZ" +
            @"VhBaU9pSktWMVFpZlEuZXcwS0lDQWliR1ZuWVd4ZlpXNTBhWFI1WDJsa0lqb2dJakU0WWpjMVlUYzJMVFU0TWpFdE5HTTVaUzFpTkRZMUxUUTNNRGt5T1RGalpqQm1OQ0lzRFFvZ0lDSnN" +
            @"aV2RoYkY5bGJuUnBkSGxmYm1GdFpTSTZJQ0pOYjJOcklGTnZablIzWVhKbElFTnZiWEJoYm5raUxBMEtJQ0FpYVhOeklqb2dJbU5rY2kxeVpXZHBjM1JsY2lJc0RRb2dJQ0pwWVhRaU9pQ" +
            @"XhOalV5TmpZNU9USTRMQTBLSUNBaVpYaHdJam9nTVRZMU1qWTNNRFV5T0N3TkNpQWdJbXAwYVNJNklDSmpOREkzTXpjd1pEa3pOR0UwWTJObFlXUTBObU0xWWpGbE9EQmlaalZoTWlJc0R" +
            @"Rb2dJQ0p2Y21kZmFXUWlPaUFpWm1aaU1XTTRZbUV0TWpjNVpTMDBOR1E0TFRrMlpqQXRNV0pqTXpSaE5tSTBNelptSWl3TkNpQWdJbTl5WjE5dVlXMWxJam9nSWsxdlkyc2dSbWx1WVc1a" +
            @"lpTQlViMjlzY3lJc0RRb2dJQ0pqYkdsbGJuUmZibUZ0WlNJNklDSk5lVUoxWkdkbGRFaGxiSEJsY2lJc0RRb2dJQ0pqYkdsbGJuUmZaR1Z6WTNKcGNIUnBiMjRpT2lBaVFTQndjbTlrZFd" +
            @"OMElIUnZJR2hsYkhBZ2VXOTFJRzFoYm1GblpTQjViM1Z5SUdKMVpHZGxkQ0lzRFFvZ0lDSmpiR2xsYm5SZmRYSnBJam9nSW1oMGRIQnpPaTh2Ylc5amEzTnZablIzWVhKbEwyMTVZblZrW" +
            @"jJWMFlYQndJaXdOQ2lBZ0luSmxaR2x5WldOMFgzVnlhWE1pT2lCYkRRb2dJQ0FnSW1oMGRIQnpPaTh2Ykc5allXeG9iM04wT2prNU9Ua3ZZMjl1YzJWdWRDOWpZV3hzWW1GamF5SU5DaUF" +
            @"nWFN3TkNpQWdJbXh2WjI5ZmRYSnBJam9nSW1oMGRIQnpPaTh2Ylc5amEzTnZablIzWVhKbEwyMTVZblZrWjJWMFlYQndMMmx0Wnk5c2IyZHZMbkJ1WnlJc0RRb2dJQ0owYjNOZmRYSnBJa" +
            @"m9nSW1oMGRIQnpPaTh2Ylc5amEzTnZablIzWVhKbEwyMTVZblZrWjJWMFlYQndMM1JsY20xeklpd05DaUFnSW5CdmJHbGplVjkxY21raU9pQWlhSFIwY0hNNkx5OXRiMk5yYzI5bWRIZGh" +
            @"jbVV2YlhsaWRXUm5aWFJoY0hBdmNHOXNhV041SWl3TkNpQWdJbXAzYTNOZmRYSnBJam9nSW1oMGRIQnpPaTh2Ykc5allXeG9iM04wT2prNU9UZ3ZhbmRyY3lJc0RRb2dJQ0p5WlhadlkyR" +
            @"jBhVzl1WDNWeWFTSTZJQ0pvZEhSd2N6b3ZMMnh2WTJGc2FHOXpkRG81TURBeEwzSmxkbTlqWVhScGIyNGlMQTBLSUNBaWNtVmphWEJwWlc1MFgySmhjMlZmZFhKcElqb2dJbWgwZEhCek9" +
            @"pOHZiRzlqWVd4b2IzTjBPamt3TURFaUxBMEtJQ0FpYzI5bWRIZGhjbVZmYVdRaU9pQWlZell6TWpkbU9EY3ROamczWVMwME16WTVMVGs1WVRRdFpXRmhZMlF6WW1JNE1qRXdJaXdOQ2lBZ" +
            @"0luTnZablIzWVhKbFgzSnZiR1Z6SWpvZ0ltUmhkR0V0Y21WamFYQnBaVzUwTFhOdlpuUjNZWEpsTFhCeWIyUjFZM1FpTEEwS0lDQWljMk52Y0dVaU9pQWliM0JsYm1sa0lIQnliMlpwYkd" +
            @"VZ1ltRnVhenBoWTJOdmRXNTBjeTVpWVhOcFl6cHlaV0ZrSUdKaGJtczZZV05qYjNWdWRITXVaR1YwWVdsc09uSmxZV1FnWW1GdWF6cDBjbUZ1YzJGamRHbHZibk02Y21WaFpDQmlZVzVyT" +
            @"25CaGVXVmxjenB5WldGa0lHSmhibXM2Y21WbmRXeGhjbDl3WVhsdFpXNTBjenB5WldGa0lHTnZiVzF2YmpwamRYTjBiMjFsY2k1aVlYTnBZenB5WldGa0lHTnZiVzF2YmpwamRYTjBiMjF" +
            @"sY2k1a1pYUmhhV3c2Y21WaFpDQmpaSEk2Y21WbmFYTjBjbUYwYVc5dUlHVnVaWEpuZVRwaFkyTnZkVzUwY3k1aVlYTnBZenB5WldGa0lHVnVaWEpuZVRwaFkyTnZkVzUwY3k1amIyNWpaW" +
            @"E56YVc5dWN6cHlaV0ZrSWcwS2ZRLnUzU0RhSW1fR21SbDg2Yi1hU3B3OHBYNlhCaVp2VVJRWUFJU2QyVXY0MUNVZlZmYXRSN3RZWWlEaVZhVWt5c1FUSW5sazNnVE85Yk5pT0lqM041Z0l" +
            @"kYmRQUXFSTmRVNTB5emxMN1RlVGhmc2JadTZsc0hVdVR6RWVUVlNBZzVMNGlPZk9OZndEYXRMSjBrTlR3b0ZPVWRZSmt4dnFLd0RJazl0Ymw4ZVpkbG1Rc21SQmNmN3oyMnBGUlQzaDJuT" +
            @"FNMWndIaEU2ck9OTEhFbk5YSjhLbUpzaXBnWmtzcFJPRzZXanZMUVl6MEtNVVdxN01EV3Y1TklIdFhXTFBsWlFpNjBLSHJYYkhxcXJkN3ZmRTRMWDFrMFRkUi1UeDFZeHpoT2EteGdvUWx" +
            @"qS1M2ck9qal9uWXl0el9WSDBTUWd5czVCX3ZOSjRtQk9qNi1JQlgzRmdSZyIsImNsaWVudF9sb2dvX3VyaSI6Imh0dHBzOi8vbW9ja3NvZnR3YXJlL215YnVkZ2V0YXBwL2ltZy9sb2dvL" +
            @"nBuZyIsImNsaWVudF9qd2tzX3VyaSI6Imh0dHBzOi8vbG9jYWxob3N0Ojk5OTgvandrcyIsImNsaWVudF90b2tlbl9lbmRwb2ludF9hdXRoX21ldGhvZCI6InByaXZhdGVfa2V5X2p3dCI" +
            @"sImNsaWVudF90b2tlbl9lbmRwb2ludF9hdXRoX3NpZ25pbmdfYWxnIjoiUFMyNTYiLCJjbGllbnRfaWRfdG9rZW5fZW5jcnlwdGVkX3Jlc3BvbnNlX2FsZyI6IlJTQS1PQUVQIiwiY2xpZ" +
            @"W50X2lkX3Rva2VuX2VuY3J5cHRlZF9yZXNwb25zZV9lbmMiOiJBMjU2R0NNIiwiY2xpZW50X2lkX3Rva2VuX3NpZ25lZF9yZXNwb25zZV9hbGciOiJQUzI1NiIsImNsaWVudF9vcmdfaWQ" +
            @"iOiJmZmIxYzhiYS0yNzllLTQ0ZDgtOTZmMC0xYmMzNGE2YjQzNmYiLCJjbGllbnRfb3JnX25hbWUiOiJNb2NrIEZpbmFuY2UgVG9vbHMiLCJjbGllbnRfcmV2b2NhdGlvbl91cmkiOiJod" +
            @"HRwczovL2xvY2FsaG9zdDo5MDAxL3Jldm9jYXRpb24iLCJjbGllbnRfY2xpZW50X2lkX2lzc3VlZF9hdCI6IjE2NTI2Njk5MzgiLCJjbGllbnRfYXBwbGljYXRpb25fdHlwZSI6IndlYiI" +
            @"sImNsaWVudF9wb2xpY3lfdXJpIjoiaHR0cHM6Ly9tb2Nrc29mdHdhcmUvbXlidWRnZXRhcHAvcG9saWN5IiwiY2xpZW50X3Rvc191cmkiOiJodHRwczovL21vY2tzb2Z0d2FyZS9teWJ1Z" +
            @"GdldGFwcC90ZXJtcyIsImNsaWVudF9sZWdhbF9lbnRpdHlfaWQiOiIxOGI3NWE3Ni01ODIxLTRjOWUtYjQ2NS00NzA5MjkxY2YwZjQiLCJjbGllbnRfbGVnYWxfZW50aXR5X25hbWUiOiJ" +
            @"Nb2NrIFNvZnR3YXJlIENvbXBhbnkiLCJjbGllbnRfcmVjaXBpZW50X2Jhc2VfdXJpIjoiaHR0cHM6Ly9sb2NhbGhvc3Q6OTAwMSIsImNsaWVudF9zZWN0b3JfaWRlbnRpZmllcl91cmkiO" +
            @"iJsb2NhbGhvc3QiLCJqdGkiOiI5V0U4cWJ0Ukhpa29neURTc1gySXJnIiwic29mdHdhcmVfaWQiOiJjNjMyN2Y4Ny02ODdhLTQzNjktOTlhNC1lYWFjZDNiYjgyMTAiLCJzZWN0b3JfaWR" +
            @"lbnRpZmllcl91cmkiOiJsb2NhbGhvc3QiLCJjbmYiOnsieDV0I1MyNTYiOiI3MTVDREQwNEZGNzMzMkNDREE3NENERjlGQkVEMTZCRUJBNURENzQ0In0sInNjb3BlIjpbImNkcjpyZWdpc" +
            @"3RyYXRpb24iXX0.gg7pSRRLYpM69cDBg83LijyKOvnHf79ySsUe007s4Yy0eFe0ALnA_sbdxyaeoARVc0Rftg0Mpck6PLE0u3zJAsuNm6tV4r3jVf5m38EbvYB5N8cl18Z04PjoKvhVKhZ" +
            @"5yM3wHYM6_eelSvv0aWkptRYDDcVCa4_H93RmiPJt5RoUX2SR7lf8gdHM9fb-n1_OcIVdEDz9W6RUw1o3TFp5kh3xIlS_sIawJ5dGTztnj3VtI36d7qL59uPojPmUQ-OT22IZE-u_KZxAe" +
            @"tUQhX0-IqUzdVsdTf2t9DNva2VRkK9Cdf2kCtqs17NGDlWceQ7IKR-U6qn9izNOYeM47Qhqa6MiROtrfe5Ja3p8vjnN72eEQ_XPd2bMVxkbyh0IrG9-5JCOolbjjnbZaxh4dIggfdY52JS" +
            @"2-DLYhQnMnJtrVkKe1J212x8SVf7FKcNGY0OM4MLG3Gcl5S8EzQQuh464Nr-rPnec7SbrqjQ2xyn56s4Nhv5PfQ-VOqPQXOkyBPzH";


        // VSCode slows on excessively long lines, splitting string constant into smaller lines.
        public const string EXPIRED_CONSUMER_ACCESS_TOKEN =
            @"eyJhbGciOiJQUzI1NiIsImtpZCI6IjdDNTcxNjU1M0U5QjEzMkVGMzI1QzQ5Q0EyMDc5NzM3MTk2QzAzREIiLCJ4NXQiOiJmRmNXVlQ2YkV5N3pKY1Njb2dlWE54bHNBOXMiLCJ0eXAiOiJhdCtqd3QifQ.eyJuYmYiOjE2NTI2ODM2NDksImV4cCI6MTY1MjY4NzI0OSwiaXNzIjoiaHR0cHM6Ly9tb2NrLWRh" +
            @"dGEtaG9sZGVyOjgwMDEiLCJhdWQiOiJjZHMtYXUiLCJjbGllbnRfaWQiOiJjNjMyN2Y4Ny02ODdhLTQzNjktOTlhNC1lYWFjZDNiYjgyMTAiLCJhdXRoX3RpbWUiOjE2NTI2ODM2NDksImlkcCI6ImxvY2FsIiwic2hhcmluZ19leHBpcmVzX2F0IjoxNjYwNDU5NjQ5LCJjZHJfYXJyYW5nZW1lbnRfaWQiOiI" +
            @"xZjc4YTY3ZS0xZDhkLTRiNzctODI3OC04NTNiMmQ5NTM5YjMiLCJqdGkiOiItNWZ0ZTJ1azMwTGZsa1g2N2JFVUx3Iiwic29mdHdhcmVfaWQiOiJjNjMyN2Y4Ny02ODdhLTQzNjktOTlhNC1lYWFjZDNiYjgyMTAiLCJzZWN0b3JfaWRlbnRpZmllcl91cmkiOiJtb2NrLWRhdGEtaG9sZGVyLWludGVncmF0aW" +
            @"9uLXRlc3RzIiwiYWNjb3VudF9pZCI6WyJiMjNtTnRTWGdwU0s2WVFsMGdOcE9NVUxlQUhFbnpRMW54YXJiTzlTUDl1NWpEbjNhOWw0RDV6azd3YndFcVFiIiwiVG1vNW96aStNeHU5cVNVQVJCeWpKMjh2VjJrM2srRVBGMHBicHJsODVFSDhJJTJGTnVaTERRb09TbW9NZTRGeVpkIl0sInN1YiI6IkgvK0VrU" +
            @"0xWeE1nZjhwNXlJS3BScFNzTlRSZVk5VTZlYTNGWVZtUWFSL1ZwQ2pHVGpSenA4YTdDOXVjWEk3ak0iLCJjbmYiOnsieDV0I1MyNTYiOiI3MTVDREQwNEZGNzMzMkNDREE3NENERjlGQkVEMTZCRUJBNURENzQ0In0sInNjb3BlIjpbIm9wZW5pZCIsInByb2ZpbGUiLCJiYW5rOmFjY291bnRzLmJhc2ljOnJl" +
            @"YWQiLCJiYW5rOnRyYW5zYWN0aW9uczpyZWFkIiwiY29tbW9uOmN1c3RvbWVyLmJhc2ljOnJlYWQiXSwiYW1yIjpbInB3ZCJdfQ.g_x-MZa6Lq_2m3D0DKKk9SEhHs2TUIF_3oyWf2E18873KI3q6YCom7zFYpIrxrtgmcW8jN1gSMZFx_b9FhvWXRCL48GFLgVmqUml6yPNLH9Oa95UziIS58ROSxkOkidaIEbM" +
            @"CIfE-6jTl_VzYxE7G19STYYbC_zU3e8hkgDShdh-KpKHW_TgWd_gvHAwHYNJF8TeFnXiAZSOSd4bfO2v9hjDWRN1SA0O-dkZssfthNZxGCsBc0yJfGYi5887KsuWhH1EMTWcAXRAfImeRa6rSgvTZu9imFFzomzdHR5KVD_L5Dq0Q4JtAlu4TmT5RIMWmQEaz7G3JvTfMAxfXkBEqe2UNP4Bm7Npgat9eCH6SS1" + 
            @"daaIJExpJF9C61C1PuV7t1fDHH3pz30H2rBJWZ_gXZJc2xUvw9oAFiJC10iB4dfd1nLgRbFfMx-i84aOfPm6bH-IoTEk1iJ4Odm-FkI9S_MO1rlpYVcuEBCuUKri6PCYKzHU4_istqN8x7bQ_kQQf";

        // Certificate used by DataRecipient for the MTLS connection with Dataholder
        public const string DH_MTLS_CERTIFICATE_FILENAME = "Certificates/server.pfx";
        public const string DH_MTLS_CERTIFICATE_PASSWORD = "#M0ckDataHolder#";

        // Certificate used by DataRecipient to sign client assertions
        public const string CERTIFICATE_FILENAME = "Certificates/client.pfx";
        public const string CERTIFICATE_PASSWORD = "#M0ckDataRecipient#";

        public const string JWT_CERTIFICATE_FILENAME = "Certificates/MDR/jwks.pfx";
        public const string JWT_CERTIFICATE_PASSWORD = "#M0ckDataRecipient#";

        // An additional certificate used by DataRecipient to sign client assertions
        protected const string ADDITIONAL_CERTIFICATE_FILENAME = "Certificates/client-additional.pfx";
        protected const string ADDITIONAL_CERTIFICATE_PASSWORD = CERTIFICATE_PASSWORD;

        protected const string INVALID_CERTIFICATE_FILENAME = "Certificates/client-invalid.pfx";
        protected const string INVALID_CERTIFICATE_PASSWORD = "#M0ckDataRecipient#";

        public const string DATAHOLDER_CERTIFICATE_FILENAME = "Certificates/mock-data-holder.pfx";
        public const string DATAHOLDER_CERTIFICATE_PASSWORD = "#M0ckDataHolder#";

        public static string DH_MTLS_GATEWAY_URL => Configuration["URL:DH_MTLS_Gateway"]
            ?? throw new ConfigurationErrorsException($"{nameof(DH_MTLS_GATEWAY_URL)} - configuration setting not found");

        public static string DH_MTLS_IDENTITYSERVER_TOKEN_URL => DH_MTLS_GATEWAY_URL + "/idp/connect/token"; // DH IdentityServer Token API

        public static string DH_TLS_IDENTITYSERVER_BASE_URL => Configuration["URL:DH_TLS_IdentityServer"]
            ?? throw new ConfigurationErrorsException($"{nameof(DH_TLS_IDENTITYSERVER_BASE_URL)} - configuration setting not found");

        public static string DH_TLS_PUBLIC_BASE_URL => Configuration["URL:DH_TLS_Public"]
            ?? throw new ConfigurationErrorsException($"{nameof(DH_TLS_PUBLIC_BASE_URL)} - configuration setting not found");

        public static string REGISTER_MTLS_URL => Configuration["URL:Register_MTLS"]
            ?? throw new ConfigurationErrorsException($"{nameof(REGISTER_MTLS_URL)} - configuration setting not found");

        public static string REGISTER_MTLS_TOKEN_URL => REGISTER_MTLS_URL + "/idp/connect/token"; // Register Token API

        public static string REGISTRATION_AUDIENCE_URI => DH_TLS_IDENTITYSERVER_BASE_URL;

        public const string ID_TYPE_ACCOUNT = "ACCOUNT_ID";
        public const string ID_TYPE_TRANSACTION = "TRANSACTION_ID";

        // Connection strings
        static public string DATAHOLDER_CONNECTIONSTRING => Configuration["ConnectionStrings:DataHolder"]
            ?? throw new ConfigurationErrorsException($"{nameof(DATAHOLDER_CONNECTIONSTRING)} - configuration setting not found");
        static public string IDENTITYSERVER_CONNECTIONSTRING => Configuration["ConnectionStrings:IdentityServer"]
            ?? throw new ConfigurationErrorsException($"{nameof(IDENTITYSERVER_CONNECTIONSTRING)} - configuration setting not found");
        static public string REGISTER_CONNECTIONSTRING => Configuration["ConnectionStrings:Register"]
            ?? throw new ConfigurationErrorsException($"{nameof(REGISTER_CONNECTIONSTRING)} - configuration setting not found");

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
        public const string SOFTWAREPRODUCT_ID = "c6327f87-687a-4369-99a4-eaacd3bb8210";
        public const string SOFTWAREPRODUCT_ID_INVALID = "f00f00f0-f00f-f00f-f00f-f00f00f00f00";

        public static string MDH_INTEGRATION_TESTS_HOST => Configuration["URL:MDH_INTEGRATION_TESTS_HOST"]
            ?? throw new ConfigurationErrorsException($"{nameof(MDH_INTEGRATION_TESTS_HOST)} - configuration setting not found");

        public static string MDH_HOST => Configuration["URL:MDH_HOST"]
            ?? throw new ConfigurationErrorsException($"{nameof(MDH_HOST)} - configuration setting not found");

        public const string SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS = "<MDH_INTEGRATION_TESTS_HOST>:9999/consent/callback";
        //static public string SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS => SubstituteConstantToken(CONSTANT_SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS);

        public const string SOFTWAREPRODUCT_JWKS_URI_FOR_INTEGRATION_TESTS = "<MDH_INTEGRATION_TESTS_HOST>:9998/jwks";
        //static public string SOFTWAREPRODUCT_JWKS_URI_FOR_INTEGRATION_TESTS => SubstituteConstantToken(CONSTANT_SOFTWAREPRODUCT_JWKS_URI_FOR_INTEGRATION_TESTS);

        // Constants are needed for attributes on test facts/theories and for method parameter defaults, 
        // but we need dynamic values that vary at runtime based on the appsettings.json in use,
        // Hence this method turns the constant value into a dynamic value by replacing "tokens" in the constant.
        public static string SubstituteConstant(string? c)
        {
            if (c == null)
                throw new ArgumentNullException(nameof(c));

            return c
                .Replace("<MDH_INTEGRATION_TESTS_HOST>", MDH_INTEGRATION_TESTS_HOST)
                .Replace("<MDH_HOST>", MDH_HOST);
        }

        public const string SOFTWAREPRODUCT_SECTOR_IDENTIFIER_URI = "api.mocksoftware";

        // Additional brand/software product
        public const string ADDITIONAL_BRAND_ID = "20C0864B-CEEF-4DE0-8944-EB0962F825EB";
        public const string ADDITIONAL_SOFTWAREPRODUCT_ID = "9381DAD2-6B68-4879-B496-C1319D7DFBC9";

        public static string ADDITIONAL_SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS => $"{MDH_INTEGRATION_TESTS_HOST}:9997/consent/callback";        

        public static string ADDITIONAL_SOFTWAREPRODUCT_JWKS_URI_FOR_INTEGRATION_TESTS => $"{MDH_INTEGRATION_TESTS_HOST}:9996/jwks";        

        public const string ADDITIONAL_JWKS_CERTIFICATE_FILENAME = ADDITIONAL_CERTIFICATE_FILENAME;
        public const string ADDITIONAL_JWKS_CERTIFICATE_PASSWORD = ADDITIONAL_CERTIFICATE_PASSWORD;

        // Scope
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
                    return null;
                }
            }

            expectedJson.Should().NotBeNullOrEmpty();
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

        public const string CUSTOMERID_JANEWILSON = "bfb689fb-7745-45b9-bbaa-b21e00072447";
        public const string CUSTOMERID_BUSINESS1 = "a97ba8d9-c89d-4126-a3b1-5aaa50f8dc5f";

        public const string ACCOUNTID_JOHN_SMITH = "1122334455";
        public const string ACCOUNTID_JANE_WILSON = "98765988";

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
                            UserId = tokenType.UserId(),
                            OTP = AUTHORISE_OTP,
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

        public static async Task<DataHolder_Token_API.Response> GetToken(TokenType tokenType,
            int tokenLifetime = 3600,
            int sharingDuration = 7776000)
        {
            // Perform authorise and consent flow and get authCode
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = tokenType switch
                {
                    TokenType.JANE_WILSON => BaseTest.USERID_JANEWILSON,
                    _ => throw new ArgumentException($"{nameof(GetToken)} - Unsupported token type - {tokenType}")
                },
                SelectedAccountIds = tokenType switch
                {
                    TokenType.JANE_WILSON => ACCOUNTIDS_ALL_JANE_WILSON,
                    _ => throw new ArgumentException($"{nameof(GetToken)} - Unsupported token type - {tokenType}")
                },
                OTP = BaseTest.AUTHORISE_OTP,
                Scope = SCOPE,
                TokenLifetime = tokenLifetime,
                SharingDuration = sharingDuration
            }.Authorise();

            // User authCode to get tokens
            var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);
            if (tokenResponse == null) throw new NullException($"{nameof(GetToken)} - TokenResponse is null");
            if (tokenResponse.IdToken == null) throw new NullException($"{nameof(GetToken)} - Id token is null");
            if (tokenResponse.AccessToken == null) throw new NullException($"{nameof(GetToken)} - Access token is null");
            if (tokenResponse.RefreshToken == null) throw new NullException($"{nameof(GetToken)} - Refresh token is null");
            if (tokenResponse.CdrArrangementId == null) throw new NullException($"{nameof(GetToken)} - CdrArrangementId is null");

            // Return access token
            return tokenResponse;
        }

        protected enum Table { LEGALENTITY, BRAND, SOFTWAREPRODUCT }

        static protected string GetStatus(Table table, string id)
        {
            using var connection = new SqlConnection(DATAHOLDER_CONNECTIONSTRING);
            connection.Open();

            using var selectCommand = new SqlCommand($"select status from {table} where {table}ID = @id", connection);
            selectCommand.Parameters.AddWithValue("@id", id);

            return selectCommand.ExecuteScalarString();
        }

        static protected void SetStatus(Table table, string id, string status)
        {
            using var connection = new SqlConnection(DATAHOLDER_CONNECTIONSTRING);
            connection.Open();

            using var updateCommand = new SqlCommand($"update {table} set status = @status where {table}ID = @id", connection);
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
        static protected void ExtractClaimsFromToken(string? accessToken, out string customerId, out string softwareProductId, bool useRandomIV = true)
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

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
