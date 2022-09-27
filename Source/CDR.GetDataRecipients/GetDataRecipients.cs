using CDR.DataHolder.Repository;
using CDR.DataHolder.Repository.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CDR.GetDataRecipients
{
    public static class GetDataRecipientsFunction
    {
        public static string dbLoggingConnString;
        public static string dbConnString;

        /// <summary>
        /// Get Data Recipients Function
        /// </summary>
        /// <remarks>Gets the Data Recipients from the Register and updates the local repository</remarks>
        [FunctionName("GetDataRecipients")]
        public static async Task DATARECIPIENTS([TimerTrigger("%Schedule%")] TimerInfo myTimer, ILogger log, ExecutionContext context)
        {
            try
            {
                var isLocalDev = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT").Equals("Development");
                var configBuilder = new ConfigurationBuilder().SetBasePath(context.FunctionAppDirectory);

                if (isLocalDev)
                {
                    configBuilder = configBuilder.AddJsonFile("local.settings.json", optional: false, reloadOnChange: true);
                }

                var config = configBuilder.AddEnvironmentVariables().Build();

                // Set connection strings.
                dbLoggingConnString = Environment.GetEnvironmentVariable("DataHolder_Logging_DB_ConnectionString");
                dbConnString = Environment.GetEnvironmentVariable("DataHolder_DB_ConnectionString");

                string dataRecipientsEndpoint = Environment.GetEnvironmentVariable("Register_GetDataRecipients_Endpoint");
                string xvVer = Environment.GetEnvironmentVariable("Register_GetDataRecipients_XV");
                bool ignoreServerCertificateErrors = Environment.GetEnvironmentVariable("Ignore_Server_Certificate_Errors").Equals("true", StringComparison.OrdinalIgnoreCase);

                (string dataRecipientJson, System.Net.HttpStatusCode respStatusCode) = await GetDataRecipients(dataRecipientsEndpoint, xvVer, log, ignoreServerCertificateErrors);
                if (respStatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Get Register -> Data Recipients
                    JsonSerializerSettings jss = new()
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    };
                    var data = JsonConvert.DeserializeObject<JObject>(dataRecipientJson, jss);
                    var dataRecipients = data["data"].ToObject<LegalEntity[]>();
                    IList<LegalEntity> regDataRecipients = new List<LegalEntity>();
                    foreach (var item in dataRecipients)
                    {
                        regDataRecipients.Add(item);
                    }

                    // Get Data Holder -> Data Recipients
                    (IList<LegalEntity> dhDataRecipients, Exception ex) = await new DataRecipientRepository(dbConnString).GetDHDataRecipients();

                    // Compare Register and Data Holder -> Data Recipients
                    await CompareDataRecipients(regDataRecipients, dhDataRecipients);

                }
            }
            catch (Exception ex)
            {
                await InsertDBLog(dbLoggingConnString, "Error", "Exception", "DATARECIPIENTS", ex);
            }
        }

        /// <summary>
        /// Get the list of Data Recipients from the Register
        /// </summary>
        /// <returns>Raw data</returns>
        private static async Task<(string, System.Net.HttpStatusCode)> GetDataRecipients(
            string dataRecipientsEndpoint, 
            string version,
            ILogger log,
            bool ignoreServerCertificateErrors = false)
        {
            var client = GetHttpClient(version, ignoreServerCertificateErrors);

            log.LogInformation("Retrieving data recipients from the Register: {dataRecipientsEndpoint}", dataRecipientsEndpoint);
            var response = await client.GetAsync(dataRecipientsEndpoint);
            var data = await response.Content.ReadAsStringAsync();
            log.LogInformation("Register response: {statusCode} - {body}", response.StatusCode, data);
            return (data, response.StatusCode);
        }

        private static HttpClient GetHttpClient(
            string version = null,
            bool ignoreServerCertificateErrors = false)
        {
            var clientHandler = new HttpClientHandler();

            if (ignoreServerCertificateErrors)
            {
                clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            }

            var client = new HttpClient(clientHandler);

            // Add the x-v header to the request if provided.
            if (!string.IsNullOrEmpty(version))
            {
                client.DefaultRequestHeaders.Add("x-v", version);
            }

            return client;
        }

        private static async Task CompareDataRecipients(IList<LegalEntity> regDataRecipients, IList<LegalEntity> dhDataRecipients)
        {
            try
            {
                IList<LegalEntity> insDataRecipients = new List<LegalEntity>();
                IList<LegalEntity> delDataRecipients = new List<LegalEntity>();
                IList<Brand> insBrands = new List<Brand>();
                IList<Brand> delBrands = new List<Brand>();
                IList<SoftwareProduct> insSwProds = new List<SoftwareProduct>();
                IList<SoftwareProduct> delSwProds = new List<SoftwareProduct>();
                IList<LegalEntity> updDataRecipients = new List<LegalEntity>();

                if (regDataRecipients.Any())
                {
                    // Compare each Register to Data Holder repo Data Recipient
                    foreach (var regDataRecipient in regDataRecipients)
                    {
                        await ProcessCompareDataRecipients(dhDataRecipients, regDataRecipient, insDataRecipients, insBrands, delBrands, insSwProds, delSwProds, updDataRecipients);
                    }

                    // Are there ANY Data Recipients in the Data Holder that ARE NOT in the Register?
                    IList<LegalEntity> dhDrs = dhDataRecipients.Where(n => !regDataRecipients.Any(o => o.LegalEntityId.CompareTo(n.LegalEntityId) == 0)).ToList();
                    if (dhDrs.Any())
                    {
                        foreach (var dhDr in dhDrs)
                        {
                            delDataRecipients.Add(dhDr);
                        }
                    }
                }
                // Delete all existing if there are no data recipients in register.
                else if (dhDataRecipients.Any())
				{
					foreach (var dhDr in dhDataRecipients)
					{
						delDataRecipients.Add(dhDr);
					}
				}

                // INSERT Register Data Recipients including its child Brands and Software Products into Data Holder repo
                if (insDataRecipients.Any())
                {
                    foreach (var insDr in insDataRecipients)
                    {
                        await InsertDhDataRecipient(insDr);
                    }
                    insDataRecipients.Clear();
                }

                // DELETE Data Holder repo Data Recipients that ARE NOT in the Register
                if (delDataRecipients.Any())
                {
                    await DeleteDhDataRecipients(delDataRecipients);
                    delDataRecipients.Clear();
                }

                // INSERT Register Brands into Data Holder repo
                if (insBrands.Any())
                {
                    foreach (var insBrand in insBrands)
                    {
                        await InsertDhDrBrand(insBrand);
                    }
                    insBrands.Clear();
                }

                // DELETE Brands from Data Holder repo that ARE NOT in the Register
                if (delBrands.Any())
                {
                    await DeleteDhBrands(delBrands);
                    delBrands.Clear();
                }

                // INSERT Register Software Products into Data Holder repo
                if (insSwProds.Any())
                {
                    foreach (var insSwProd in insSwProds)
                    {
                        await InsertDhDrSwProd(insSwProd);
                    }
                    insSwProds.Clear();
                }

                // DELETE Software Products from Data Holder repo that ARE NOT in the Register
                if (delSwProds.Any())
                {
                    await DeleteDhSwProducts(delSwProds);
                    delSwProds.Clear();
                }

                // UPDATE Data Holder repo with Register Data Recipients
                if (updDataRecipients.Any())
                {
                    foreach (var updDr in updDataRecipients)
                    {
                        await UpdateDhDataRecipient(updDr);
                    }
                    updDataRecipients.Clear();
                }
            }
            catch (Exception ex)
            {
                await InsertDBLog(dbLoggingConnString, "Error", "Exception", "CompareDataRecipients", ex);
            }
        }

        private static async Task ProcessCompareDataRecipients(IList<LegalEntity> dhDataRecipients, LegalEntity regDataRecipient, IList<LegalEntity> insDataRecipients, IList<Brand> insBrands, IList<Brand> delBrands, IList<SoftwareProduct> insSwProds, IList<SoftwareProduct> delSwProds, IList<LegalEntity> updDataRecipients)
        {
            try
            {
                // DOES this Register Data Recipient -->
                LegalEntity dhDataRecipient = null;

                // EXIST in the Data Holder repo?
                dhDataRecipient = dhDataRecipients.FirstOrDefault(n => n.LegalEntityId.CompareTo(regDataRecipient.LegalEntityId) == 0);
                if (dhDataRecipient == null)
                {
                    // NO - AM I in the Insert List?
                    var alreadyInList = insDataRecipients.FirstOrDefault(x => x.LegalEntityId.CompareTo(regDataRecipient.LegalEntityId) == 0);
                    if (alreadyInList == null)
                    {
                        insDataRecipients.Add(regDataRecipient);
                    }
                    return;
                }

                // Register Data Recipient -> Legal Entity ONLY - NO Brands or Software Products
                if (!regDataRecipient.Brands.Any())
                {
                    // DO Data Holder Brands EXIST? (if yes then delete them)
                    if (dhDataRecipient.Brands.Any())
                    {
                        foreach (var brand in dhDataRecipient.Brands)
                        {
                            delBrands.Add(brand);
                        }
                    }
                    return;
                }

                // Check if there are more brands than register
                var dhExtraDrBrands = dhDataRecipient.Brands.ExceptBy(regDataRecipient.Brands.Select(b => b.BrandId), b => b.BrandId);
                if (dhExtraDrBrands.Any())
                {
					foreach (var brand in dhExtraDrBrands)
					{
						delBrands.Add(brand);
					}
				}

				foreach (var regDrBrand in regDataRecipient.Brands)
                {
                    // Register Data Recipient -> Legal Entity and Brand ONLY - NO Software Products
                    if (!regDrBrand.SoftwareProducts.Any())
                    {
                        // DO Data Holder Brands EXIST?
                        IList<Brand> dhDrBrands = dhDataRecipient.Brands.Where(x => x.BrandId.CompareTo(regDrBrand.BrandId) == 0).ToList();
                        if (dhDrBrands.Any())
                        {
                            foreach (var brand in dhDrBrands)
                            {
                                // DO Data Holder Software Products EXIST? (if yes then delete them)
                                foreach (var swProd in brand.SoftwareProducts)
                                {
                                    delSwProds.Add(swProd);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Register Data Recipient -> Legal Entity, Brands and Software Products
                        foreach (var regDrSwProd in regDrBrand.SoftwareProducts)
                        {
                            await CompareRegToDh(dhDataRecipients, regDataRecipient, regDrBrand, regDrSwProd, insBrands, insSwProds, delBrands, delSwProds, updDataRecipients);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await InsertDBLog(dbLoggingConnString, "Error", "Exception", "ProcessCompareDataRecipients", ex);
            }
        }

        private static async Task CompareRegToDh(
            IList<LegalEntity> dhDataRecipients, LegalEntity regDr, Brand regDrBrand, SoftwareProduct regDrSwProd, 
            IList<Brand> insBrands, IList<SoftwareProduct> insSwProds,
			IList<Brand> delBrands, IList<SoftwareProduct> delSwProds, IList<LegalEntity> updDrs)
        {
            try
            {
                // DOES this Register Data Recipient --> ;
                LegalEntity dhDataRecipient = null;

                // DOES the Brand exist in the Data Holder repo?
                dhDataRecipient = dhDataRecipients.FirstOrDefault(n => n.Brands.Any(p => p.BrandId.CompareTo(regDrBrand.BrandId) == 0));
                if (dhDataRecipient == null)
                {
                    // NO - AM I in the Insert List?
                    // (plausible to try to INSERT for each of the Brands in the Data Recipient, this violates PRIMARY KEY constraint PK_Brand)
                    var alreadyInList = insBrands.FirstOrDefault(x => x.BrandId.CompareTo(regDrBrand.BrandId) == 0);
                    if (alreadyInList == null)
                    {
                        regDrBrand.LegalEntityId = regDr.LegalEntityId;
                        insBrands.Add(regDrBrand);
                    }
                }
                else
                {
                    // DOES the Software Product exist in the Data Holder repo?
                    dhDataRecipient = dhDataRecipients.FirstOrDefault(n => n.Brands.Any(p => p.SoftwareProducts.Any(q => q.SoftwareProductId.CompareTo(regDrSwProd.SoftwareProductId) == 0)));
                    if (dhDataRecipient == null)
                    {
                        regDrSwProd.BrandId = regDrBrand.BrandId;
                        insSwProds.Add(regDrSwProd);
                    }
                }

                if (dhDataRecipient != null)
                {
                    // DOES the Data Holder repo differ to Register?
                    foreach (var dhDrBrand in dhDataRecipient.Brands)
                    {
                        // Check if the brand has already been marked for deletion
                        if (delBrands.Any(db => db.BrandId == dhDrBrand.BrandId))
                        {
                            continue;
                        }

						if (dhDrBrand.SoftwareProducts.Count > regDrBrand.SoftwareProducts.Count)
                        {
                            foreach (var dhSwProd in dhDrBrand.SoftwareProducts)
                            {
                                // FIND this Data Holder Software Product in the Register Software Products
                                var regSwProd = regDrBrand.SoftwareProducts.FirstOrDefault(x => x.SoftwareProductId.CompareTo(dhSwProd.SoftwareProductId) == 0);
                                if (regSwProd == null)
                                {
                                    // NOT FOUND, REMOVE -> this Data Holder Software Product - it DOES NOT EXIST in the Register
                                    var alreadyInList = delSwProds.FirstOrDefault(x => x.SoftwareProductId.CompareTo(dhSwProd.SoftwareProductId) == 0);
                                    if (alreadyInList == null)
                                        delSwProds.Add(dhSwProd);
                                }
                            }
                        }
                        else if (dhDrBrand.SoftwareProducts.Count == regDrBrand.SoftwareProducts.Count)
                        {
                            foreach (var dhSwProd in dhDrBrand.SoftwareProducts)
                            {
                                LegalEntity upRegDr = await CompareEntityProperties(dhDataRecipient, dhDrBrand, dhSwProd, regDr, regDrBrand, regDrSwProd);
                                if (upRegDr != null)
                                    updDrs.Add(upRegDr);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await InsertDBLog(dbLoggingConnString, "Error", "Exception", "CompareRegToDh", ex);
            }
        }

        private static async Task<LegalEntity> CompareEntityProperties(LegalEntity dhDr, Brand dhDrBrand, SoftwareProduct dhDrSWProd, LegalEntity regDr, Brand regDrBrand, SoftwareProduct regDrSWProd)
        {
            try
            {
                // UPDATE -> Compare Legal Entity properties
                if (dhDr.LegalEntityId.CompareTo(regDr.LegalEntityId) == 0)
                {
                    if (string.Compare(dhDr.LegalEntityName, regDr.LegalEntityName, StringComparison.OrdinalIgnoreCase) != 0)
                        return regDr;

                    else if (string.Compare(dhDr.LogoUri, regDr.LogoUri, StringComparison.OrdinalIgnoreCase) != 0)
                        return regDr;

                    else if (string.Compare(dhDr.Status, regDr.Status, StringComparison.OrdinalIgnoreCase) != 0)
                        return regDr;
                }

                // UPDATE -> Compare Brand entity properties
                if (dhDrBrand.BrandId.CompareTo(regDrBrand.BrandId) == 0)
                {
                    if (string.Compare(dhDrBrand.BrandName, regDrBrand.BrandName, StringComparison.OrdinalIgnoreCase) != 0)
                        return regDr;

                    else if (string.Compare(dhDrBrand.LogoUri, regDrBrand.LogoUri, StringComparison.OrdinalIgnoreCase) != 0)
                        return regDr;

                    else if (string.Compare(dhDrBrand.Status, regDrBrand.Status, StringComparison.OrdinalIgnoreCase) != 0)
                        return regDr;
                }

                // UPDATE -> Compare Software Product entity properties
                if (dhDrSWProd.SoftwareProductId.CompareTo(regDrSWProd.SoftwareProductId) == 0)
                {
                    if (string.Compare(dhDrSWProd.SoftwareProductName, regDrSWProd.SoftwareProductName, StringComparison.OrdinalIgnoreCase) != 0)
                        return regDr;

                    else if (string.Compare(dhDrSWProd.SoftwareProductDescription, regDrSWProd.SoftwareProductDescription, StringComparison.OrdinalIgnoreCase) != 0)
                        return regDr;

                    else if (string.Compare(dhDrSWProd.LogoUri, regDrSWProd.LogoUri, StringComparison.OrdinalIgnoreCase) != 0)
                        return regDr;

                    else if (string.Compare(dhDrSWProd.Status, regDrSWProd.Status, StringComparison.OrdinalIgnoreCase) != 0)
                        return regDr;
                }
            }
            catch (Exception ex)
            {
                await InsertDBLog(dbLoggingConnString, "Error", "Exception", "CompareEntityProperties", ex);
            }
            return null;
        }

        private static async Task InsertDhDataRecipient(LegalEntity regDr)
        {
            Exception ex = await new DataRecipientRepository(dbConnString).InsertDataRecipient(regDr);
            if (ex == null)
                await InsertDBLog(dbLoggingConnString, $"Added - {regDr.LegalEntityName} - ({regDr.LegalEntityId})", "Information", "InsertRegDr");
            else
                await InsertDBLog(dbLoggingConnString, "", "Exception", "InsertDhDataRecipient", ex, SerialiseEntity(regDr));
        }

        private static async Task InsertDhDrBrand(Brand regDrBrand)
        {
            Exception ex = await new DataRecipientRepository(dbConnString).InsertBrand(regDrBrand);
            if (ex == null)
                await InsertDBLog(dbLoggingConnString, $"Added - {regDrBrand.BrandName} - ({regDrBrand.BrandId})", "Information", "InsertRegDrBrand");
            else
                await InsertDBLog(dbLoggingConnString, "", "Exception", "InsertDhDrBrand", ex, SerialiseEntity(regDrBrand));
        }
        private static async Task InsertDhDrSwProd(SoftwareProduct regDrSwProd)
        {
            Exception ex = await new DataRecipientRepository(dbConnString).InsertSoftwareProduct(regDrSwProd);
            if (ex == null)
                await InsertDBLog(dbLoggingConnString, $"Added - {regDrSwProd.SoftwareProductName} - ({regDrSwProd.SoftwareProductId})", "Information", "InsertRegDrSwProd");
            else
                await InsertDBLog(dbLoggingConnString, "", "Exception", "InsertDhDrSwProd", ex, SerialiseEntity(regDrSwProd));
        }

        private static async Task UpdateDhDataRecipient(LegalEntity regDr)
        {
            Exception ex = await new DataRecipientRepository(dbConnString).UpdateDataRecipient(regDr);
            if (ex == null)
                await InsertDBLog(dbLoggingConnString, $"Updated - {regDr.LegalEntityName} - ({regDr.LegalEntityId})", "Information", "UpdateDhDr");
            else
                await InsertDBLog(dbLoggingConnString, "", "Exception", "UpdateDhDataRecipient", ex, SerialiseEntity(regDr));
        }

        private static async Task DeleteDhDataRecipients(IList<LegalEntity> dhDataRecipients)
        {
            Exception ex = await new DataRecipientRepository(dbConnString).DeleteDataRecipients(dhDataRecipients);
            if (ex == null)
            {
                foreach (var dhDataRecipient in dhDataRecipients)
                {
                    await InsertDBLog(dbLoggingConnString, $"Deleted - {dhDataRecipient.LegalEntityName} - ({dhDataRecipient.LegalEntityId})", "Information", "DeleteDhDataRecipients");
                }
            }
            else
            {
                await InsertDBLog(dbLoggingConnString, "", "Exception", "DeleteDhDataRecipients", ex, SerialiseEntity(dhDataRecipients));
            }
        }

        private static async Task DeleteDhBrands(IList<Brand> dhBrands)
        {
            Exception ex = await new DataRecipientRepository(dbConnString).DeleteBrands(dhBrands);
            if (ex == null)
            {
                foreach (var dhBrand in dhBrands)
                {
                    await InsertDBLog(dbLoggingConnString, $"Deleted - {dhBrand.BrandName} - ({dhBrand.BrandId})", "Information", "DeleteDhBrands");
                }
            }
            else
            {
                await InsertDBLog(dbLoggingConnString, "", "Exception", "DeleteDhBrands", ex, SerialiseEntity(dhBrands));
            }
        }

        private static async Task DeleteDhSwProducts(IList<SoftwareProduct> dhSwProds)
        {
            Exception ex = await new DataRecipientRepository(dbConnString).DeleteSoftwareProduct(dhSwProds);
            if (ex == null)
            {
                foreach (var dhSwProd in dhSwProds)
                {
                    await InsertDBLog(dbLoggingConnString, $"Deleted - {dhSwProd.SoftwareProductName} - ({dhSwProd.SoftwareProductId})", "Information", "DeleteDhSwProds");
                }
            }
            else
            {
                await InsertDBLog(dbLoggingConnString, "", "Exception", "DeleteDhSwProducts", ex, SerialiseEntity(dhSwProds));
            }
        }

        private static string SerialiseEntity(object ent)
        {
            JsonSerializerSettings jss = new()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            return JsonConvert.SerializeObject(ent, jss);
        }

        /// <summary>
        /// Update the Log table
        /// </summary>
        private static async Task InsertDBLog(string dbConnString, string msg, string lvl, string methodName, Exception exMsg = null, string entity = "")
        {
            string exMessage = "";

            if (exMsg != null)
            {
                Exception innerException = exMsg;
                StringBuilder innerMsg = new();
                int ctr = 0;

                do
                {
                    // skip the first inner exeception message as it is the same as the exception message
                    if (ctr > 0)
                    {
                        innerMsg.Append(string.IsNullOrEmpty(innerException.Message) ? string.Empty : innerException.Message);
                        innerMsg.Append("\r\n");
                    }
                    else
                    {
                        ctr++;
                    }

                    innerException = innerException.InnerException;
                }
                while (innerException != null);

                // USE the EXCEPTION MESSAGE
                if (innerMsg.Length == 0)
                    exMessage = exMsg.Message;

                // USE the INNER EXCEPTION MESSAGE (INCLUDES the EXCEPTION MESSAGE)	
                else
                    exMessage = innerMsg.ToString();

                // Include the serialised entity for use with EXCEPTION message ONLY
                if (!string.IsNullOrEmpty(entity))
                    exMessage += "\r\nEntity: " + entity;

                exMessage = exMessage.Replace("'", "");
            }

            using (SqlConnection db = new(dbConnString))
            {
                db.Open();
                var cmdText = "";

                if (string.IsNullOrEmpty(exMessage))
                    cmdText = $"INSERT INTO [LogEventsDrService] ([Message], [Level], [TimeStamp], [ProcessName], [MethodName], [SourceContext]) VALUES (@msg,@lvl,GETUTCDATE(),@procName,@methodName,@srcContext)";
                else
                    cmdText = $"INSERT INTO [LogEventsDrService] ([Message], [Level], [TimeStamp], [Exception], [ProcessName], [MethodName], [SourceContext]) VALUES (@msg,@lvl,GETUTCDATE(), @exMessage,@procName,@methodName,@srcContext)";

                using var cmd = new SqlCommand(cmdText, db);
                cmd.Parameters.AddWithValue("@msg", msg);
                cmd.Parameters.AddWithValue("@lvl", lvl);
                cmd.Parameters.AddWithValue("@exMessage", exMessage);
                cmd.Parameters.AddWithValue("@procName", "Azure Function");
                cmd.Parameters.AddWithValue("@methodName", methodName);
                cmd.Parameters.AddWithValue("@srcContext", "CDR.GetDataRecipients");
                await cmd.ExecuteNonQueryAsync();
                db.Close();
            }
        }
    }
}