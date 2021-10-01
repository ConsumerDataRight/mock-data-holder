using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using CDR.DataHolder.API.Infrastructure.Extensions;
using CDR.DataHolder.Domain.Repositories;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.Extensions.Configuration;

namespace CDR.DataHolder.IdentityServer.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IIdSvrRepository _idSvrRepository;
        private readonly IConfiguration _configuration;                

        public ProfileService(IIdSvrRepository idSvrRepository, IConfiguration configuration)
        {
            _idSvrRepository = idSvrRepository;
            _configuration = configuration;            
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            if (context.Subject.Identity.AuthenticationType == "UserInfo")
            {
                var sub = context.Subject.GetSubject(_configuration);
                var userInfoClaims = await _idSvrRepository.GetUserInfoClaims(new Guid(sub));
                
                if (userInfoClaims != null)
                {
                    context.IssuedClaims = new List<Claim>(context.IssuedClaims)
                    {
                        new Claim(JwtClaimTypes.GivenName, userInfoClaims.GivenName),
                        new Claim(JwtClaimTypes.FamilyName, userInfoClaims.FamilyName),
                        new Claim(JwtClaimTypes.Name, userInfoClaims.Name),
                        new Claim(JwtClaimTypes.Audience, context.Client.ClientId),
                        new Claim(JwtClaimTypes.Issuer, _configuration["IssuerUri"].ToString())
                    };
                }                
            }                
        }

        public async Task IsActiveAsync(IsActiveContext context)
        {                        
            await Task.FromResult(0);
        }
    }
}
