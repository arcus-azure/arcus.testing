# Contributing to Arcus Testing
> 🎉 First off, **thank you** for taking the time to contribute! </br>
> We're really glad you're reading this.

This guide helps newcomers to contribute effectively to Arcus Testing.

* **🐞 I found a bug!** </br>
Great Scott! Please [report the problem](https://github.com/arcus-azure/arcus.testing/issues/new/choose) so we can have a look, or if you're up to it: you can always fix it yourself and [submit a PR](#submitting-a-pr).

* **❔ I have a question or an idea** </br>
[GitHub discussions](https://github.com/arcus-azure/arcus.testing/discussions/new/choose) is the place to host discussions, questions or ideas related to anything available in the codebase. Ideas can always be transformed in workable [GitHub issues](https://github.com/arcus-azure/arcus.testing/issues) once the team decides to place this on the roadmap.

* **👷‍♀️ I want to work** </br>
Have a look at our [good first issues](https://github.com/arcus-azure/arcus.testing/issues?q=is%3Aissue%20state%3Aopen%20label%3A%22good%20first%20issue%22) to get you started on something easy. Please comment on the issue, so we can assign you to it.

## Submitting a PR
When you submit a new pull request, please make sure that:
* 📦 it **packages everything** related to the feature/bug
* 🧪 it **proofs using tests** the feature/bugfix (both unit & integration tests are available to run locally)
* 🌐 it **updates the documentation**, if necessary (changes should happen in the `/docs/preview/` folder)

## FAQ
<details>
<summary>How to run the integration tests locally?</summary>
The integration tests make use of real Azure resources, which means that the test suite needs to be aware of which resources you want to use locally.

> 👉 If you're a [Codit](http://codit.eu/) employee, we can also provide you with a ready-to-use `appsettings.local.json` that allows for you to run the tests locally.

1. Make sure you have an **active Azure subscription**.

2. Set up a **valid managed identity connection**: </br>
   The tests uses [`DefaultAzureCredential`](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential) to authenticate themselves, which means that you can use your logged-in VisualStudio/AzureCLI user to run the tests locally.

3. Add the necessary **Azure resources** for your test: </br>
   There exists a [`/build/templates/test-resources.bicep`](https://github.com/arcus-azure/arcus.testing/blob/main/build/templates/test-resources.bicep) file that let's you deploy all the necessary Azure resources that are required to run *all* the tests.
    * 💡 Usually, you don't need to run *all* the tests locally. Arcus Testing is very flexible and modular. If you're working on something, like **Azure Blob Storage**-related, than you only need an **Azure Storage Account**.
    
    * ⚠️ Make sure that you have enough rights on your Azure resources to do CRUD operations (ex. the **Azure Blob Storage** tests require [`Storage Blob Data Contributor`](https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles/storage#storage-blob-data-contributor)-rights.)

4. Add an `appsettings.local.json` file to your integration test project - this file gets ignored by Git: </br>
   The local file needs to have the names of your **Azure resources**. If you deployed an **Azure Storage Account**, then you the file will look like this:
   ```json
   {
     "Arcus": {
        "StorageAccount": {
            "Name": "mystorageaccount"
        }
     }
   }
   ```
   🚀 Don't worry, if a value is missing, the test will fail and point you to the values you require to run the test.

5. You can now run the tests locally! 🎉
</details>
