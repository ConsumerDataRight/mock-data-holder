// #define DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON

using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using Newtonsoft.Json;
using Xunit;
using FluentAssertions;
using FluentAssertions.Execution;
using System.Net.Http;
using System.Net;

#nullable enable

namespace CDR.GetDataRecipients.IntegrationTests
{
    // 28724
    public class US28391_GetDataRecipients : BaseTest
    {
        private async Task ExecuteAzureFunction()
        {
            var client = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Get, $"{AZUREFUNCTIONS_URL}/INTEGRATIONTESTS_DATARECIPIENTS");

            var response = await client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"Expected OK calling {request.RequestUri} but got {response.StatusCode}");
            }
        }

        private async Task Test(
            int registerLegalEntityCount, int registerBrandCount, int registerSoftwareProductCount,
            int dataHolderLegalEntityCount, int dataHolderBrandCount, int dataHolderSoftwareProductCount,
            bool registerModified = false, bool dataHolderModified = false)
        {
            // Arrange
            await DatabaseSeeder.Execute(
                registerLegalEntityCount, registerBrandCount, registerSoftwareProductCount,
                dataHolderLegalEntityCount, dataHolderBrandCount, dataHolderSoftwareProductCount,
                registerModified, dataHolderModified
            );

            // Act
            await ExecuteAzureFunction();

            // Assert
            using (new AssertionScope())
            {
                await Assert_RegisterAndDataHolderIsSynced();
            }
        }

        [Theory]
        [InlineData(0, 0, 0)] // no records
        [InlineData(1, 0, 0)] // has DH legalentity - FAILS - doesnt delete extra legalentity
        [InlineData(1, 1, 0)] // has DH legalentity & brand - FAILS - doesnt delete extra legalentity, brand
        [InlineData(1, 1, 1)] // has DH legalentity & brand & softwareproduct - FAILS - doesnt delete extra legalentity, brand, softwareproduct
        public async Task ACX01_WhenRegisterEmpty_ShouldSync(int dataHolderLegalEntityCount, int dataHolderBrandCount, int dataHolderSoftwareProductCount)
        {
            await Test(0, 0, 0, dataHolderLegalEntityCount, dataHolderBrandCount, dataHolderSoftwareProductCount);
        }        

        [Theory]
        [InlineData(0, 0, 0)] // no records
        [InlineData(1, 0, 0)] // has DH legalentity
        [InlineData(1, 1, 0)] // has DH legalentity & brand
        [InlineData(1, 1, 1)] // has DH legalentity & brand & softwareproduct
        public async Task ACX01_WhenDataHolderEmpty_ShouldSync(int registerLegalEntityCount, int registerBrandCount, int registerSoftwareProductCount)
        {
            await Test(registerLegalEntityCount, registerBrandCount, registerSoftwareProductCount, 0, 0, 0);
        }        

        [Theory]
        [InlineData(0, 0, 0)] // nothing
        [InlineData(1, 0, 0)] // has legalentity
        [InlineData(1, 1, 0)] // has legalentity & brand
        [InlineData(1, 1, 1)] // has legalentity & brand & softwareproduct
        public async Task ACX01_WhenRegisterAndDataHolderSame_ShouldSync(int legalEntityCount, int brandCount, int softwareProductCount)
        {
            await Test(legalEntityCount, brandCount, softwareProductCount, legalEntityCount, brandCount, softwareProductCount);
        }        

        [Theory]
        [InlineData(2, 1, 1)] // extra legalentity
        [InlineData(2, 2, 1)] // extra legalentity & brand
        [InlineData(2, 2, 2)] // extra legalentity, brand & softwareproduct
        public async Task ACX01_WhenAdditionalRegisterRecords_ShouldSync(int registerLegalEntityCount, int registerBrandCount, int registerSoftwareProductCount)
        {
            await Test(registerLegalEntityCount, registerBrandCount, registerSoftwareProductCount, 1, 1, 1);
        }        

        [Theory]
        [InlineData(2, 1, 1)] // extra legalentity
        [InlineData(2, 2, 1)] // extra legalentity & brand - FAILS - doesnt delete extra brand
        [InlineData(2, 2, 2)] // extra legalentity, brand & softwareproduct - FAILS - doesnt delete extra software product
        public async Task ACX01_WhenAdditionalDataHolderRecords_ShouldSync(int dataHolderLegalEntityCount, int dataHolderBrandCount, int dataHolderSoftwareProductCount)
        {
            await Test(1, 1, 1, dataHolderLegalEntityCount, dataHolderBrandCount, dataHolderSoftwareProductCount);
        }        

        [Fact]  // FAILS - doesn't update legalentity.status in DH
        public async Task ACX01_WhenRegisterChanged_ShouldSync()
        {
            await Test(1, 1, 1, 1, 1, 1, true, false);
        }        

        [Fact]  
        public async Task ACX01_WhenDataHolderChanged_ShouldSync()
        {
            await Test(1, 1, 1, 1, 1, 1, false, true);
        }        

        // No need to test 1001 records for DataRecipients as register does not implement paging for GetDataRecipients
        // [Theory]
        // [InlineData(1000)]
        // [InlineData(1001)] // This will fail because Azure function is not using paging and Register can only return 1000 records max
        // public async Task ACX02_WhenMoreThan1000DataRecipients_ShouldSync(int dataRecipientsInRegister)
        // {
        //     // Arrange
        //     await DatabaseSeeder.Execute(dataRecipientsInRegister);

        //     // Act
        //     await ExecuteAzureFunction();

        //     // Assert
        //     using (new AssertionScope())
        //     {
        //         await Assert_RegisterAndDataHolderIsSynced();
        //     }
        // }

        static private async Task Assert_RegisterAndDataHolderIsSynced()
        {
            static async Task Assert_TableDataIsEqual(
                SqlConnection registerConnection, string registerSql,
                SqlConnection dataHolderConnection, string dataHolderSql,
                string tableName)
            {
                var registerJson = JsonConvert.SerializeObject(await registerConnection.QueryAsync(registerSql));
                var dataHolderJson = JsonConvert.SerializeObject(await dataHolderConnection.QueryAsync(dataHolderSql));

                // Assert data is same
#if DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON
                File.WriteAllText($"c:/temp/expected_{tableName}.json", registerJson);
                File.WriteAllText($"c:/temp/actual_{tableName}.json", dataHolderJson);
#endif
                // registerJson.Should().Be(dataHolderJson);
                dataHolderJson.Should().Be(registerJson);
            }

            const string REGISTER_LEGALENTITY_SQL = @"
                select 
                    le.LegalEntityId,
                    le.LegalEntityName,
                    -- le.LegalEntityStatusId,
                    Upper(ps.ParticipationStatusCode) Status,
                    le.LogoUri
                    -- p.ParticipationTypeId,
                    -- p.IndustryId,
                    -- p.StatusId,
                    -- pt.ParticipationTypeCode
                from LegalEntity le
                left outer join LegalEntityStatus les on les.LegalEntityStatusId = le.LegalEntityStatusId
                left outer join Participation p on p.LegalEntityId = le.LegalEntityId
                left outer join ParticipationStatus ps on ps.ParticipationStatusId = p.StatusId
                left outer join ParticipationType pt on pt.ParticipationTypeId = p.ParticipationTypeId
                where pt.ParticipationTypeCode = 'DR'
                order by le.LegalEntityId";

            const string REGISTER_BRAND_SQL = @"
                select 
                    b.BrandId, 
                    b.BrandName,
                    b.LogoUri,
                    bs.BrandStatusCode Status,
                    le.LegalEntityId LegalEntityId
                from Brand b
                left outer join Participation p on p.ParticipationId = b.ParticipationId
                left outer join ParticipationType pt on pt.ParticipationTypeId = p.ParticipationTypeId                
                left outer join LegalEntity le on le.LegalEntityId = p.LegalEntityId
                left outer join BrandStatus bs on bs.BrandStatusId = b.BrandStatusId
                where pt.ParticipationTypeCode = 'DR'                
                order by BrandId";

            const string REGISTER_SOFTWAREPRODUCT_SQL = @"
                select 
                    sp.SoftwareProductId,
                    sp.SoftwareProductName,
                    sp.SoftwareProductDescription,
                    sp.LogoUri,
                    sps.SoftwareProductStatusCode Status
                from SoftwareProduct sp
                left outer join Brand b on b.BrandId = sp.BrandId
                left outer join Participation p on p.ParticipationId = b.ParticipationId
                left outer join ParticipationType pt on pt.ParticipationTypeId = p.ParticipationTypeId                
                left outer join SoftwareProductStatus sps on sps.SoftwareProductStatusId = sp.StatusId
                where pt.ParticipationTypeCode = 'DR' -- hardly necessary since only DRs have software products anyway
                order by SoftwareProductId";

            // just 'select *' incase new fields are added, at least test will fail and let someone investigate what other columns might need to be synced
            var DATAHOLDER_LEGALENTITY_SQL = "select * from LegalEntity order by LegalEntityId";
            var DATAHOLDER_BRAND_SQL = "select * from Brand order by BrandId";
            var DATAHOLDER_SOFTWAREPRODUCT_SQL = "select SoftwareProductId, SoftwareProductName, SoftwareProductDescription, LogoUri, [Status] from SoftwareProduct order by SoftwareProductId";

            using var registerConnection = new SqlConnection(BaseTest.CONNECTIONSTRING_REGISTER_RW);
            registerConnection.Open();

            using var dataHolderConnection = new SqlConnection(BaseTest.CONNECTIONSTRING_MDH_RW);
            dataHolderConnection.Open();

            // Assert
            await Assert_TableDataIsEqual(registerConnection, REGISTER_LEGALENTITY_SQL, dataHolderConnection, DATAHOLDER_LEGALENTITY_SQL, "LegalEntity");
            await Assert_TableDataIsEqual(registerConnection, REGISTER_BRAND_SQL, dataHolderConnection, DATAHOLDER_BRAND_SQL, "Brand");
            await Assert_TableDataIsEqual(registerConnection, REGISTER_SOFTWAREPRODUCT_SQL, dataHolderConnection, DATAHOLDER_SOFTWAREPRODUCT_SQL, "SoftwareProduct");
        }
    }
}
