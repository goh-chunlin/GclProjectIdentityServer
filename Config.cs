using IdentityServer4.Models;
using System.Collections.Generic;
using IdentityServer4;

namespace GclProjectIdentityServer
{
	public class Config
	{
		public static IEnumerable<IdentityResource> GetIdentityResources()
		{
			return new List<IdentityResource>
			{
				new IdentityResources.OpenId(),
				new IdentityResources.Profile(),
				new IdentityResources.Email(),
			};
		}

		// scopes define the API resources in your system
		public static IEnumerable<ApiResource> GetApiResources()
		{
			return new List<ApiResource>
			{
				new ApiResource("api1", "My API")
			};
		}

		// client want to access resources (aka scopes)
		public static IEnumerable<Client> GetClients(string domainName)
		{
			return new List<Client>
			{
				new Client
				{
					ClientId = "client",
					ClientName = "MVC Client",
					AllowedGrantTypes = GrantTypes.Implicit,

					RequireConsent = false,

					ClientSecrets =
					{
						new Secret("secret")
					},

					RedirectUris           = { $"{domainName}/signin-oidc" },
					PostLogoutRedirectUris = { $"{domainName}/signout-callback-oidc" },

					AllowedScopes = {
						IdentityServerConstants.StandardScopes.OpenId,
						IdentityServerConstants.StandardScopes.Profile,
						"api1"
					},

					AllowOfflineAccess = true

				}
			};
		}
	}
}