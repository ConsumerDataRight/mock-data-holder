using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CDR.DataHolder.API.Infrastructure.IdPermanence
{
    public interface IIdPermanenceManager
    {
        string EncryptId(string internalId, IdPermanenceParameters idParameters);
        IEnumerable<T> EncryptIds<T>(IEnumerable<T> list, IdPermanenceParameters idParameters, params Expression<Func<T, string>>[] idProperties);
        string DecryptId(string encryptedId, IdPermanenceParameters idParameters);
        string EncryptSub(string customerId, SubPermanenceParameters subParameters);
        string DecryptSub(string sub, SubPermanenceParameters subParameters);
    }
}