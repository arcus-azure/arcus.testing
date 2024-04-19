---
title: 'Home'
layout: default
sidebar_label: 'Welcome'
slug: /
---

# Installation

Easy to install via NuGet:

- **Logging during tests**

```shell
PM > Install-Package Arcus.Testing.Logging
```

- **In-memory secret store**

```shell
PM > Install-Package Arcus.Testing.Security.Providers.InMemory
```

For more granular packages we recommend reading the documentation.

# Features

- [In-memory test secret provider the Arcus secret store](./02-Features/inmemory-secret-provider.md)
- [In-memory test Azure Service Bus message pump](./02-Features/servicebus-messsage-pump.md)
- [In-memory test Azure EventHubs message pump](./02-Features/eventhubs-messsage-pump.md)
- [xUnit Logging and Logging Testing](./02-Features/logging.md)

# License

This is licensed under The MIT License (MIT). Which means that you can use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the web application. But you always need to state that Codit is the original author of this web application.

_[Full license here](https://github.com/arcus-azure/arcus.testing/blob/master/LICENSE)_
