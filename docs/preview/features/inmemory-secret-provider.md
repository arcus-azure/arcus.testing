---
title: Security
layout: default
---

# Security

## Installation

The following functionality is available when installing this package:

```shell
PM> Install-Package -Name Arcus.Testing.Security.Providers.InMemory
```

## In-memory secret provider

As an addition to the [Arcus Security](https://github.com/arcus-azure/arcus.security) package, we have added an in-memory `ISecretProvider` implementation. 
This secret provider is created so you can test you secret store configuration with non-secret values in a easy manner, without implementing your own `ISecretProvider`.

⚡ Supports [synchronous secret retrieval](https://security.arcus-azure.net/Features/secrets/general).

After installing the package, the `.AddInMemory` extension should be available to you:

```csharp
using Microsoft.Extensions.Hosting;

public void Program
{
    public static void Main(string[] args)
    {
        Host.CreateDefaultBuilder()
            .ConfigureSecretStore((config, stores) =>
            {
                // Adding a in-memory secret provider to the secret store, without any additional secrets.
                // This is mainly used to have at least a single secret provider registration which is required for the secret store to be set up.
                stores.AddInMemory();

                // Adding a in-memory secret provider to the secret store, with a single secret name/value pair.
                stores.AddInMemory("MySecret", "P@ssw0rd");

                // Adding a in-memory secret provider to the secret store, with several secret name/value pairs.
                stores.AddInMemory(new Dictionary<string, string>
                {
                    ["MySecret-1"] = "P@ssw0rd",
                    ["MySecret-2"] = "qwerty"
                });
            })
            .Build()
            .Run();
    }
}
```

The secret store will behave the same, so this in-memory secret provider will be a part when you inject the `ISecretProvider` in your application:

```csharp
using Arcus.Security.Core;

[ApiController]
public class MyController : ControllerBase
{
    public MyController(ISecretProvider secretProvider)
    {
        secretProvider.GetRawSecretAsync("MySecret");
    }
}
```

### Customization

The in-memory secret provider also has some several extra options to customize the usage.

```csharp
using Microsoft.Extenions.Hosting;

public void Program
{
    public static void Main(string[] args)
    {
        Host.CreateDefaultBuilder()
            .ConfigureSecretStore((config, stores) =>
            {
                // Adding a in-memory secret provider to the secret store, with caching configuration.
                // This means that the secret provider will be registered as a cached variant and can be retrieved as such (via `ISecretStore.GetCachedProvider`).
                // For more information on caching secrets: https://security.arcus-azure.net/features/secrets/general
                stores.AddInMemory("MySecret", "P@ssw0rd", new CacheConfiguration(TimeSpan.FromSeconds(5));

                // Adding a in-memory secret provider to the secret store, with a dedicated name.
                // This means that the secret provider can be retrieved with the `ISecretStore.GetProvider("your-name")`.
                // For more information on retrieving a specific secret provider: https://security.arcus-azure.net/features/secret-store/named-secret-providers
                stores.AddInMemory(new Dictionary<string, string> { ["MySecret"] = "P@ssw0rd" }, secretProviderName: "InMemory");
            })
            .Build()
            .Run();
    }
}
```
