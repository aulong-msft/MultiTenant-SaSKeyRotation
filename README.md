# SaS Key Rotation Strategy
The Azure infrastructure today contains many services that need to connect in a multi-tenated fashion, some of these servives are challanging for Azure AD to provide a service principal (which eliminates the need for multi-tenant Key rotation) thus a SAS token is utilized to gain access into the Azure cloud services. SAS keys must be rotated to maintain the highest integrity of cloud data and ensuring those keys are safegaurded in transit is also crucial for the system's confidentiality.

This PoC contains a sample solution for handling cross-tenant SaS key rotation for a Service Bus between a Provider and one or more of its Customers. This scenario can be adaptend into solutions that are using services which needs a key rotation strategy in any multi-tenanted capacity. For simplicity, this solution has been boiled down to a few key componants but can be adapted to fit your solution's needs.

![PoC Infrastructure](https://github.com/aulong-msft/MultiTenant-SaSKeyRotation/blob/main/Docs/keyrotation.png)

The structure of this repository can read as follows: the proivder kicks off a powershell script that rotates the service bus keys then kicks off an "update keys" message into the service bus. Then another function in the provider consumes that message and grabs the keys from the service and stores the new information into a secret message than then gets encrypted by an AES 256 bit key (with a 128 bit IV) and placed into another queue. A client function then gets triggered from the new message on the service bus, retreives the new key information, decrypts the payload, and stores the values into the client side key vault. A client acknowledgement message is then sent backt to the provider to report back on the status.


## Provider Setup

### Azure Function - HTTP Triggered
This HTTP function is used to start off the deployment from the Provider's tenant by sending a message into the customer's Service Bus deployment queue. (This HTTP triggered function was the chosen method of delivery to kick off this proof-of-concept.) The Service Prinicpal generated earlier will be used as the credential to access the Customer tenant and write to a specific queue within the service bus. 
*Note - Customer Setup also needs to be completed for this to work properly*

### Azure Function - Service Bus Trigger
Generate an Azure Function from a Service Bus queue trigger from the following documentation was followed to define the function trigger from the service bus. https://docs.microsoft.com/en-us/azure/azure-functions/functions-identity-based-connections-tutorial-2 This documentation also showcases how to setup a managed idntity used for the azure function to get triggered from the service bus when a message has been added to the queue and we will continue to use that managed identity when putting a message into a different queue (for demo purposes, the same function was used to push the message through)
In your service bus namespace that you just created, select Access Control (IAM). This is where you can view and configure who has access to the resource.

#### Grant the Function App Access to the Service Bus Namespace using Managed Identities
*Note these directions were taken from the doc listed above*
1. CLick on "Access Control (IAM)"
1. Click "Add" and select "Add role assignment".
1. Search for "Azure Service Bus Data Receiver", select it, and click "Next".
1. On the" Members" tab, under Assign access to, choose Managed Identity
1. Click "Select" members to open the Select managed identities panel.
Confirm that the Subscription is the one in which you created the resources earlier.
1. In the Managed identity selector, choose "Function App" from the "System-assigned managed identity" category. The label "Function App" may have a number in parentheses next to it, indicating the number of apps in the subscription with system-assigned identities.
Your app should appear in a list below the input fields. If you don't see it, you can use the Select box to filter the results with your app's name.
1. Click on your application. It should move down into the Selected members section. Click "Select".
1. Back on the Add role assignment screen, click "Review + assign". Review the configuration, and then click "Review + assign".

#### Grant the Function App Access to the Key Vault using Managed Identities
1. CLick on "Access Control (IAM)"
1. Click "Add" and select "Add role assignment".
1. Search for "Key Vault Administrator", select it, and click "Next".
1. On the" Members" tab, under Assign access to, choose Managed Identity
1. Click "Select" members to open the Select managed identities panel.
Confirm that the Subscription is the one in which you created the resources earlier.
1. In the Managed identity selector, choose "Function App" from the "System-assigned managed identity" category. The label "Function App" may have a number in parentheses next to it, indicating the number of apps in the subscription with system-assigned identities.
Your app should appear in a list below the input fields. If you don't see it, you can use the Select box to filter the results with your app's name.
1. Click on your application. It should move down into the Selected members section. Click "Select".
1. Back on the Add role assignment screen, click "Review + assign". Review the configuration, and then click "Review + assign".

### Set up RBAC for the System Assigned Managed Identities
Scope the System Assigned Managed Identity to have "Service Bus Data Owner" roles on the Service Bus. This Managed Identity will be used in both writing to a queue with an HTTP triggered function, as well as reading from a queue from a timer triggered function* 
1. Naviate to the Customer Service Bus namespace
1. Click on "Access Control (IAM)
1. Click on "Role Assignments" and "Add" at the top and "Add Role Assignment" 
1. Select "Azure Service Bus Data Owner" and hit "Next"
1. Ensure "User, group, or Service Principal" is selected and click "+Select members" in the "Select" field type in the Service Principal name and select the system assigned managed identity.
1. Select "Review + assign"

Scope the System Assigned Managed Identity to have "Key Vault Administrator" roles on the Service Bus. This Managed Identity will be used in both writing qne reading from a keyvault with a timer triggered function* 
1. Naviate to the Customer Service Bus namespace
1. Click on "Access Control (IAM)
1. Click on "Role Assignments" and "Add" at the top and "Add Role Assignment" 
1. Select "Key Vault Administrator" and hit "Next"
1. Ensure "User, group, or Service Principal" is selected and click "+Select members" in the "Select" field type in the Service Principal name and select the system assigned managed identity.
1. Select "Review + assign"

## Customer Setup
1. Create an Service Bus triggered function to read a message off a Service Bus message queue and place a message into anoter queue. (for demo purposes this flow as optimal to test out the functionality)
*Note! must do an az login -t CUSTOMER TENANT ID before deploying locally, this will mitigate against invalid token issuer error messages*

#### Grant the Function App Access to the Key Vault using Managed Identities
1. CLick on "Access Control (IAM)"
1. Click "Add" and select "Add role assignment".
1. Search for "Key Vault Administrator", select it, and click "Next".
1. On the" Members" tab, under Assign access to, choose Managed Identity
1. Click "Select" members to open the Select managed identities panel.
Confirm that the Subscription is the one in which you created the resources earlier.
1. In the Managed identity selector, choose "Function App" from the "System-assigned managed identity" category. The label "Function App" may have a number in parentheses next to it, indicating the number of apps in the subscription with system-assigned identities.
Your app should appear in a list below the input fields. If you don't see it, you can use the Select box to filter the results with your app's name.
1. Click on your application. It should move down into the Selected members section. Click "Select".
1. Back on the Add role assignment screen, click "Review + assign". Review the configuration, and then click "Review + assign".

### Set up RBAC for the System Assigned Managed Identities
Scope the System Assigned Managed Identity to have "Key Vault Administrator" roles on the Service Bus. This Managed Identity will be used in both writing qne reading from a keyvault with a timer triggered function* 
1. Naviate to the Customer Service Bus namespace
1. Click on "Access Control (IAM)
1. Click on "Role Assignments" and "Add" at the top and "Add Role Assignment" 
1. Select "Key Vault Administrator" and hit "Next"
1. Ensure "User, group, or Service Principal" is selected and click "+Select members" in the "Select" field type in the Service Principal name and select the system assigned managed identity.
1. Select "Review + assign"

#### Connect to Service Bus in your function app
*Note these directions were taken from the doc listed above*
1. In the portal, search for the your function app , or browse to it in the Function App page.
1. In your function app, select "Configuration" under Settings.
1. In Application settings, select "+ New" application setting to create the new setting in the following table. "ServiceBusConnection__fullyQualifiedNamespace	<SERVICE_BUS_NAMESPACE>.servicebus.windows.net" 
1.After you create the two settings, select "Save > Confirm".

## Local Settings
Each subdirectory contains a stubbed version of the local.settings.json files which can be modified to run the Azure functions locally. To configure settings in Azure, update the Application Settings.
  
*Note! must do an az login -t CUSTOMER TENANT ID before deploying locally, this will mitigate against invalid token issuer error messages*

## Team Moon Raccoon

```
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣠⡄⠀⠀
⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣠⣾⣿⣿⠀⠀⠀
⠀⠘⣿⣶⡦⠄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣠⠄⠀⠀⠀⠀⠛⣿⠇⠀⠀⠀
⠀⠀⠈⠻⠁⠀⠀⠀⢀⠄⠀⠀⠠⡆⣠⠾⠛⠟⠛⠒⠒⠤⢀⠀⠀⠁⠀⠀⠀⠀
⠀⠀⠀⠀⠀⠀⠐⠒⠉⠀⣤⠀⠀⠉⠁⢀⣠⠆⣀⣀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
⠀⠀⠀⠀⣠⣶⣦⣄⠀⠀⣿⠀⠀⢀⣴⣿⣿⣿⣿⣿⣿⣷⣦⣄⠀⠀⠀⠀⠀⠀
⠀⠀⠀⢀⣿⣿⡉⠻⣷⣄⣿⣦⣴⣿⣿⡉⠁⣀⣤⣼⣿⣿⣿⣿⣷⣄⡀⠀⠀⠀
⠀⠀⢀⣾⣿⣿⣿⣿⣿⣿⣿⣿⠿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣦⡀⠀
⠀⢴⣿⣿⣿⣿⣿⠁⠈⠙⠟⠁⠀⠉⠻⢿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⠿⠿⠿⠂
⢴⣿⣿⣿⣿⣿⠇⠀⠀⠀⠀⠀⠀⠀⠀⢰⣿⣿⣿⣿⣿⣿⣿⣿⣿⡿⠿⠒⠀⠀
⠀⠙⠻⢿⣿⡿⠂⢀⣀⣀⣀⠀⠀⠀⠀⠀⣼⣿⣿⣿⠿⠿⠟⠋⠁⠀⠀⠀⠀⠀
⠀⠀⠀⠈⠁⠀⠀⢸⣿⣿⣿⡇⠀⠀⠀⠀⠀⠈⠉⠉⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀
⠀⠀⠀⠀⠀⠀⠀⠀⠙⠛⠛⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
```
