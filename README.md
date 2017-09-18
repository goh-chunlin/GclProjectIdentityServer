# GclProjectIdentityServer
This is an IdentityServer using ASP .NET Core Identity to access the resources in GCL Projects.

## Objective
This project is to develop and maintain Identity Server that will be used by many projects in GCL Projects which requires a Single Sign-On (SSO) solution. By doing this, user who logs in from an application in GCL Projects can also access the another resource in GCL Projects with the same credential.

| ![Traditional Authentication Scheme (Source: http://www.devx.com/supportitems/showSupportItem.php?co=37692&supportitem=figure1)](github-images/traditional-authentication-scheme.jpg?raw=true) | ![OpenID Authentication Scheme (Source: http://www.devx.com/supportitems/showSupportItem.php?co=37692&supportitem=figure2)](github-images/openid-authentication-scheme.jpg?raw=true) |
| --- | --- |
| Traditional Authentication Scheme | OpenID Authentication Scheme |

Outsourcing the Authentication and Authorization functions to a security token service prevents duplicating that functionality across those applications and endpoints. Due to the fact that OpenID Connect is an extension on top of OAuth 2.0, the two fundamental security concerns, Authentication and API access, are combined into a single protocol - often with a single round trip to the security token service.

Hence, IdentityServer4, a middleware that adds the spec compliant OpenID Connect and OAuth 2.0 endpoints to an ASP.NET Core application, is chosen because it is an implementation of these two protocols to solve the typical security problems of modern mobile, native and web applications.

## Architecture Overview
IdentityServer uses a Security Token Service to provide token based protection to resources, such as web apps, mobile apps, and API calls.

In this project, we have
- A database of user Claims, based on ASP.Net Core Identity;
- A Security Token Service for authenticating the user, and providing a token containing the Claims, using IdentityServer.

![Clients, Scopes, and Claims (Source: https://stackoverflow.com/a/39560625/1177328)](https://i.stack.imgur.com/gxGob.png)

IdentityServer is designed for flexibility and part of that is allowing us to use any database we want. Since we are starting with a new user database, then ASP.NET Identity is what we have chosen in this project.

![OpenID Authentication Flow (Source: http://www.devx.com/supportitems/showSupportItem.php?co=37692&supportitem=figure3)](github-images/openid-authentication-flow.jpg?raw=true)

## Setup (Step 1): Adding Necessary Nuget Packages
The following Nuget packages are needed. At the point of writing this document, the version we use are 2.0 RC1 or 2.0.
- [IdentityServer4 2.0.0-rc1](https://www.nuget.org/packages/IdentityServer4/2.0.0-rc1)
- [IdentityServer4.AspNetIdentity 2.0.0-rc1](https://www.nuget.org/packages/IdentityServer4.AspNetIdentity/2.0.0-rc1)
- [Microsoft.AspNetCore.Authentication.OpenIdConnect 2.0.0](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.OpenIdConnect/)

## Setup (Step 2): Defining Resources and Clients
IdentityServer4 must know what scopes can be requested by users. These are defined as **Resources**. IdentityServer4 has two kinds of resources:
1. **API Resources**: Protected data or functionality which a user might gain access to with an access token. An example of an API resource would be a Web API that require authorization to call.
2. **Identity Resources**: Claims which are given to a client to identify a user. This could include their name, email address, or other claims. Identity information is returned in an ID token by OpenID Connect flows.

Both resources and clients which want to access resources can be [defined in a single **Config.cs** file](https://github.com/goh-chunlin/GclProjectIdentityServer/blob/master/Config.cs).

## Setup (Step 3): Configure and Add IdentityServer to ASP .NET Core
In Startup.cs file, we need to modify the codes in ConfigureServices so that the required services are configured and added to the Dependency Injection system.

```
public void ConfigureServices(IServiceCollection services)
{
    ...
    
    // Adds IdentityServer
    services.AddIdentityServer()
        .AddDeveloperSigningCredential()
        .AddInMemoryIdentityResources(Config.GetIdentityResources())
        .AddInMemoryApiResources(Config.GetApiResources())
        .AddInMemoryClients(new ClientStore(Configuration).GetClients())
        .AddAspNetIdentity<ApplicationUser>();

    services.AddMvc();
}
```

### Components Explanation
- **AddIdentityServer()**: To register IdentityServer4 services;
- **AddDeveloperSigningCredential()**: To be used for testing with an auto-generated certificate until a real certificate is available. Take note that we are using `AddDeveloperSigningCredential()` because [the original `AddTemporarySigningCredential()` which is used in previous releases of IdentityServer4 has been removed](https://github.com/IdentityServer/IdentityServer4/issues/1139). Alternatively, we can create a self-signing certificate and then use `AddSigningCredential()` to load the certificate from the machine certificate store;
- **AddInMemoryIdentityResources()**: To include the identity resources;
- **AddInMemoryApiResources()**: To include the API resources;
- **AddInMemoryClients()**: To configure clients because IdentityServer4 must be configured with a list of clients that will be requesting tokens;
- **AddAspIdentity()**: To get user profile information from our ASP.NET Core Identity context, and will automatically setup the necessary IResourceOwnerPasswordValidator for validating credentials. It will also configure IdentityServer4 to correctly extract JWT subject, user name, and role claims from ASP.NET Core Identity entities.

In Configure the middleware is added to the HTTP pipeline.
```
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    ...

    // Adds IdentityServer
    app.UseIdentityServer();

    app.UseMvc(...);
}
```

If we view the **Discovery Document** at `http://localhost:4000/.well-known/openid-configuration` (The port is 4000 because it [is defined so in the **Program.cs** file](https://github.com/goh-chunlin/GclProjectIdentityServer/blob/master/Program.cs), then  we will be able to see the following which will be used by our clients and APIs to download the necessary configuration data.

```
{
    "issuer":"http://localhost:4000",
    "jwks_uri":"http://localhost:4000/.well-known/openid-configuration/jwks",
    "authorization_endpoint":"http://localhost:4000/connect/authorize",
    "token_endpoint":"http://localhost:4000/connect/token",
    "userinfo_endpoint":"http://localhost:4000/connect/userinfo",
    "end_session_endpoint":"http://localhost:4000/connect/endsession",
    "check_session_iframe":"http://localhost:4000/connect/checksession",
    "scopes_supported":["openid","profile","email","api1","offline_access"],
    "id_token_signing_alg_values_supported":["RS256"],
    "code_challenge_methods_supported":["plain","S256"],
    ...
}
```

We can also use **Postman** to test the clients, as shown in the screenshot below. 
![Testing the Client on Postman](github-images/postman-identityserver-test.png?raw=true)

### Scopes
Scopes represent what we are allowed to do. In IdentityServer4 scopes are modelled as resources, which come in two flavors:
- Identity;
- API.

An identity resource allows us to model a scope that will return a certain set of claims, whilst an API resource scope allows us to model access to a protected resource (typically an API).

### Grant Types
Grant Types are ways a client wants to interact with IdentityServer. The OpenID Connect and OAuth 2 specs define the following grant types:
- Resource Owner Password;
- Client Credential;
- Implicit;
- Authorization Code;
- Hybrid;
- Refresh Token;
- Extension Grants.

![Deciding which Grant Type to Use(Source: http://oauth2.thephpleague.com/authorization-server/which-grant/)](github-images/which-grants.png?raw=true)

#### Resource Owner Password (Not Recommended!)
It allows to request tokens **on behalf** of a user by sending the user's name and password to the token endpoint. This is the so called "non-interactive" authentication and is generally **not recommended**, unless used in certain legacy or first-party integration scenarios.

#### Client Credential
This is the simplest grant type and is used for **server to server** communication. Hence, tokens are always requested on behalf of a client, not a user. With this grant type we send a token request to the token endpoint, and get an access token back that represents the client. The **client has to authenticate with the token endpoint** using its client ID and secret.

#### Implicit
It allows a client to obtain an access token directly from the authorization endpoint, without contacting the token endpoint nor authenticating the client. An important characteristic of the OAuth2 implicit grant is the fact that such flows never return Refresh Tokens to the client. Once the token is expired, the user will need to log in again to have further access to the web resource.

![Implicit Grant (Source: http://www.qeo.org/Doc/Implicit-Grant_21676147.html)](github-images/implicit-grant.png?raw=true)

#### Authorization Code
The Authorization Code grant provides additional security, but it only works when we have a web server requesting the protected resources. Since the web server can store the access token, we run less risk of the access token being exposed to the Internet, and we can issue a token that lasts a long time. And since the web server is trusted, it can be given a "refresh token", so it can get a new access token when the old one expires.

![Authorization Code Grant (Source: https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-protocols-oauth-code)](github-images/authorization-code-grant.png?raw=true)

#### Hybrid
Hybrid flow is a combination of the implicit and authorization code flow. This is the recommended flow for native applications that want to retrieve access tokens (and possibly refresh tokens as well) and is used for server-side web applications and native desktop/mobile applications.

#### Refresh Token
Refresh tokens allow requesting new access tokens without user interaction. Every time the client refreshes a token it needs to make an (authenticated) back-channel call to IdentityServer. This allows checking if the refresh token is still valid, or has been revoked in the meantime.

Refresh tokens are supported in hybrid, authorization code and resource owner password flows. To request a refresh token, the client needs to include the **offline_access** scope in the token request (and must be authorized to for that scope).

#### Extension Grant
Extension grants allow extending the token endpoint with new grant types.

### From Auth 1.0 to Auth 2.0
In ASP .NET Core 2.0, [the old 1.0 Authentication stack no longer will work, and is obsolete in 2.0](https://github.com/aspnet/Announcements/issues/262). All authentication related functionality must be migrated to the 2.0 stack. BuilderExtensions.UseIdentity(IApplicationBuilder)' is obsolete and will be removed in a future version. The recommended alternative is UseAuthentication().

Hence, the line of code `app.UseIdentity();` is no longer needed. Instead, we will just have `app.UseAuthentication();` as shown in the code above.

## Consent Page
During an authorization request, if IdentityServer requires user consent the browser will be redirected to the consent page.

The codes can be found at the following places in the project.
- [Controllers/Consent.cs](https://github.com/goh-chunlin/GclProjectIdentityServer/blob/master/Controllers/ConsentController.cs);
- [Models/ConsentPageViewModels/](https://github.com/goh-chunlin/GclProjectIdentityServer/tree/master/Models/ConsentPageViewModels);
- [Views/Consent/](https://github.com/goh-chunlin/GclProjectIdentityServer/tree/master/Views/Consent);
- [Views/Shared/_ScopeListItem](https://github.com/goh-chunlin/GclProjectIdentityServer/blob/master/Views/Shared/_ScopeListItem.cshtml).

![Consent Page](github-images/consent-page.png?raw=true)

## References
- [Stack Overflow - IdentityServer Architecture Overview](https://stackoverflow.com/a/39560625/1177328)
- [Sample - Identity Server with Asp .Net Identity](https://github.com/IdentityServer/IdentityServer4.Samples/tree/release/Quickstarts/6_AspNetIdentity/src/IdentityServerWithAspNetIdentity)
- [Identity Server 4 Using ASP.NET Core Identity](http://docs.identityserver.io/en/release/quickstarts/6_aspnet_identity.html)
- [Identity Server 4 Setup and Overview](http://docs.identityserver.io/en/release/quickstarts/0_overview.html#modify-hosting)
- [ASP.NET Core Authentication with IdentityServer4](https://blogs.msdn.microsoft.com/webdev/2017/01/23/asp-net-core-authentication-with-identityserver4/)
- [Stack Overflow - Specify the Port an ASP.NET Core Application](https://stackoverflow.com/a/37365382/1177328)
- ['IIdentityServerBuilder' does not Contain a Definition for 'AddAspNetIdentity'](https://github.com/IdentityServer/IdentityServer4/issues/888)
- [Remove AddTemporarySigningCredential in 2.0](https://github.com/IdentityServer/IdentityServer4/issues/1139)
- [IdentityServer4.Demo - Config.cs](https://github.com/IdentityServer/IdentityServer4.Demo/blob/master/src/IdentityServer4Demo/Config.cs)
- [Draft - Auth 2.0 Migration Announcement](https://github.com/aspnet/Security/issues/1310)
- [Feedback Voice - Using Identity Server in MVC WITH ASP .NET Core 2.0](https://github.com/IdentityServer/IdentityServer4.Samples/issues/193)
- [InvalidOperationException when Trying to Do SignInAsync](https://github.com/aspnet/Security/issues/1126)
- [Identity Server 4 - AssertRequiredClaims](https://github.com/IdentityServer/IdentityServer4/blob/dev/src/IdentityServer4/IdentityServerPrincipal.cs)
- [Adding User Authentication with OpenID Connect](http://docs.identityserver.io/en/release/quickstarts/3_interactive_login.html)
- [Auth 2.0 Changes / Migration](https://github.com/aspnet/Announcements/issues/262)
- [Sometimes get a "idp claim is missing" with AspNetIdentity when authorizing](https://github.com/IdentityServer/IdentityServer4/issues/277)
- [When You Sign the User in You must Issue at Least a Sub Claim and a Name Claim](http://docs.identityserver.io/en/release/topics/signin.html)
- [InvalidOperationException: No IAuthenticationSignInHandler is Configured to Handle Sign in for the Scheme: MyCookieAuthenticationScheme](https://stackoverflow.com/questions/45776374/invalidoperationexception-no-iauthenticationsigninhandler-is-configured-to-hand)
- [ASP.NET Core 2.0 Authentication and Authorization System Demystified](https://digitalmccullough.com/posts/aspnetcore-auth-system-demystified.html)
- [OpenID Connect for User Authentication In ASP.NET Core](https://stormpath.com/blog/openid-connect-user-authentication-in-asp-net-core)
- [Migrating Authentication and Identity to ASP.NET Core 2.0](https://docs.microsoft.com/en-us/aspnet/core/migration/1x-to-2x/identity-2x)
- [Identity Server 4: adding claims to access token](https://stackoverflow.com/questions/41387069/identity-server-4-adding-claims-to-access-token)
- [Qeo Native Documentation : Implicit Grant](http://www.qeo.org/Doc/Implicit-Grant_21676147.html)
- [Which OAuth 2.0 Grant should I Implement?](http://oauth2.thephpleague.com/authorization-server/which-grant/)
- [Authorize access to Web Applications using OAuth 2.0 and Azure Active Directory](https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-protocols-oauth-code)
- [Getting Started with IdentityServer 4](https://www.scottbrady91.com/Identity-Server/Getting-Started-with-IdentityServer-4)
- [Consent](https://identityserver4.readthedocs.io/en/release/topics/consent.html)
