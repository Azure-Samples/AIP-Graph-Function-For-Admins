---
page_type: sample
languages:
- csharp
products:
- dotnet
description: "This sample solution helps security and/or compliance administrators to track access to protected files on behalf of another user."
urlFragment: "AIP-Graph-Function-For-Admins"
---

# AIP Tracking Function for Security and Compliance Professionals

<!-- 
Guidelines on README format: https://review.docs.microsoft.com/help/onboard/admin/samples/concepts/readme-template?branch=master

Guidance on onboarding samples to docs.microsoft.com/samples: https://review.docs.microsoft.com/help/onboard/admin/samples/process/onboarding?branch=master

Taxonomies for products and languages: https://review.docs.microsoft.com/new-hope/information-architecture/metadata/taxonomies?branch=master
-->

Azure Information Protection Custom Tracking Solution for Administrators.

## Summary
This repo contains sample code demonstrating how you can build a custom solution that helps security and/or compliance administrators to track access to AIP protected files on behalf of another user. The sample solution uses PowerShell to display a list of recently protected documents for a particular user provided by the amdinistrator. The solution queries AIP Log Analytics via an Azure Function using REST API to display the results.

Why should you build something like this? For starters, you have more flexibility and can restrict the AIP data each administrator can see based on his/her country or region. Additionally, you could combine the data provided by AIP with other datasets within your organization to enrich the context of the information displayed to security professionals.

Please read our blog [for more details on this and the prior user focused solution](https://techcommunity.microsoft.com/t5/Azure-Information-Protection/How-to-Restrict-Access-to-AIP-Audit-Logs-to-a-Single-Country-or/ba-p/898424 "Restrict Admin Access to AIP Logs")

## Prerequisites

While the solution is quite simple, some assembly is required.

•	Visual Studio 2017 or higher  
•	An Azure subscription with a Log Analytics Workspace created  
•	Azure Information Protection (AIP) with Log Analytics integration configured   
•	Either Classic or Unified Labeling client installed on a supported version of Windows (7 or above as of today)   
•	One Azure Function   
•	An Azure AD application (Service Principal)  
•	Optional: Azure Key Vault  


## Setup

## Clone the Repository
1. Open a command prompt  
2. Create a new folder mkdir c:\samples  
3. Navigate to the new folder using cd c:\samples  
4. Clone the repository by running git clone https://github.com/Azure-Samples/AIP-Graph-Function-For-Admins  
5. In explorer, navigate to c:\samples\AIP-Graph-Function-For-Admins and open the AIP-Graph-Function-For-Admins.sln in Visual Studio 2017 or later.  

## Add the NuGet Package
In Visual Studio, right click the _AIP-Graph-Function-For-Admins_ solution.  
Click **Restore NuGet Packages**

## Authentication
This sample solution uses a single application (service principal) that you must register in Azure AD. Note that this service pricipal requires **Data.Reader** rights in your Log Analytics Workspace as explained on the blog above.  
[Follow these instructions to register an application in Azure Active Directory](https://dev.loganalytics.io/oms/documentation/1-Tutorials/1-Direct-API "Register Azure AD app")

## Azure Functions keys
To view your keys, create new ones, navigate to one of your HTTP-triggered functions in the [Azure portal]( https://portal.azure.com "Azure Portal") and select **Manage**.  
Functions lets you use keys to make it harder to access your HTTP function endpoints during development. A standard HTTP trigger may require such an API key be present in the request. Most HTTP trigger templates require an API key in the request. 
 
 ## Important
While keys may help obfuscate your HTTP endpoints during development, they are not intended to secure an HTTP trigger in production. To learn more, see [Secure an HTTP endpoint in production](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook#secure-an-http-endpoint-in-production "Secure HTTP endpoint in production").

## Setup/Configure Azure Key Vault
Although Azure Key Vault is an optional component, we highly recommend it. As a bonus, it’s already wired up on both Azure Functions.    

**NOTE** Make sure you follow the [managed identities]( https://docs.microsoft.com/en-us/azure/app-service/overview-managed-identity#creating-an-app-with-an-identity "Tenant ID") instructions as well if you decide to use Key Vault.

Please follow Jeff’s excellent walkthrough on how to setup Key Vault: [Configure Azure Key Vault]( https://medium.com/statuscode/getting-key-vault-secrets-in-azure-functions-37620fd20a0b "Tenant ID")

## Update appSettings
For production deployment you may want to use the Azure Key Vault implementation to make sure your keys/secrets are properly protected. For testing however, you can just assign hardcoded values to the variables below within the Function1 Run method.

| Key       | Value                                |
|-------------------|--------------------------------------------|
| `workspaceId`             | Log Analytics Worskpace ID from the [Log Analytics workspaces blade]( https://portal.azure.com/#blade/HubsExtension/BrowseResourceBlade/resourceType/Microsoft.OperationalInsights%2Fworkspaces "Workspace Id") |
| `clientId`      | From the [AAD Registered Apps blade]( https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredApps "Client Id")      |
| `clientSecret`    | From the [AAD Registered Apps blade]( https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredApps "Client Secret")            |
| `tenantId`      | From the [AAD Properties Blade]( https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/Properties "Tenant ID")  |
| `domain`    | Domain of AAD Tenant - e.g. Contoso.Onmicrosoft.com      |

## Publish your Function to Azure
You can just right click on the _AIP-Graph-Function-For-Admins_ solution in Visual Studio and click Publish.  
Please follow these instructions on [how to publish an Azure Function to Azure using Visual Studio]( https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-your-first-function-visual-studio "Publish First Function to Azure"). 

Once you've deployed the Azure Function into Azure, natigate to the **.\samples\AIP-Graph-Function-For-Admins** folder you cloned earlier and right click this file: **AdminBlogCode.ps1**.  
Select to open with PowerShell ISE and edit the following values:  

| Variable       | Value                                |
|-------------------|--------------------------------------------|
| `$AzSubscriptionId` | Azure Subscription ID from the [Azure Subscriptions blade]( https://portal.azure.com/#blade/Microsoft_Azure_Billing/SubscriptionsBlade "Subscription blade") |  
| `$userId`      | User UPN - e.g. _username@domain.com_     |  
| `$AppName`    | Obtain the Function app name from its URL in the Functions Blade. e.g. https://**<APP_NAME>**.azurewebsites.net    |  
| `$FunctionName`    | Function name hosting the Run method. e.g. _Function1_ which is the default name    |  
| `$FunctionKey`      | From your Azure Function. See **Obtaining keys** from the [this link]( https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook "Azure Function Key")      |  
| `$adminEmail`    | Security or compliance professional email - e.g. _username@domain.com_     |  

Finally, we want to hear from you. Please contribute and let us know what other use cases you come up with.

## Sources/Attribution/License/3rd Party Code
Unless otherwise noted, all content is licensed under MIT license.  
JSON de/serialization provided by [Json.NET](https://www.nuget.org/packages/Newtonsoft.Json/)  

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
