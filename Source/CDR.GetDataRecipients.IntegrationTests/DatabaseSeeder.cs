using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;

#nullable enable

namespace CDR.GetDataRecipients.IntegrationTests
{
    static public class DatabaseSeeder
    {
        private enum IndustryType { Banking, Energy, Telecommunications }
        private enum ParticipantType { DH, DR }

        private static int nextRegisterLegalEntityId = 0;
        private static int nextRegisterParticipationId = 0;
        private static int nextRegisterBrandId = 0;
        private static int nextRegisterSoftwareProductId = 0;

        private static int nextDataHolderLegalEntityId = 0;
        private static int nextDataHolderBrandId = 0;
        private static int nextDataHolderSoftwareProductId = 0;

        static public async Task Execute(
            int registerLegalEntityCount, int registerBrandCount, int registerSoftwareProductCount,
            int dataHolderLegalEntityCount, int dataHolderBrandCount, int dataHolderSoftwareProductCount,
            bool registerModified,  // simulate change to register records
            bool dataHolderModified // simulate change to dataholder records
        )
        {
            // Database is purged so, reset next ids so that ids are consistent across tests
            nextRegisterLegalEntityId = 0;
            nextRegisterParticipationId = 0;
            nextRegisterBrandId = 0;
            nextRegisterSoftwareProductId = 0;
            nextDataHolderLegalEntityId = 0;
            nextDataHolderBrandId = 0;
            nextDataHolderSoftwareProductId = 0;

            // Seed Register
            using var registerConnection = new SqlConnection(BaseTest.CONNECTIONSTRING_REGISTER_RW);
            registerConnection.Open();
            await RegisterPurge(registerConnection);
            await RegisterInsert(registerConnection, registerLegalEntityCount, registerBrandCount, registerSoftwareProductCount, registerModified);

            // Seed MockDataHolder
            using var dataHolderConnection = new SqlConnection(BaseTest.CONNECTIONSTRING_MDH_RW);
            dataHolderConnection.Open();
            await DataHolderPurge(dataHolderConnection);
            await DataHolderInsert(dataHolderConnection, dataHolderLegalEntityCount, dataHolderBrandCount, dataHolderSoftwareProductCount, dataHolderModified);
        }

        // Purge register database but leave standing data intact
        private static async Task RegisterPurge(SqlConnection connection)
        {
            await connection.ExecuteAsync("delete AuthDetail");
            await connection.ExecuteAsync("delete Brand");
            await connection.ExecuteAsync("delete Endpoint");
            await connection.ExecuteAsync("delete LegalEntity");
            await connection.ExecuteAsync("delete Participation");
            await connection.ExecuteAsync("delete SoftwareProduct");
            await connection.ExecuteAsync("delete SoftwareProductCertificate");
        }

        // Purge data holder database but leave standing data intact
        private static async Task DataHolderPurge(SqlConnection connection)
        {
            await connection.ExecuteAsync("delete [Transaction]");
            await connection.ExecuteAsync("delete Account");
            await connection.ExecuteAsync("delete SoftwareProduct");
            await connection.ExecuteAsync("delete Brand");
            await connection.ExecuteAsync("delete Customer");
            await connection.ExecuteAsync("delete Organisation");
            await connection.ExecuteAsync("delete Person");
            await connection.ExecuteAsync("delete LegalEntity");
        }

