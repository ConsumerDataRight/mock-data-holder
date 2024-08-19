using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace CDR.DataHolder.Shared.Repository.Infrastructure
{
    public static class Extensions
    {
        private static readonly Regex datetimeMatchRegex = new Regex("[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}Z", RegexOptions.Compiled);

        /// <summary>
        /// This is the initial database seed. If there are records in the database, this will not re-seed the database
        /// </summary>
        public async static Task SeedDatabaseFromJsonFile(
            this DbContext dataHolderDatabaseContext,
            string jsonFileFullPath,
            ILogger logger,
            HealthCheckStatuses healthStatuses,
            bool overwriteExistingData = false,
            bool offsetDates = true)
        {
            if (!File.Exists(jsonFileFullPath))
            {
                logger.LogDebug("Seed data file '{jsonFileFullPath}' not found.", jsonFileFullPath);
                return;
            }

            var json = await File.ReadAllTextAsync(jsonFileFullPath);
            await dataHolderDatabaseContext.SeedDatabaseFromJson(json, logger, healthStatuses, overwriteExistingData, offsetDates);
        }

        /// <summary>
        /// This is the initial database seed. If there are records in the database, this will not re-seed the database
        /// </summary>
        public async static Task SeedDatabaseFromJson(
            this DbContext dataHolderDatabaseContext,
            string json,
            ILogger logger,
            HealthCheckStatuses healthStatuses,
            bool overwriteExistingData = false,
            bool offsetDates = true)
        {
            var industryDbContext = dataHolderDatabaseContext as IIndustryDbContext ?? throw new InvalidCastException($"{nameof(IIndustryDbContext)} not implemented");
            var hasExistingData = await industryDbContext.HasExistingData();
            if (hasExistingData && !overwriteExistingData)
            {
                logger.LogInformation("Seed-Data:not-imported");
                healthStatuses.SeedingStatus = SeedingStatus.Succeeded;
                return;
            }

            logger.LogInformation("{message}", hasExistingData ?
                 "Existing data found, but set to overwrite.  Seeding data..." :
                 "No existing data found.  Seeding data...");

            await dataHolderDatabaseContext.ReSeedDatabaseFromJson(json, logger, healthStatuses, offsetDates);
        }

        /// <summary>
        /// Re-Seed the database from the input JSON data. All existing data in the database will be removed prior to creating the new data set.
        /// </summary>
        public async static Task ReSeedDatabaseFromJson(this DbContext dataHolderDatabaseContext, string json, ILogger logger, HealthCheckStatuses healthStatuses, bool offsetDates = true)
        {
            using var transaction = dataHolderDatabaseContext.Database.BeginTransaction();

            try
            {
                logger.LogInformation("Removing the existing data from the repository...");

                if (dataHolderDatabaseContext is not IIndustryDbContext industryDbContext)
                {                    
                    logger.LogInformation("Unable to get {IndustryDbContext}", nameof(IIndustryDbContext));
                    return;
                }

                await industryDbContext.RemoveExistingData();

                logger.LogInformation("Existing data removed from the repository.");
                logger.LogInformation("Adding Seed data to repository...");

                // Offset seed data relative to the current date.
                // The out-of-the-box seed data has been created relative to the baseline data 2021-05-01.
                // That means, all the transaction datetimes in the "past" and "future" are relative to that date.
                // When running, the we have to offset the baseline date to the current date in order to keep the record set relavent.
                if (offsetDates)
                {
                    OffsetDates(json);
                }

                // Re-create all participants from the incoming JSON file.
                var allData = JsonConvert.DeserializeObject<JObject>(json);
                if(allData == null)
                {
                    logger.LogInformation("No seed data to be imported.");
                    return;
                }

                industryDbContext.ReCreateParticipants(allData);

                // Finally commit the transaction
                transaction.Commit();

                logger.LogInformation("Seed-Data:imported");
                healthStatuses.SeedingStatus = SeedingStatus.Succeeded;
            }
            catch (Exception ex)
            {
                healthStatuses.SeedingStatus = SeedingStatus.Failed;
                // Log any errors.
                logger.LogError(ex, "Error while seeding the database.");
                throw;
            }
        }

        private static void OffsetDates(string json)
        {
            var dataBaseline = new DateTime(2021, 05, 01);
            var nowDate = DateTime.UtcNow;
            datetimeMatchRegex.Replace(json, (match) =>
            {
                return nowDate.Add(DateTime.Parse(match.Value) - dataBaseline).ToString("yyyy-MM-ddThh:mm:ssZ");
            });
        }
    }
}
