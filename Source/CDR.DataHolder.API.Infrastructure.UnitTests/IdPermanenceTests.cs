using System;
using CDR.DataHolder.API.Infrastructure.IdPermanence;
using Xunit;

namespace CDR.DataHolder.API.Infrastructure.UnitTests
{
    public class IdPermanenceTests
    {
        [Fact]
        public void IdPermanenceHelper_EncryptId_InternalIdIsNull_ThrowsArgumentException()
        {
            // Arrange.
            var softwareProductId = Guid.NewGuid().ToString();
            var customerId = Guid.NewGuid().ToString();
            string internalId = null;
            var privateKey = Guid.NewGuid().ToString();
            var idParameters = new IdPermanenceParameters
            {
                SoftwareProductId = softwareProductId,
                CustomerId = customerId,
            };

            // Act and Assert.
            Assert.Throws<ArgumentException>(() => IdPermanenceHelper.EncryptId(internalId, idParameters, privateKey));
        }

        [Fact]
        public void IdPermanenceHelper_EncryptId_InternalIdIsEmpty_ThrowsArgumentException()
        {
            // Arrange.
            var softwareProductId = Guid.NewGuid().ToString();
            var customerId = Guid.NewGuid().ToString();
            string internalId = "";
            var privateKey = Guid.NewGuid().ToString();
            var idParameters = new IdPermanenceParameters
            {
                SoftwareProductId = softwareProductId,
                CustomerId = customerId,
            };

            // Act and Assert.
            Assert.Throws<ArgumentException>(() => IdPermanenceHelper.EncryptId(internalId, idParameters, privateKey));
        }

        [Fact]
        public void IdPermanenceHelper_EncryptId_PrivateKeyIsNull_ThrowsArgumentException()
        {
            // Arrange.
            var softwareProductId = Guid.NewGuid().ToString();
            var customerId = Guid.NewGuid().ToString();
            string internalId = "123";
            string privateKey = null;
            var idParameters = new IdPermanenceParameters
            {
                SoftwareProductId = softwareProductId,
                CustomerId = customerId,
            };

            // Act and Assert.
            Assert.Throws<ArgumentException>(() => IdPermanenceHelper.EncryptId(internalId, idParameters, privateKey));
        }

        [Fact]
        public void IdPermanenceHelper_EncryptId_PrivateKeyIsEmpty_ThrowsArgumentException()
        {
            // Arrange.
            var softwareProductId = Guid.NewGuid().ToString();
            var customerId = Guid.NewGuid().ToString();
            string internalId = "123";
            string privateKey = "";
            var idParameters = new IdPermanenceParameters
            {
                SoftwareProductId = softwareProductId,
                CustomerId = customerId,
            };

            // Act and Assert.
            Assert.Throws<ArgumentException>(() => IdPermanenceHelper.EncryptId(internalId, idParameters, privateKey));
        }

        [Fact]
        public void IdPermanenceHelper_EncryptId_IdParametersIsNull_ThrowsArgumentNullException()
        {
            // Arrange.
            var softwareProductId = Guid.NewGuid().ToString();
            var customerId = Guid.NewGuid().ToString();
            string internalId = "123";
            string privateKey = Guid.NewGuid().ToString();
            IdPermanenceParameters idParameters = null;

            // Act and Assert.
            Assert.Throws<ArgumentNullException>(() => IdPermanenceHelper.EncryptId(internalId, idParameters, privateKey));
        }

        [Fact]
        public void IdPermanenceHelper_EncryptId_IdParameters_SoftwareProductIdIsNull_ThrowsArgumentException()
        {
            // Arrange.
            var softwareProductId = Guid.NewGuid().ToString();
            var customerId = Guid.NewGuid().ToString();
            string internalId = "123";
            string privateKey = Guid.NewGuid().ToString();
            var idParameters = new IdPermanenceParameters
            {
                CustomerId = customerId,
            };

            // Act and Assert.
            Assert.Throws<ArgumentException>(() => IdPermanenceHelper.EncryptId(internalId, idParameters, privateKey));
        }