        private static async Task RegisterInsert(SqlConnection connection, int legalEntityCount, int brandCount, int softwareProductCount, bool modified)
        {
            static async Task<Guid> Register_InsertLegalEntity(SqlConnection connection, IndustryType industryType, bool modified)
            {
                var legalEntityId = new Guid($"00000000-0000-0000-0000-{++nextRegisterLegalEntityId:d012}");

                string legalEntityName = $"LegalEntity_{legalEntityId}".ToString().Replace('-', '_');

                await connection.ExecuteScalarAsync<Guid>(@"
                        insert into LegalEntity(LegalEntityId, LegalEntityName, LogoUri, AnzsicDivision, OrganisationTypeId, LegalEntityStatusId, AccreditationLevelId, AccreditationNumber) 
                        values(@LegalEntityId, @LegalEntityName, @LogoUri, @AnzsicDivision, @OrganisationTypeId, @LegalEntityStatusId, @AccreditationLevelId, @AccreditationNumber)",
                    new
                    {
                        LegalEntityId = legalEntityId,
                        LegalEntityName = modified ? "foo" : legalEntityName,
                        LogoUri = modified ? "foo" : $"https://www.{legalEntityName}.com/logo.jpg",
                        AnzsicDivision = industryType switch
                        {
                            IndustryType.Banking => "6221",
                            IndustryType.Energy => "2640",
                            IndustryType.Telecommunications => "5801",
                            _ => throw new NotSupportedException()
                        },
                        OrganisationTypeId = "2", // company 
                        LegalEntityStatusId = "1", // make it active by default
                        AccreditationLevelId = "1", // unrestricted
                        AccreditationNumber = $"ABC{nextRegisterLegalEntityId:d012}"
                    });

                return legalEntityId;
            }

            static async Task<Guid> Register_InsertParticipation(SqlConnection connection, Guid legalEntityId, ParticipantType participantType, IndustryType industryType, bool modified)
            {
                var participationId = new Guid($"00000000-0000-0000-0000-{++nextRegisterParticipationId:d012}");

				await connection.ExecuteScalarAsync<Guid>(@"
                    insert into Participation(ParticipationId, LegalEntityId, ParticipationTypeId, IndustryId, StatusId) 
                    values(@ParticipationId, @LegalEntityId,
                        (select ParticipationTypeId from ParticipationType where ParticipationTypeCode = @ParticipantTypeCode),
                        (select IndustryTypeId from IndustryType where IndustryTypeCode = @IndustryTypeCode),
                        (select ParticipationStatusId from ParticipationStatus where Upper(ParticipationStatusCode) = @ParticipationStatusCode))",
                    new
                    {
                        ParticipationId = participationId,
                        LegalEntityId = legalEntityId,
                        ParticipantTypeCode = participantType.ToString(),
						ParticipationStatusCode = modified ? "INACTIVE" : "ACTIVE",
						IndustryTypeCode = industryType.ToString()
                    });

                return participationId;
            }

            static async Task<Guid> Register_InsertBrand(SqlConnection connection, Guid participationId, bool modified)
            {
                var brandId = new Guid($"00000000-0000-0000-0000-{++nextRegisterBrandId:d012}");

                string brandName = $"Brand_{brandId}".ToString().Replace('-', '_');

                await connection.ExecuteScalarAsync<Guid>(@"
                    insert into Brand(BrandId, BrandName, LogoUri, BrandStatusId, ParticipationId, LastUpdated) 
                    values(@BrandId, @BrandName, @LogoUri,
                        --(select BrandStatusId from BrandStatus where Upper(BrandStatusCode) = 'ACTIVE'),
                        @StatusId,
                        @ParticipationId,
                        @LastUpdated)",
                    new
                    {
                        BrandId = brandId,
                        BrandName = modified ? "foo" : brandName,
                        LogoUri = modified ? "foo" : $"https://www.{brandName}.com/logo.jpg",
                        StatusId = modified ? "2" : "1", // 1=active, 2=inactive
                        ParticipationId = participationId,
                        LastUpdated = DateTime.UtcNow
                    });

                return brandId;
            }

            static async Task<Guid> Register_InsertSoftwareProduct(SqlConnection connection, Guid brandId, bool modified)
            {
                var softwareProductId = new Guid($"00000000-0000-0000-0000-{++nextRegisterSoftwareProductId:d012}");

                string softwareProductName = $"SoftwareProduct_{softwareProductId}".ToString().Replace('-', '_');

                await connection.ExecuteScalarAsync<Guid>(@"
                        insert into SoftwareProduct(
                            SoftwareProductId, 
                            SoftwareProductName, 
                            SoftwareProductDescription, 
                            LogoUri,
                            SectorIdentifierUri, 
                            ClientUri, 
                            RecipientBaseUri,
                            RevocationUri, 
                            RedirectUris, 
                            JwksUri, 
                            Scope, 
                            StatusId, 
                            BrandId) 
                        values(
                            @SoftwareProductId, 
                            @SoftwareProductName, 
                            @SoftwareProductDescription, 
                            @LogoUri,
                            @SectorIdentifierUri,
                            @ClientUri, 
                            @RecipientBaseUri,
                            @RevocationUri, 
                            @RedirectUris, 
                            @JwksUri, 
                            @Scope, 
                            --(select SoftwareProductStatusId from SoftwareProductStatus where Upper(SoftwareProductStatusCode) = 'ACTIVE'), 
                            @StatusId,
                            @BrandId)",
                    new
                    {
                        SoftwareProductId = softwareProductId,
                        SoftwareProductName =  modified ? "foo" : $"{softwareProductName}",
                        SoftwareProductDescription = modified ? "foo" : $"{softwareProductName} description",
                        LogoUri = modified ? "foo" : $"https://www.{softwareProductName}.com/logo.jpg",
                        SectorIdentifierUri = $"https://www.{softwareProductName}.com/sectoridentifier",
                        ClientUri = $"https://www.{softwareProductName}.com/client",
                        RecipientBaseUri = $"https://www.{softwareProductName}.com/recipientbase",
                        RevocationUri = $"https://www.{softwareProductName}.com/revocation",
                        RedirectUris = $"https://www.{softwareProductName}.com/redirect1,https://www.{softwareProductName}.com/redirect2",
                        JwksUri = $"https://www.{softwareProductName}.com/jwks",
                        Scope = "scope",
                        StatusId = modified ? "2" : "1", // 1=active, 2=inactive
                        BrandId = brandId,
                    });

                return softwareProductId;
            }

            // Insert legal entities
            for (int ilegalEntity = 0; ilegalEntity < legalEntityCount; ilegalEntity++)
            {
                var register_LegalEntityId = await Register_InsertLegalEntity(connection, IndustryType.Banking, modified);
                var register_ParticipationId = await Register_InsertParticipation(connection, register_LegalEntityId, ParticipantType.DR, IndustryType.Banking, modified);

                // Insert brands
                for (int ibrandCount = 0; ibrandCount < brandCount; ibrandCount++)
                {
                    var register_BrandId = await Register_InsertBrand(connection, register_ParticipationId, modified);

                    // Insert software products
                    for (int isoftwareProductCount = 0; isoftwareProductCount < softwareProductCount; isoftwareProductCount++)
                    {
                        var register_SoftwareProductId = await Register_InsertSoftwareProduct(connection, register_BrandId, modified);
                    }
                }
            }
        }

