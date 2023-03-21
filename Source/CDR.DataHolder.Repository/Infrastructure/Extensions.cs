using CDR.DataHolder.Repository.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CDR.DataHolder.Repository.Infrastructure
{
    public static class Extensions
    {
        private static Regex datetimeMatchRegex = new Regex("[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}Z", RegexOptions.Compiled);

        /// <summary>
        /// This is the initial database seed. If there are records in the database, this will not re-seed the database
        /// </summary>
        public async static Task SeedDatabaseFromJsonFile(
            this DataHolderDatabaseContext dataHolderDatabaseContext,
            string jsonFileFullPath,
            ILogger logger,
            bool overwriteExistingData = false,
            bool offsetDates = true)
        {
            if (!File.Exists(jsonFileFullPath))
            {
                logger.LogDebug("Seed data file '{jsonFileFullPath}' not found.", jsonFileFullPath);
                return;
            }

            var json = await File.ReadAllTextAsync(jsonFileFullPath);
            await dataHolderDatabaseContext.SeedDatabaseFromJson(json, logger, overwriteExistingData, offsetDates);
        }

        /// <summary>
        /// This is the initial database seed. If there are records in the database, this will not re-seed the database
        /// </summary>
        public async static Task SeedDatabaseFromJson(
            this DataHolderDatabaseContext dataHolderDatabaseContext,
            string json,
            ILogger logger,
            bool overwriteExistingData = false,
            bool offsetDates = true)
        {
            bool hasExistingData = await dataHolderDatabaseContext.Customers.AnyAsync();
            if (hasExistingData && !overwriteExistingData)
            {
                // DO NOT REMOVE or ALTER this is referenced in the health check
                logger.LogInformation("Seed-Data:not-imported");
                return;
            }

            logger.LogInformation("{message}", hasExistingData ?
                 "Existing data found, but set to overwrite.  Seeding data..." :
                 "No existing data found.  Seeding data...");

            await dataHolderDatabaseContext.ReSeedDatabaseFromJson(json, logger, offsetDates);
        }

        /// <summary>
        /// Re-Seed the database from the input JSON data. All existing data in the database will be removed prior to creating the new data set.
        /// </summary>
        public async static Task ReSeedDatabaseFromJson(this DataHolderDatabaseContext dataHolderDatabaseContext, string json, ILogger logger, bool offsetDates = true)
        {
            using (var transaction = dataHolderDatabaseContext.Database.BeginTransaction())
            {
                try
                {
                    logger.LogInformation("Removing the existing data from the repository...");

                    // Remove all existing account data in the system
                    var existingTxns = await dataHolderDatabaseContext.Transactions.AsNoTracking().ToListAsync();
                    var existingCustomers = await dataHolderDatabaseContext.Customers.AsNoTracking().ToListAsync();
                    var existingPersons = await dataHolderDatabaseContext.Persons.AsNoTracking().ToListAsync();
                    var existingOrgs = await dataHolderDatabaseContext.Organisations.AsNoTracking().ToListAsync();

                    dataHolderDatabaseContext.RemoveRange(existingTxns);
                    dataHolderDatabaseContext.SaveChanges();

                    dataHolderDatabaseContext.RemoveRange(existingCustomers);
                    dataHolderDatabaseContext.SaveChanges();

                    dataHolderDatabaseContext.RemoveRange(existingPersons);
                    dataHolderDatabaseContext.SaveChanges();

                    dataHolderDatabaseContext.RemoveRange(existingOrgs);
                    dataHolderDatabaseContext.SaveChanges();

                    logger.LogInformation("Existing data removed from the repository.");
                    logger.LogInformation("Adding Seed data to repository...");

                    // Offset seed data relative to the current date.
                    // The out-of-the-box seed data has been created relative to the baseline data 2021-05-01.
                    // That means, all the transaction datetimes in the "past" and "future" are relative to that date.
                    // When running, the we have to offset the baseline date to the current date in order to keep the record set relavent.
                    if (offsetDates)
                    {
                        var dataBaseline = new DateTime(2021, 05, 01);
                        var nowDate = DateTime.UtcNow;
                        json = datetimeMatchRegex.Replace(json, (match) =>
                        {
                            return nowDate.Add(DateTime.Parse(match.Value) - dataBaseline).ToString("yyyy-MM-ddThh:mm:ssZ");
                        });
                    }

                    // Re-create all participants from the incoming JSON file.
                    var allData = JsonConvert.DeserializeObject<JObject>(json);
                    var newCustomers = allData["Customers"].ToObject<Customer[]>();                    
                    dataHolderDatabaseContext.Customers.AddRange(newCustomers);                    
                    dataHolderDatabaseContext.SaveChanges();

                    // Finally commit the transaction
                    transaction.Commit();

                    // DO NOT REMOVE or ALTER this is referenced in the health check
                    logger.LogInformation("Seed-Data:imported");
                }
                catch (Exception ex)
                {
                    // Log any errors.
                    logger.LogError(ex, "Error while seeding the database.");
                    throw;
                }
            }
        }
    }
}