        [Fact]
        public void IdPermanenceHelper_EncryptId_IdParameters_SoftwareProductIdIsEmpty_ThrowsArgumentException()
        {
            // Arrange.
            var softwareProductId = Guid.NewGuid().ToString();
            var customerId = Guid.NewGuid().ToString();
            string internalId = "123";
            string privateKey = Guid.NewGuid().ToString();
            var idParameters = new IdPermanenceParameters
            {
                SoftwareProductId = "",
                CustomerId = customerId,
            };

            // Act and Assert.
            Assert.Throws<ArgumentException>(() => IdPermanenceHelper.EncryptId(internalId, idParameters, privateKey));
        }

        [Fact]
        public void IdPermanenceHelper_EncryptId_IdParameters_CustomerIdIsNull_ThrowsArgumentException()
        {
            // Arrange.
            var softwareProductId = Guid.NewGuid().ToString();
            var customerId = Guid.NewGuid().ToString();
            string internalId = "123";
            string privateKey = Guid.NewGuid().ToString();
            var idParameters = new IdPermanenceParameters
            {
                SoftwareProductId = Guid.NewGuid().ToString(),
                CustomerId = null,
            };

            // Act and Assert.
            Assert.Throws<ArgumentException>(() => IdPermanenceHelper.EncryptId(internalId, idParameters, privateKey));
        }

        [Fact]
        public void IdPermanenceHelper_EncryptId_IdParameters_CustomerIdIsEmpty_ThrowsArgumentException()
        {
            // Arrange.
            var softwareProductId = Guid.NewGuid().ToString();
            var customerId = Guid.NewGuid().ToString();
            string internalId = "123";
            string privateKey = Guid.NewGuid().ToString();
            var idParameters = new IdPermanenceParameters
            {
                SoftwareProductId = Guid.NewGuid().ToString(),
                CustomerId = "",
            };

            // Act and Assert.
            Assert.Throws<ArgumentException>(() => IdPermanenceHelper.EncryptId(internalId, idParameters, privateKey));
        }

        [Fact]
        public void IdPermanenceHelper_EncryptSub_CustomerIdIsNull_ThrowsArgumentException()
        {
            // Arrange.
            var softwareProductId = Guid.NewGuid().ToString();
            string customerId =null;
            var privateKey = Guid.NewGuid().ToString();
            var subParameters = new SubPermanenceParameters
            {
                SoftwareProductId = softwareProductId,
            };

            // Act and Assert.
            Assert.Throws<ArgumentException>(() => IdPermanenceHelper.EncryptSub(customerId, subParameters, privateKey));
        }

        [Fact]
        public void IdPermanenceHelper_EncryptSub_CustomerIdIsEmpty_ThrowsArgumentException()
        {
            // Arrange.
            var softwareProductId = Guid.NewGuid().ToString();
            string customerId = "";
            var privateKey = Guid.NewGuid().ToString();
            var subParameters = new SubPermanenceParameters
            {
                SoftwareProductId = softwareProductId,
            };

            // Act and Assert.
            Assert.Throws<ArgumentException>(() => IdPermanenceHelper.EncryptSub(customerId, subParameters, privateKey));
        }

        [Fact]
        public void IdPermanenceHelper_EncryptSub_PrivateKeyIsNull_ThrowsArgumentException()
        {
            // Arrange.
            var softwareProductId = Guid.NewGuid().ToString();
            var customerId = Guid.NewGuid().ToString();
            string privateKey = null;
            var subParameters = new SubPermanenceParameters
            {
                SoftwareProductId = softwareProductId,
            };

            // Act and Assert.
            Assert.Throws<ArgumentException>(() => IdPermanenceHelper.EncryptSub(customerId, subParameters, privateKey));
        }

        [Fact]
        public void IdPermanenceHelper_EncryptSub_PrivateKeyIsEmpty_ThrowsArgumentException()
        {
            // Arrange.
            var softwareProductId = Guid.NewGuid().ToString();
            var customerId = Guid.NewGuid().ToString();
            string privateKey = "";
            var subParameters = new SubPermanenceParameters
            {
                SoftwareProductId = softwareProductId,
            };

            // Act and Assert.
            Assert.Throws<ArgumentException>(() => IdPermanenceHelper.EncryptSub(customerId, subParameters, privateKey));
        }

