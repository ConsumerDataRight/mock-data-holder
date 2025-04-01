using CDR.DataHolder.Shared.API.Infrastructure.Extensions;
using System;
using Xunit;

namespace CDR.DataHolder.Shared.API.Infrastructure.UnitTests.Extensions
{
    [Trait("Category", "UnitTests")]
    public class DateTimeExtensionsTests
    {
        [Fact]
        public void HasExpired_WithinRange_ReturnsFalse()
        {
            // Arrange.
            DateTime creationTime = new DateTime(2021, 01, 01, 0, 0, 0, DateTimeKind.Utc);
            DateTime now = new DateTime(2021, 01, 02, 0, 0, 0, DateTimeKind.Utc);
            int seconds = 60 * 60 * 25;
            var expected = false;

            // Act.
            var actual = creationTime.HasExpired(seconds, now);

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void HasExpired_AtRange_ReturnsFalse()
        {
            // Arrange.
            DateTime creationTime = new DateTime(2021, 01, 01, 0, 0, 0, DateTimeKind.Utc);
            DateTime now = new DateTime(2021, 01, 02, 0, 0, 0, DateTimeKind.Utc);
            int seconds = 60 * 60 * 24;
            var expected = false;

            // Act.
            var actual = creationTime.HasExpired(seconds, now);

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void HasExpired_OutsideRange_ReturnsTrue()
        {
            // Arrange.
            DateTime creationTime = new DateTime(2021, 01, 01, 0, 0, 0, DateTimeKind.Utc);
            DateTime now = new DateTime(2021, 01, 02, 0, 0, 0, DateTimeKind.Utc);
            int seconds = 60 * 60 * 23;
            var expected = true;

            // Act.
            var actual = creationTime.HasExpired(seconds, now);

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ToEpoch_At01012000_Returns946684800()
        {
            // Arrange.
            DateTime creationTime = new DateTime(2000, 01, 01, 0, 0, 0, DateTimeKind.Utc);
            var expected = 946684800;

            // Act.
            var actual = creationTime.ToEpoch();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ToEpoch_At01011970_Returns0()
        {
            // Arrange.
            DateTime creationTime = DateTime.UnixEpoch;
            var expected = 0;

            // Act.
            var actual = creationTime.ToEpoch();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ToEpoch_At31121969_ReturnsNegative()
        {
            // Arrange.
            DateTime creationTime = new DateTime(1969, 12, 31, 0, 0, 0, DateTimeKind.Utc);
            var expected = -86400;

            // Act.
            var actual = creationTime.ToEpoch();

            // Assert.
            Assert.Equal(expected, actual);
        }
    }
}
