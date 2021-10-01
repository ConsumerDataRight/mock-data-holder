using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web;
using HtmlAgilityPack;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Infrastructure.API2
{
    /// <summary>
    /// Html parsing
    /// </summary>
    static class HtmlParser
    {
        /// <summary>
        /// Parse form and return dictionary of input elements
        /// </summary>
        public static Dictionary<string, string?> ParseForm(string html, string formXPath)
        {
            var formFields = new Dictionary<string, string?>();

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            // Get form
            var form = htmlDoc.DocumentNode.SelectSingleNode(formXPath)
                ?? throw new Exception($"xpath '{formXPath}' not found");

            // Get each input in form and add to dictionary
            var inputNodes = form.SelectNodes("//input");
            foreach (var inputNode in inputNodes)
            {
                string value = inputNode.Attributes["Value"].Value;

                // Need to HtmlDecode the value, because it will be re-encoded later
                value = HttpUtility.HtmlDecode(value);

                formFields[inputNode.Attributes["Name"].Value] = value;
            }

            return formFields;
        }

        // Return FormUrlEncodedContent for dictionary
        public static FormUrlEncodedContent FormUrlEncodedContent(Dictionary<string, string?> formFields)
        {
            var list = new List<KeyValuePair<string?, string?>>();

            foreach (var kvp in formFields)
            {
                list.Add(new KeyValuePair<string?, string?>(kvp.Key, kvp.Value));
            }

            return new FormUrlEncodedContent(list);
        }
    }
}