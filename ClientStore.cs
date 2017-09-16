using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Configuration;

namespace GclProjectIdentityServer
{
    public class ClientStore : IClientStore
    {
        public ClientStore(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public IEnumerable<Client> GetClients()
        {
            var availableClients = new List<Client>();

            availableClients.Add(new Client
                {
                    ClientId = "client",
                    ClientName = "MVC Client",
                    AllowedGrantTypes = GrantTypes.Implicit,

                    RequireConsent = false,

                    ClientSecrets =
                    {
                        new Secret("secret")
                    },

                    RedirectUris           = { $"{Configuration["AppSettings:DomainName"]}/signin-oidc" },
                    PostLogoutRedirectUris = { $"{Configuration["AppSettings:DomainName"]}/signout-callback-oidc" },

                    AllowedScopes = {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "api1"
                    },

                    AllowOfflineAccess = true

                });

            availableClients.Add(
                new Client
                {
                    ClientId = Configuration["AppSettings:Gcl-ApiClientId"],
                    AllowedGrantTypes = GrantTypes.ClientCredentials,

                    ClientSecrets = { new Secret(Configuration["AppSettings:Gcl-ApiClientSecret"].Sha256()) },
                    AllowedScopes = { "api1" }
                });

            return availableClients;
        }

        public Task<Client> FindClientByIdAsync(string clientId)
        {
            return Task.FromResult(GetClients().FirstOrDefault(c => c.ClientId == clientId));
        }
	}
}