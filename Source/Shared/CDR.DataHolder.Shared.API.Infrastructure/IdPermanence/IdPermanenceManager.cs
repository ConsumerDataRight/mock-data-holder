using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;

namespace CDR.DataHolder.Shared.API.Infrastructure.IdPermanence
{
    /// <summary>
    /// Id Permanence Manager
    /// </summary>
    public class IdPermanenceManager : IIdPermanenceManager
    {
        private string? _privateKey;
        private readonly IConfiguration _config;

        public IdPermanenceManager(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Method to create permanence ids for specified properties in a list of objects
        /// </summary>
        /// <typeparam name="T">The type of list</typeparam>
        /// <param name="list">The list</param>
        /// <param name="idParameters">The permanence id parameters</param>
        /// <param name="idProperties">The specified id properties to create permanence ids for</param>
        /// <returns>The list with permanence ids set</returns>
        public IEnumerable<T> EncryptIds<T>(IEnumerable<T> list, IdPermanenceParameters idParameters, params Expression<Func<T, string>>[] idProperties)
        {
            var privateKey = GetPrivateKey();

            return list.Select(item =>
            {
                idProperties.ToList().ForEach(idProperty =>
                {
                    var id = idProperty.Compile()(item);
                    //Generate Permanence Id
                    id = IdPermanenceManager.EncryptId(id, idParameters, privateKey);

                    var memberSelectorExpression = idProperty.Body as MemberExpression;
                    if (memberSelectorExpression != null)
                    {
                        var property = memberSelectorExpression.Member as PropertyInfo;
                        if (property != null)
                        {
                            property.SetValue(item, id, null);
                        }
                    }
                });

                return item;
            }).ToList();
        }

        /// <summary>
        /// Encrypt an ID to meet ID Permanence rules.
        /// </summary>
        /// <param name="internalId">Internal ID (i.e. accountId, transactionId) to encrypt</param>
        /// <param name="idParameters">IdPermanenceParameters</param>
        /// <returns>Encrypted ID</returns>
        public string EncryptId(string internalId, IdPermanenceParameters idParameters)
        {
            var privateKey = GetPrivateKey();
            return IdPermanenceHelper.EncryptId(internalId, idParameters, privateKey);
        }

        private static string EncryptId(string internalId, IdPermanenceParameters idParameters, string privateKey)
        {
            return IdPermanenceHelper.EncryptId(internalId, idParameters, privateKey);
        }

        /// <summary>
        /// Decrypt an encrypted ID back to the internal value.
        /// </summary>
        /// <param name="encryptedId">Encrypted ID to decrypt back to internal value</param>
        /// <param name="idParameters">IdPermanenceParameters</param>
        /// <returns>Internal ID</returns>
        public string DecryptId(string encryptedId, IdPermanenceParameters idParameters)
        {
            var privateKey = GetPrivateKey();
            return IdPermanenceHelper.DecryptId(encryptedId, idParameters, privateKey);
        }

        /// <summary>
        /// Encrypt the internal customer id for inclusion as the "sub" claim in id_token and access_token.
        /// </summary>
        /// <param name="customerId">Internal Customer Id</param>
        /// <param name="subParameters">SubPermanenceParameters</param>
        /// <returns>Encrypted customer id to be included in sub claim</returns>
        public string EncryptSub(string customerId, SubPermanenceParameters subParameters)
        {
            var privateKey = GetPrivateKey();
            return IdPermanenceHelper.EncryptSub(customerId, subParameters, privateKey);
        }

        /// <summary>
        /// Decrypt the encrypted sub claim value from the access_token into the internal customer id.
        /// </summary>
        /// <param name="sub">Encrypted Customer Id found in sub claim of the access_token</param>
        /// <param name="subParameters">SubPermanenceParameters</param>
        /// <returns>Internal Customer Id</returns>
        public string DecryptSub(string sub, SubPermanenceParameters subParameters)
        {
            var privateKey = GetPrivateKey();
            return IdPermanenceHelper.DecryptSub(sub, subParameters, privateKey);
        }

        private string GetPrivateKey()
        {
            if (!string.IsNullOrEmpty(_privateKey))
            {
                return _privateKey;
            }

            _privateKey = IdPermanenceHelper.GetPrivateKey(_config);
            return _privateKey;
        }
    }
}