        private static async Task DataHolderInsert(SqlConnection connection, int legalEntityCount, int brandCount, int softwareProductCount, bool modified)
        {
            static async Task<Guid> DataHolder_InsertLegalEntity(SqlConnection connection, bool modified)
            {
                var legalEntityId = new Guid($"00000000-0000-0000-0000-{++nextDataHolderLegalEntityId:d012}");

                string legalEntityName = $"LegalEntity_{legalEntityId}".ToString().Replace('-', '_');

                await connection.ExecuteScalarAsync<Guid>(@"
                        insert into LegalEntity(LegalEntityId, LegalEntityName, LogoUri, Status) 
                        values(@LegalEntityId, @LegalEntityName, @LogoUri, @Status)",
                    new
                    {
                        LegalEntityId = legalEntityId,
                        LegalEntityName = modified ? "foo" :legalEntityName,
                        LogoUri = modified ? "foo" : $"https://www.{legalEntityName}.com/logo.jpg",
                        Status = modified ? "REMOVED" : "ACTIVE"
                    });

                return legalEntityId;
            }

            static async Task<Guid> DataHolder_InsertBrand(SqlConnection connection, Guid legalEntityId, bool modified)
            {
                var brandId = new Guid($"00000000-0000-0000-0000-{++nextDataHolderBrandId:d012}");

                string brandName = $"Brand_{brandId}".ToString().Replace('-', '_');

                await connection.ExecuteScalarAsync<Guid>(@"
                        insert into Brand(BrandId, BrandName, LogoUri, Status, LegalEntityId) 
                        values(@BrandId, @BrandName, @LogoUri, @Status, @LegalEntityId)",
                    new
                    {
                        BrandId = brandId,
                        BrandName = modified ? "foo" : brandName,
                        LogoUri = modified ? "foo" : $"https://www.{brandName}.com/logo.jpg",
                        Status = modified ? "INACTIVE" : "ACTIVE",
                        legalEntityId = legalEntityId
                    });

                return brandId;
            }

            static async Task<Guid> DataHolder_InsertSoftwareProduct(SqlConnection connection, Guid brandId, bool modified)
            {
                var softwareProductId = new Guid($"00000000-0000-0000-0000-{++nextDataHolderSoftwareProductId:d012}");

                string SoftwareProductName = $"SoftwareProduct_{softwareProductId}".ToString().Replace('-', '_');

                await connection.ExecuteScalarAsync<Guid>(@"
                        insert into SoftwareProduct(SoftwareProductId, SoftwareProductName, SoftwareProductDescription, LogoUri, Status, BrandId) 
                        values(@SoftwareProductId, @SoftwareProductName, @SoftwareProductDescription, @LogoUri, @Status, @BrandId)",
                    new
                    {
                        SoftwareProductId = softwareProductId,
                        SoftwareProductName = modified ? "foo" : SoftwareProductName,
                        SoftwareProductDescription = modified ? "foo" : $"{SoftwareProductName} description",
                        LogoUri = modified ? "foo" : $"https://www.{SoftwareProductName}.com/logo.jpg",
                        Status = modified ? "INACTIVE" : "ACTIVE",
                        BrandId = brandId
                    });

                return softwareProductId;
            }

            for (int i = 1; i <= legalEntityCount; i++)
            {
                var dataholder_LegalEntityId = await DataHolder_InsertLegalEntity(connection, modified);

                for (int i2 = 1; i2 <= brandCount; i2++)
                {
                    var dataholder_BrandId = await DataHolder_InsertBrand(connection, dataholder_LegalEntityId, modified);

                    for (int i3 = 1; i3 <= brandCount; i3++)
                    {
                        var dataholder_SoftwareProductId = await DataHolder_InsertSoftwareProduct(connection, dataholder_BrandId, modified);
                    }
                }
            }
        }
    }
}
