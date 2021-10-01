using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace CDR.DataHolder.IntegrationTests.Extensions.UnitTests
{
    public class JTokenExtensionsUnitTests
    {
        [Theory]
        [InlineData("foo", null, false)]
        [InlineData("myString", null, false)]
        [InlineData("myString", "", false)]
        [InlineData("myString", "abc", true)]
        [InlineData("myString", "xyz", false)]
        [InlineData("mystring", "abc", false)]
        [InlineData("myString", "ABC", false)]
        public void ContainsString(string key, string value, bool expectedResult)
        {
            var jToken = JToken.Parse(@"
            {
                myString: ""abc"",
                myInt: 1,
                myBool: true
            }");

            jToken.Contains(key, value).Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("myBool", true, true)]
        [InlineData("myBool", false, false)]
        public void ContainsBool(string key, bool value, bool expectedResult)
        {
            var jToken = JToken.Parse(@"
            {
                myString: ""abc"",
                myInt: 1,
                myBool: true
            }");

            jToken.Contains(key, value).Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("foo", null, false)]
        [InlineData("foo", "", false)]
        [InlineData("foo", "a", false)]
        [InlineData("myArray", null, false)]
        [InlineData("myArray", "", false)]
        [InlineData("myArray", "a", true)]
        [InlineData("myArray", "1", false)] // not a string
        [InlineData("myarray", "a", false)]
        [InlineData("myArray", "A", false)]
        public void ArrayContains(string key, string element, bool expectedResult)
        {
            var jToken = JToken.Parse(@"
            {
                myArray: [""a"", ""b"", ""c"", 1, {foo: ""bar""}]
            }");

            jToken.ArrayContains(key, element).Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("foo", null, false)]
        [InlineData("myArray", null, false)]
        [InlineData("myArray", new string[] { "" }, false)]
        [InlineData("myArray", new string[] { "a" }, true)]
        [InlineData("myArray", new string[] { "a", "z" }, false)]
        [InlineData("myArray", new string[] { "z" }, false)]
        public void ArrayContainsAll(string key, string[] elements, bool expectedResult)
        {
            var jToken = JToken.Parse(@"
            {
                myArray: [""a"", ""b"", ""c""]
            }");

            jToken.ArrayContainsAll(key, elements).Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("foo", null, false)]
        [InlineData("myArray", null, false)]
        [InlineData("myArray", new string[] { "" }, false)]
        [InlineData("myArray", new string[] { "a" }, true)]
        [InlineData("myArray", new string[] { "a", "z" }, true)]
        [InlineData("myArray", new string[] { "z" }, false)]
        public void ArrayContainsAny(string key, string[] elements, bool expectedResult)
        {
            var jToken = JToken.Parse(@"
            {
                myArray: [""a"", ""b"", ""c""]
            }");

            jToken.ArrayContainsAny(key, elements).Should().Be(expectedResult);
        }
    }
}

namespace CDR.DataHolder.IntegrationTests.Extensions
{
    static public class JTokenExtensions
    {
        /// <summary>
        /// Does JToken have key with given string value?
        /// </summary>
        static public bool Contains(this JToken jToken, string key, string value)
        {
            var childJToken = jToken[key];

            if (childJToken == null || childJToken.Type != JTokenType.String)
            {
                return false;
            }

            return childJToken.Value<string>() == value;
        }

        /// <summary>
        /// Does JToken have key with given bool value?
        /// </summary>
        static public bool Contains(this JToken jToken, string key, bool value)
        {
            var childJToken = jToken[key];

            if (childJToken == null || childJToken.Type != JTokenType.Boolean)
            {
                return false;
            }

            return childJToken.Value<bool>() == value;
        }

        /// <summary>
        /// Is jToken[key] an array?
        /// </summary>
        static public bool IsArray(this JToken jToken, string key)
        {
            var childJToken = jToken[key];

            return
                childJToken != null &&
                childJToken.Type == JTokenType.Array;
        }

        /// <summary>
        /// Length of jToken[key] array
        /// </summary>
        static public int ArrayLength(this JToken jToken, string key)
        {
            if (!jToken.IsArray(key))
            {
                throw new Exception($"'{key}' is not an array");
            }

            var childJToken = jToken[key];

            return childJToken.Children().Count();
        }

        /// <summary>
        /// Returns intersection of array and jToken[key] array
        /// </summary>
        static public string[] ArrayIntersection(this JToken jToken, string key, string[] array)
        {
            if (!jToken.IsArray(key) || array == null)
            {
                return Array.Empty<string>();
            }

            var childValues = jToken[key]
                .Children()
                .Where(jt => jt.Type == JTokenType.String)
                .Select(jt => jt.Value<string>())
                .ToArray();

            var intersection = childValues.Intersect(array).ToArray();

            return intersection;
        }

        /// <summary>
        /// Returns true jToken[key] array has same elements as array
        /// </summary>
        static public bool ArrayEquals(this JToken jToken, string key, string[] array)
        {
            if (!jToken.IsArray(key) || array == null)
            {
                return false;
            }

            return
                jToken.ArrayContainsAll(key, array) &&
                jToken.ArrayLength(key) == array.Length;
        }

        /// <summary>
        /// Returns true if jToken[key] array contains element
        /// </summary>
        static public bool ArrayContains(this JToken jToken, string key, string element)
        {
            if (!jToken.IsArray(key) || element == null)
            {
                return false;
            }

            return jToken.ArrayContainsAll(key, new string[] { element });
        }

        /// <summary>
        /// Returns true if jToken[key] array contains all elements of array
        /// </summary>
        static public bool ArrayContainsAll(this JToken jToken, string key, string[] array)
        {
            if (!jToken.IsArray(key) || array == null)
            {
                return false;
            }

            var intersection = jToken.ArrayIntersection(key, array);

            return intersection.Length == array.Length;
        }

        /// <summary>
        /// Returns true if jToken[key] array contains any elements of array
        /// </summary>
        static public bool ArrayContainsAny(this JToken jToken, string key, string[] array)
        {
            if (!jToken.IsArray(key) || array == null)
            {
                return false;
            }

            var intersection = jToken.ArrayIntersection(key, array);

            return intersection.Length >= 1;
        }



        /// <summary>
        /// Remove child JToken from JToken
        /// </summary>
        /// <param name="jToken">JToken from which to remove child JToken</param>
        /// <param name="key">Key of child to remove</param>
        static public void Remove(this JToken jToken, string key)
        {
            var t = jToken[key];

            if (t == null)
            {
                throw new Exception($"Key '{key}' not found");
            }

            switch (t.Type)
            {
                case JTokenType.String:
                    t.Parent.Remove();
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Should a JToken be removed?
        /// </summary>
        public delegate bool ShouldRemove(JToken jToken);

        /// <summary>
        /// Recursively remove child JTokens from a JToken
        /// </summary>
        /// <param name="rootJToken">Root JToken to start processing from</param>
        /// <param name="shouldRemove">Should a token be removed?</param>
        public static void RemoveJTokens(this JToken rootJToken, ShouldRemove shouldRemove)
        {
            List<JToken> jTokensToRemove = new();

            void DoJToken(JToken jToken)
            {
                // Find nodes that are empty
                foreach (var childJToken in jToken.Children())
                {
                    // Should this token be removed?
                    if (shouldRemove(childJToken))
                    {
                        jTokensToRemove.Add(childJToken);
                    }
                    // Not marked for removal, so process children if applicable
                    else if (childJToken.HasValues)
                    {
                        // Process children
                        DoJToken(childJToken);
                    }
                }
            }

            // Build list of JTokens to remove
            DoJToken(rootJToken);

            // Remove the JTokens marked for removal
            foreach (var jTokenToRemove in jTokensToRemove)
            {
                jTokenToRemove.Parent.Remove();
            }
        }

        /// <summary>
        /// Recursively remove null JTokens from a root JToken
        /// </summary>
        public static void RemoveNulls(this JToken rootJToken)
        {
            RemoveJTokens(rootJToken, (jToken) => jToken.Type == JTokenType.Null);
        }

        /// <summary>
        /// Recursively remove empty array JTokens from a root JToken
        /// </summary>
        public static void RemoveEmptyArrays(this JToken rootJToken)
        {
            RemoveJTokens(rootJToken, (jToken) => jToken.Type == JTokenType.Array && !jToken.HasValues);
        }

        public static void RemovePath(this JToken jToken, string jsonPath)
        {
            var tokens = jToken.SelectTokens(jsonPath).ToList();

            foreach (var token in tokens)
            {
                token.Parent.Remove();
            }
        }

        public delegate JToken Replace(JToken token);
        public static void ReplacePath(this JToken jToken, string jsonPath, Replace replace)
        {
            var tokens = jToken.SelectTokens(jsonPath);

            foreach (var token in tokens)
            {
                token.Replace(replace(token));
            }
        }

        /// <summary>
        /// Sort tokens under jsonPath by key
        /// </summary>
        public static void Sort(this JToken rootToken, string jsonPath, string key)
        {
            var tokens = rootToken.SelectTokens(jsonPath);

            foreach (var token in tokens)
            {
                if (token.Type != JTokenType.Array)
                {
                    throw new Exception($"{token.Path} is not an array");
                }

                var array = token as JArray;

                var sorted = new JArray(array.OrderBy(obj => obj[key]));

                token.Replace(sorted);
            }
        }

        /// <summary>
        /// Sort array by key
        /// </summary>
        public static void SortArray(this JToken token, string key)
        {
            // Check token is array
            if (token.Type != JTokenType.Array)
            {
                throw new Exception($"{token.Path} is not an array");
            }

            // Exit if nothing to srot
            if (!token.HasValues)
            {
                return;
            }

            // Sort the children by key
            var sortedChildTokens = token.Children().OrderBy(childToken => childToken[key]);

            // And replace
            token.Replace(new JArray(sortedChildTokens));
        }

        /// <summary>
        /// Sort path arrays by key
        /// </summary>
        /// <param name="path">For arrays matching this path</param>
        /// <param name="key">Key to sort array by</param>
        public static void SortArray(this JToken token, string path, string key)
        {
            var tokens = token.SelectTokens(path);

            foreach (var _token in tokens)
            {
                _token.SortArray(key);
            }
        }
    }
}