        [Fact]
        public void IdPermanenceHelper_EncryptSub_IdParametersIsNull_ThrowsArgumentNullException()
        {
            // Arrange.
            var softwareProductId = Guid.NewGuid().ToString();
            var customerId = Guid.NewGuid().ToString();
            string privateKey = Guid.NewGuid().ToString();
            SubPermanenceParameters subParameters = null;

            // Act and Assert.
            Assert.Throws<ArgumentNullException>(() => IdPermanenceHelper.EncryptSub(customerId, subParameters, privateKey));
        }

        [Fact]
        public void IdPermanenceHelper_EncryptSub_IdParameters_SoftwareProductIdIsNull_ThrowsArgumentException()
        {
            // Arrange.
            var softwareProductId = Guid.NewGuid().ToString();
            var customerId = Guid.NewGuid().ToString();
            string privateKey = Guid.NewGuid().ToString();
            var subParameters = new SubPermanenceParameters
            {
            };

            // Act and Assert.
            Assert.Throws<ArgumentException>(() => IdPermanenceHelper.EncryptSub(customerId, subParameters, privateKey));
        }

        [Fact]
        public void IdPermanenceHelper_EncryptSub_IdParameters_SoftwareProductIdIsEmpty_ThrowsArgumentException()
        {
            // Arrange.
            var softwareProductId = Guid.NewGuid().ToString();
            var customerId = Guid.NewGuid().ToString();
            string privateKey = Guid.NewGuid().ToString();
            var idParameters = new IdPermanenceParameters
            {
                SoftwareProductId = "",
            };

            // Act and Assert.
            Assert.Throws<ArgumentException>(() => IdPermanenceHelper.EncryptId(customerId, idParameters, privateKey));
        }

        [Fact]
        public void Test_Id_Permanence_Algorithm_Uniqueness_Success()
        {
            // Arrange.
            var softwareProductId = Guid.NewGuid().ToString();
            var customerId = Guid.NewGuid().ToString();
            var transactionId1 = "TRX111";
            var transactionId2 = "TRX112";
            var privateKey = Guid.NewGuid().ToString();
            var idParameters = new IdPermanenceParameters
            {
                SoftwareProductId = softwareProductId,
                CustomerId = customerId,
            };

            // Act.
            string idPermanence1 = IdPermanenceHelper.EncryptId(transactionId1, idParameters, privateKey);
            string idPermanence2 = IdPermanenceHelper.EncryptId(transactionId2, idParameters, privateKey);

            // Assert.
            Assert.True(idPermanence1 != idPermanence2);
        }

        [Fact]
        public void Test_Id_Permanence_Algorithm_Immutability_Success()
        {
            // Arrange.
            var softwareProductId = Guid.NewGuid().ToString();
            var customerId = Guid.NewGuid().ToString();
            var accountId = "1122334455";
            var privateKey = Guid.NewGuid().ToString();
            var idParameters = new IdPermanenceParameters
            {
                SoftwareProductId = softwareProductId,
                CustomerId = customerId,
            };

            // Act.
            string idPermanence1 = IdPermanenceHelper.EncryptId(accountId, idParameters, privateKey);
            string idPermanence2 = IdPermanenceHelper.EncryptId(accountId, idParameters, privateKey);

            // Assert.
            Assert.Equal(idPermanence1, idPermanence2);
        }

        [Fact]
        public void Test_Id_Permanence_Algorithm_Decrypt_Success()
        {
            // Arrange.
            var softwareProductId = Guid.NewGuid().ToString();
            var customerId = Guid.NewGuid().ToString();
            var accountId = "1122334455";
            var privateKey = Guid.NewGuid().ToString();            
            var idParameters = new IdPermanenceParameters
            {
                SoftwareProductId = softwareProductId,
                CustomerId = customerId,
            };

            // Act.
            var encrypted = IdPermanenceHelper.EncryptId(accountId, idParameters, privateKey);
            var decrypted = IdPermanenceHelper.DecryptId(encrypted, idParameters, privateKey);

            // Assert.
            Assert.Equal(accountId, decrypted);
        }

