# Arcus - Testing
[![Build Status](https://dev.azure.com/codit/Arcus/_apis/build/status/Commit%20builds/CI%20-%20Arcus.Testing?branchName=master)](https://dev.azure.com/codit/Arcus/_build/latest?definitionId=804&branchName=master)
[![NuGet Badge](https://buildstats.info/nuget/Arcus.Testing.Logging?includePreReleases=true)](https://www.nuget.org/packages/Arcus.Testing.Logging/)

Reusable testing components for Arcus repo's.

![Arcus](https://raw.githubusercontent.com/arcus-azure/arcus/master/media/arcus.png)

# Installation
Easy to install it via NuGet:

- **Logging**
```shell
PM > Install-Package Arcus.Testing.Logging
```

- **In-memory secret store**
```shell
PM > Install-Package Arcus.Testing.Security.Providers.InMemory
```

For a more thorough overview, we recommend reading our [documentation](/docs/index.md).

# Features

* [Logging](/docs/features/logging.md): provides reusable logging components during testing.
* [In-memory secret provider](/docs/features/inmemory-secret-provider.md): provides an secret provider with in-memory secrets during testing. 

# License Information
This is licensed under The MIT License (MIT). Which means that you can use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the web application. But you always need to state that Codit is the original author of this web application.

Read the full license [here](https://github.com/arcus-azure/arcus.testing/blob/master/LICENSE).
