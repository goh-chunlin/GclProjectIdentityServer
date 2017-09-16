# GclProjectIdentityServer
This is an IdentityServer using ASP .NET Core Identity to access the resources in GCL Projects.

## Objective
This project is to develop and maintain Identity Server that will be used by many projects in GCL Projects which requires a Single Sign-On (SSO) solution. By doing this, user who logs in from an application in GCL Projects can also access the another resource in GCL Projects with the same credential.

![A simple architecture supporting a security token service (Source: https://stackoverflow.com/q/42959647/1177328)](https://i.stack.imgur.com/yF5kh.png)

Outsourcing the Authentication and Authorization functions to a security token service prevents duplicating that functionality across those applications and endpoints. Due to the fact that OpenID Connect is an extension on top of OAuth 2.0, the two fundamental security concerns, Authentication and API access, are combined into a single protocol - often with a single round trip to the security token service.

Hence, IdentityServer4, a middleware that adds the spec compliant OpenID Connect and OAuth 2.0 endpoints to an ASP.NET Core application, is chosen because it is an implementation of these two protocols to solve the typical security problems of modern mobile, native and web applications.

## Architecture Overview
IdentityServer uses a Security Token Service to provide token based protection to resources, such as web apps, mobile apps, and API calls.

In this project, we have
- A database of user Claims, based on ASP.Net Core Identity;
- A Security Token Service for authenticating the user, and providing a token containing the Claims, using IdentityServer.

![Clients, Scopes, and Claims (Source: https://stackoverflow.com/a/39560625/1177328)](https://i.stack.imgur.com/gxGob.png)

IdentityServer is designed for flexibility and part of that is allowing us to use any database we want. Since we are starting with a new user database, then ASP.NET Identity is what we have chosen in this project.

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

## Setup (Step 3): Configure ASP .NET Identity Core

## Setup (Step 4): Configure and Add IdentityServer to ASP .NET Core
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