        [Fact]
        public void IdPermanenceManager_EncryptDecrypt_Success()
        {
            // Arrange.
            var softwareProductId = "C6327F87-687A-4369-99A4-EAACD3BB8210";
            var customerId = "BFB689FB-7745-45B9-BBAA-B21E00072447";
            var accountId = "123456789";
            var privateKey = "90733A75F19347118B3BE0030AB590A8";
            var idParameters = new IdPermanenceParameters
            {
                SoftwareProductId = softwareProductId,
                CustomerId = customerId,
            };

            // Act.
            var encrypted = IdPermanenceHelper.EncryptId(accountId, idParameters, privateKey);
            var decrypted = IdPermanenceHelper.DecryptId(encrypted, idParameters, privateKey);

            // Assert.
            Assert.Equal(accountId, decrypted);
        }

        [Fact]
        public void Test_Id_Permanence_Algorithm_Decrypt_InvalidPrivateKey_Success()
        {
            // Arrange.
            var softwareProductId = Guid.NewGuid().ToString();
            var customerId = Guid.NewGuid().ToString();
            var accountId = "1122334455";
            var privateKey = Guid.NewGuid().ToString();
            var idParameters = new IdPermanenceParameters
            {
                SoftwareProductId = softwareProductId,
                CustomerId = customerId,
            };

            // Act.
            var idPermanence = IdPermanenceHelper.EncryptId(accountId, idParameters, privateKey);
            var invalidPrivateKey = privateKey.Substring(1, privateKey.Length - 1);

            try
            {
                var decrypted = IdPermanenceHelper.DecryptId(idPermanence, idParameters, invalidPrivateKey);
            }
            catch(Exception ex)
            {
                // Assert.
                Assert.NotNull(ex);
                Assert.Equal("Unable to decrypt.", ex.Message);
            }
        }

        [Fact]
        public void IdPermanenceManager_SubClaim_EncryptDecrypt_Success()
        {
            // Arrange.
            var softwareProductId = "C6327F87-687A-4369-99A4-EAACD3BB8210";
            var sectorIdentifierUri = "https://datarecipient/uris";
            var customerId = Guid.NewGuid().ToString();
            var privateKey = "90733A75F19347118B3BE0030AB590A8";
            var subParameters = new SubPermanenceParameters
            {
                SoftwareProductId = softwareProductId,
                SectorIdentifierUri = sectorIdentifierUri,
            };

            // Act.
            var encrypted = IdPermanenceHelper.EncryptSub(customerId, subParameters, privateKey);
            var decrypted = IdPermanenceHelper.DecryptSub(encrypted, subParameters, privateKey);

            // Assert.
            Assert.Equal(customerId, decrypted);
        }

        [Fact]
        public void Test_Id_Permanence_Algorithm_SubClaim_Uniqueness_Success()
        {
            // Arrange.
            var softwareProductId1 = Guid.NewGuid().ToString();
            var softwareProductId2 = Guid.NewGuid().ToString();
            var sectorIdentifierUri = "https://datarecipient/uris";
            var customerId = Guid.NewGuid().ToString();
            var privateKey = Guid.NewGuid().ToString();
            var subParameters1 = new SubPermanenceParameters
            {
                SoftwareProductId = softwareProductId1,
                SectorIdentifierUri = sectorIdentifierUri,
            };
            var subParameters2 = new SubPermanenceParameters
            {
                SoftwareProductId = softwareProductId2,
                SectorIdentifierUri = sectorIdentifierUri,
            };

            // Act.
            string idPermanence1 = IdPermanenceHelper.EncryptSub(customerId, subParameters1, privateKey);
            string idPermanence2 = IdPermanenceHelper.EncryptSub(customerId, subParameters2, privateKey);

            // Assert.
            Assert.True(idPermanence1 != idPermanence2);
        }

        [Fact]
        public void Test_Id_Permanence_Algorithm_SubClaim_Immutability_Success()
        {
            // Arrange.
            var softwareProductId = Guid.NewGuid().ToString();
            var sectorIdentifierUri = "https://datarecipient/uris";
            var customerId = Guid.NewGuid().ToString();
            var privateKey = Guid.NewGuid().ToString();
            var subParameters = new SubPermanenceParameters
            {
                SoftwareProductId = softwareProductId,
                SectorIdentifierUri = sectorIdentifierUri,
            };

            // Act.
            string idPermanence1 = IdPermanenceHelper.EncryptSub(customerId, subParameters, privateKey);
            string idPermanence2 = IdPermanenceHelper.EncryptSub(customerId, subParameters, privateKey);

            // Assert.
            Assert.Equal(idPermanence1, idPermanence2);
        }
    }
}
