using System;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CDR.DataHolder.IdentityServer.Serialization
{
    //public class ScopeValidatorConverter : JsonConverter
    //{
    //    public override bool CanConvert(Type objectType)
    //    {
    //        return typeof(ScopeValidator) == objectType;
    //    }

    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {
    //        var source = serializer.Deserialize<ScopeValidatorLite>(reader);
    //        var resourceStore = new InMemoryResourcesStore(
    //            source.GrantedResources.IdentityResources, source.GrantedResources.ApiResources);

    //        var target = new ScopeValidator(resourceStore, new LoggerFactory().CreateLogger<ScopeValidator>());
    //        target.SetConsentedScopes(source.RequestedResources.ToScopeNames());
    //        target.AreScopesValidAsync(source.RequestedResources.ToScopeNames());

    //        return target;
    //    }

    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //    {
    //        var source = (ScopeValidator)value;

    //        var target = new ScopeValidatorLite
    //        {
    //             GrantedResources = source.GrantedResources,
    //             RequestedResources = source.RequestedResources,
    //        };

    //        serializer.Serialize(writer, target);
    //    }
    //}
}
