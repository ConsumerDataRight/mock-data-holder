using System;
using Xunit;
using CDR.DataHolder.API.Infrastructure.Extensions;
using System.Collections.Generic;

namespace CDR.DataHolder.API.Infrastructure.UnitTests.Extensions
{
    public class StringExtensionsTests
    {

        [Fact]
        public void IsMissing_StringIsNull_ReturnsTrue()
        {
            // Arrange.
            string value = null;
            var expected = true;

            // Act.
            var actual = value.IsMissing();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IsMissing_StringIsEmpty_ReturnsTrue()
        {
            // Arrange.
            string value = "";
            var expected = true;

            // Act.
            var actual = value.IsMissing();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IsMissing_StringIsWhitespace_ReturnsTrue()
        {
            // Arrange.
            string value = "   ";
            var expected = true;

            // Act.
            var actual = value.IsMissing();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IsMissing_StringNotEmptyOrWhitespace_ReturnsFalse()
        {
            // Arrange.
            string value = "something";
            var expected = false;

            // Act.
            var actual = value.IsMissing();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IsPresent_StringIsNull_ReturnsFalse()
        {
            // Arrange.
            string value = null;
            var expected = false;

            // Act.
            var actual = value.IsPresent();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IsPresent_StringIsEmpty_ReturnsFalse()
        {
            // Arrange.
            string value = "";
            var expected = false;

            // Act.
            var actual = value.IsPresent();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IsPresent_StringIsWhitespace_ReturnsFalse()
        {
            // Arrange.
            string value = "   ";
            var expected = false;

            // Act.
            var actual = value.IsPresent();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IsPresent_StringNotEmptyOrWhitespace_ReturnsTrue()
        {
            // Arrange.
            string value = "something";
            var expected = true;

            // Act.
            var actual = value.IsPresent();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FromSpaceSeparatedString_NullString_ReturnsEmptyList()
        {
            // Arrange.
            string value = null;
            var expected = new List<string>(Array.Empty<string>());

            // Act.
            var actual = value.FromSpaceSeparatedString();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FromSpaceSeparatedString_EmptyString_ReturnsEmptyList()
        {
            // Arrange.
            string value = "";
            var expected = new List<string>(Array.Empty<string>());

            // Act.
            var actual = value.FromSpaceSeparatedString();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FromSpaceSeparatedString_WhitespaceString_ReturnsEmptyList()
        {
            // Arrange.
            string value = "   ";
            var expected = new List<string>(Array.Empty<string>());

            // Act.
            var actual = value.FromSpaceSeparatedString();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FromSpaceSeparatedString_SingleItemString_ReturnsSingleItemList()
        {
            // Arrange.
            string value = "a";
            var expected = new List<string>(new string[] { "a" });

            // Act.
            var actual = value.FromSpaceSeparatedString();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FromSpaceSeparatedString_MultipleItemString_ReturnsMultipleItemList()
        {
            // Arrange.
            string value = "a b c";
            var expected = new List<string>(new string[] { "a", "b", "c" });

            // Act.
            var actual = value.FromSpaceSeparatedString();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ToSpaceSeparatedString_EmptyList_ReturnsEmptyString()
        {
            // Arrange.
            List<string> values = new List<string>(new string[] { });
            var expected = "";

            // Act.
            var actual = values.ToSpaceSeparatedString();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ToSpaceSeparatedString_NullList_ReturnsEmptyString()
        {
            // Arrange.
            List<string> values = null;
            var expected = "";

            // Act.
            var actual = values.ToSpaceSeparatedString();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ToSpaceSeparatedString_OneItem_ReturnsItemAsString()
        {
            // Arrange.
            List<string> values = new List<string>(new string[] { "test1" }); ;
            var expected = "test1";

            // Act.
            var actual = values.ToSpaceSeparatedString();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ToSpaceSeparatedString_MultipleItems_ReturnsItemsAsString()
        {
            // Arrange.
            List<string> values = new List<string>(new string[] { "test1", "test2", "test3" }); ;
            var expected = "test1 test2 test3";

            // Act.
            var actual = values.ToSpaceSeparatedString();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IsMissingOrTooLong_StringIsNull_ReturnsTrue()
        {
            // Arrange.
            string value = null;
            var expected = true;

            // Act.
            var actual = value.IsMissingOrTooLong(10);

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IsMissingOrTooLong_StringIsEmpty_ReturnsTrue()
        {
            // Arrange.
            string value = "";
            var expected = true;

            // Act.
            var actual = value.IsMissingOrTooLong(10);

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IsMissingOrTooLong_StringIsWhitespace_ReturnsTrue()
        {
            // Arrange.
            string value = "   ";
            var expected = true;

            // Act.
            var actual = value.IsMissingOrTooLong(10);

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IsMissingOrTooLong_StringNotEmptyOrWhitespace_ReturnsFalse()
        {
            // Arrange.
            string value = "something";
            var expected = false;

            // Act.
            var actual = value.IsMissingOrTooLong(10);

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IsMissingOrTooLong_StringTooLong_ReturnsTrue()
        {
            // Arrange.
            string value = "something--";
            var expected = true;

            // Act.
            var actual = value.IsMissingOrTooLong(10);

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void EnsureTrailingSlash_NullString_ReturnsNull()
        {
            // Arrange.
            string value = null;
            string expected = null;

            // Act.
            var actual = value.EnsureTrailingSlash();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void EnsureTrailingSlash_EmptyString_ReturnsTrailingSlash()
        {
            // Arrange.
            string value = "";
            string expected = "/";

            // Act.
            var actual = value.EnsureTrailingSlash();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void EnsureTrailingSlash_NoTrailingSlashString_ReturnsWithTrailingSlash()
        {
            // Arrange.
            string value = "https://accc.gov.au";
            string expected = "https://accc.gov.au/";

            // Act.
            var actual = value.EnsureTrailingSlash();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void EnsureTrailingSlash_WithTrailingSlashString_ReturnsWithTrailingSlash()
        {
            // Arrange.
            string value = "https://accc.gov.au/";
            string expected = "https://accc.gov.au/";

            // Act.
            var actual = value.EnsureTrailingSlash();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AddQueryString_WithNullUrl_ThrowsArgumentNullException()
        {
            // Arrange.
            string url = null;
            string query = "a=b";

            // Act + Assert.
            Assert.Throws<ArgumentNullException>(() => url.AddQueryString(query));

            // Assert.
        }

        [Fact]
        public void AddQueryString_WithNullQuery_ThrowsArgumentNullException()
        {
            // Arrange.
            string url = "/path";
            string query = null;

            // Act + Assert.
            Assert.Throws<ArgumentNullException>(() => url.AddQueryString(query));

            // Assert.
        }

        [Fact]
        public void AddQueryString_WithNoExistingQueryString_AddsQuestionMarkQueryString()
        {
            // Arrange.
            string url = "/path";
            string query = "a=b";
            string expected = "/path?a=b";

            // Act + Assert.
            var actual = url.AddQueryString(query);

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AddQueryString_WithExistingQueryString_AddsAmpersandQueryString()
        {
            // Arrange.
            string url = "/path?1=2";
            string query = "a=b";
            string expected = "/path?1=2&a=b";

            // Act + Assert.
            var actual = url.AddQueryString(query);

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AddQueryStringNameValue_WithNullUrl_ThrowsArgumentNullException()
        {
            // Arrange.
            string url = null;
            string name = "a";
            string value = "b";

            // Act + Assert.
            Assert.Throws<ArgumentNullException>(() => url.AddQueryString(name, value));

            // Assert.
        }

        [Fact]
        public void AddQueryStringNameValue_WithNullName_ThrowsArgumentException()
        {
            // Arrange.
            string url = "/path";
            string name = null;
            string value = "b";

            // Act + Assert.
            Assert.Throws<ArgumentException>(() => url.AddQueryString(name, value));

            // Assert.
        }

        [Fact]
        public void AddQueryStringNameValue_WithEmptyName_ThrowsArgumentException()
        {
            // Arrange.
            string url = "/path";
            string name = "";
            string value = "b";

            // Act + Assert.
            Assert.Throws<ArgumentException>(() => url.AddQueryString(name, value));

            // Assert.
        }

        [Fact]
        public void AddQueryStringNameValue_WithWhitespaceName_ThrowsArgumentException()
        {
            // Arrange.
            string url = "/path";
            string name = "   ";
            string value = "b";

            // Act + Assert.
            Assert.Throws<ArgumentException>(() => url.AddQueryString(name, value));

            // Assert.
        }

        [Fact]
        public void AddQueryStringNameValue_WithNullValue_ReturnsUrlAndQuery()
        {
            // Arrange.
            string url = "/path";
            string name = "a";
            string value = null;
            string expected = "/path?a=";

            // Act.
            var actual = url.AddQueryString(name, value);

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AddQueryStringNameValue_WithNoExistingQueryString_AddsQuestionMarkQueryString()
        {
            // Arrange.
            string url = "/path";
            string name = "a";
            string value = "b";
            string expected = "/path?a=b";

            // Act + Assert.
            var actual = url.AddQueryString(name, value);

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AddQueryStringNameValue_WithExistingQueryString_AddsAmpersandQueryString()
        {
            // Arrange.
            string url = "/path?1=2";
            string name = "a";
            string value = "b";
            string expected = "/path?1=2&a=b";

            // Act + Assert.
            var actual = url.AddQueryString(name, value);

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AddHashFragment_WithNullUrl_ThrowsArgumentNullException()
        {
            // Arrange.
            string url = null;
            string query = "a";

            // Act + Assert.
            Assert.Throws<ArgumentNullException>(() => url.AddHashFragment(query));

            // Assert.
        }

        [Fact]
        public void AddHashFragment_WithNullQuery_ThrowsArgumentNullException()
        {
            // Arrange.
            string url = "/path";
            string query = null;

            // Act + Assert.
            Assert.Throws<ArgumentNullException>(() => url.AddHashFragment(query));

            // Assert.
        }

        [Fact]
        public void AddHashFragment_WithExistingHash_ReturnsValue()
        {
            // Arrange.
            string url = "/path#";
            string query = "a";
            string expected = "/path#a";

            // Act.
            var actual = url.AddHashFragment(query);

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AddHashFragment_WithNoExistingHash_ReturnsValue()
        {
            // Arrange.
            string url = "/path";
            string query = "a";
            string expected = "/path#a";

            // Act.
            var actual = url.AddHashFragment(query);

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ParseScopesString_WithNullScopes_ReturnsNull()
        {
            // Arrange.
            string scopes = null;
            List<string> expected = null;

            // Act.
            var actual = scopes.ParseScopesString();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ParseScopesString_WithEmptyScopes_ReturnsNull()
        {
            // Arrange.
            string scopes = "";
            List<string> expected = null;

            // Act.
            var actual = scopes.ParseScopesString();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ParseScopesString_WithWhitespaceScopes_ReturnsNull()
        {
            // Arrange.
            string scopes = "    ";
            List<string> expected = null;

            // Act.
            var actual = scopes.ParseScopesString();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ParseScopesString_WithScopes_ReturnsList()
        {
            // Arrange.
            string scopes = "scope1 scope2 scope3";
            List<string> expected = new List<string>(new string[] { "scope1", "scope2", "scope3" });

            // Act.
            var actual = scopes.ParseScopesString();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ParseScopesString_WithDuplicateScopes_ReturnsDistinctList()
        {
            // Arrange.
            string scopes = "scope1 scope2 scope3 scope1 scope3";
            List<string> expected = new List<string>(new string[] { "scope1", "scope2", "scope3" });

            // Act.
            var actual = scopes.ParseScopesString();

            // Assert.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ParseScopesString_WithUnsortedScopes_ReturnsSortedList()
        {
            // Arrange.
            string scopes = "z y x";
            List<string> expected = new List<string>(new string[] { "x", "y", "z" });

            // Act.
            var actual = scopes.ParseScopesString();

            // Assert.
            Assert.Equal(expected, actual);
        }
    }
}
