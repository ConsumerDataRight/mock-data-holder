using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Jwk;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Stores
{
	public class ClientStore : IClientStore
	{
		protected readonly ILogger<ClientStore> _logger;
		protected readonly ConfigurationDbContext _configurationDbContext;

		public ClientStore(ILogger<ClientStore> logger,
			ConfigurationDbContext configurationDbContext)
		{
			_logger = logger;
			_configurationDbContext = configurationDbContext;
		}

		public async Task<IdentityServer4.Models.Client> FindClientByIdAsync(string clientId)
		{
			_logger.LogInformation("DynamicClientStore FindClientByIdAsync");

			var client = await _configurationDbContext.Clients
				.Include(c => c.RedirectUris)
				.Include(c => c.ClientSecrets)
				.Include(c => c.AllowedGrantTypes)
				.Include(c => c.AllowedScopes)
				.Include(c => c.Claims)
				.FirstOrDefaultAsync(x => x.ClientId == clientId);

			if (client == null)
			{
				_logger.LogError("Client {ClientId} is not found.", clientId);
				return null;
			}

			// This implements the client key rotation 
			// Get the latest JWKs from the client secrets. The keys may have been rotated from the client side, so it is always best to get the latest keys dynamically.
			// Filter only URIs as there can be other secrets such as keys.
			var updatedClientSecrets = new List<ClientSecret>();
			foreach (var clientSecret in client.ClientSecrets)
			{
				if (clientSecret.Type == SecretTypes.JsonWebKey && Uri.IsWellFormedUriString(clientSecret.Value, UriKind.Absolute))
				{
					try
					{
						var jwks = await GetJwks(clientSecret.Value);
						updatedClientSecrets.AddRange(
							jwks.Keys.Select(key => ConvertJwkToClientSecret(key)));
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Failed to get JWKs from JwksUri endpoint {jwksUri}", clientSecret.Value);
					}
				}
				else
				{
					updatedClientSecrets.Add(clientSecret);
				}
			}
			client.ClientSecrets = updatedClientSecrets;

			return client.ToModel();
		}

		private static ClientSecret ConvertJwkToClientSecret(IdentityModel.Jwk.JsonWebKey jsonWebKey)
		{
			return new ClientSecret
			{
				Type = SecretTypes.JsonWebKey,
				Value = JsonConvert.SerializeObject(jsonWebKey),
				Description = SecretDescription.Encyption
			};
		}

		/// <summary>
		/// Note:
		/// This can be cached for a short period of time for performance enhancement
		/// </summary>
		private async Task<JsonWebKeySet> GetJwks(string jwksEndpoint)
		{
			_logger.LogInformation($"{nameof(ClientStore)}.{nameof(GetJwks)}");

			var clientHandler = new HttpClientHandler();
			clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

			_logger.LogInformation("Retrieving JWKS from: {jwksEndpoint}", jwksEndpoint);

			var jwksClient = new HttpClient(clientHandler);
			var jwksResponse = await jwksClient.GetAsync(jwksEndpoint);
			var jwks = await jwksResponse.Content.ReadAsStringAsync();

			_logger.LogDebug("JWKS: {jwks}", jwks);

			return new JsonWebKeySet(jwks);
		}
	}
}
