using System;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Wtw.SecretRotationPOC.ServiceBus;
namespace CustomerFunctions;

// Pre-function work
// 1) create a managed identity in the Customer subscription to interface with the service bus
// 2) wire the managed identity in the IAM service bus blade and give it the "Service Bus Data Owner" permissions
public class ReadDeploymentCommandAndSendDeploymentStatus
{
    // This customer-side service bus trigger connects to the customer's
    // service bus with a managed identity to read the contents.
    [FunctionName("ReadDeploymentCommandAndSendDeploymentStatus")]

    // ServiceBusConnection resolves to ServiceBusConnection__fullyQualifiedNamespace
    // which is referenced within the local.settings.json file to use the managed identity
    public static void Run([ServiceBusTrigger("%InitiatedDeploymentQueueName%",
        Connection = "ServiceBusConnection")]string command, ILogger log)
    {
        log.LogInformation($"ReadDeploymentCommandAndSendDeploymentStatus triggered with command: {command}");
        ServiceBusService sbService = new(log);
        DateTime now = DateTime.Now;
        TimeSpan span = now.AddYears(1) - now;

        // retrieve connection strings from Srv Bus for queues
        string rootString = Environment.GetEnvironmentVariable("RootManageSharedAccessKeyConnectionString");
        string commandQName = Environment.GetEnvironmentVariable("CommandQueueName");
        string responseQName = Environment.GetEnvironmentVariable("ResponseQueueName");
        string cmdSaPolicyName = Environment.GetEnvironmentVariable("CommandQueueAuthPolName");
        string respSaPolicyName = Environment.GetEnvironmentVariable("ResponseQueueAuthPolName");

        var cmdConnectionStr =  sbService.GetSASTokenConnectionString(rootString, commandQName, cmdSaPolicyName, span);
        var respConnectionStr =  sbService.GetSASTokenConnectionString(rootString, responseQName, respSaPolicyName, span);
        var credential = new DefaultAzureCredential();

        string serviceBusNamespace = Environment.GetEnvironmentVariable("ServiceBusConnection__fullyQualifiedNamespace");
        string commandQueue = Environment.GetEnvironmentVariable("CommandQueueName");
        var client = new ServiceBusClient(serviceBusNamespace, credential);
        string encryptedCommandMessage = command; // TODO encrypt the connection strings and what not

        var sender = client.CreateSender(commandQueue);
        var message = new ServiceBusMessage(encryptedCommandMessage);
        sender.SendMessageAsync(message).GetAwaiter().GetResult();

    }
}