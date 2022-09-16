using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Wtw.SecretRotationPOC.ServiceBus;

// Pre-function work
// This functions purpose is to kick off the sas key update and act
// as an update app message onto the service bus queue

namespace ProviderFunctions
{
    public static class SendDeploymentCommand
    {
        [FunctionName("SendDeploymentCommand")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            ServiceBusService sbService = new(log);
            log.LogInformation("SendDeploymentCommand endpoint called.");

            string customerId = req.Query["customerId"];

            if (string.IsNullOrEmpty(customerId))
            {
                log.LogInformation("SendDeploymentCommand endpoint called with no customerId.");
                return new BadRequestObjectResult(nameof(customerId));
            }

            log.LogInformation($"SendDeploymentCommand endpoint called with customerId = {customerId}");
           
           // retrieve connection strings from Srv Bus for queues
           var rootString = Environment.GetEnvironmentVariable("RootManageSharedAccessKeyConnectionString");
           var commandQName = Environment.GetEnvironmentVariable("CommandQueueName");
           var responseQName = Environment.GetEnvironmentVariable("ResponseQueueName");
           var cmdSaPolicyName = Environment.GetEnvironmentVariable("CommandQueueAuthPolName");
           var respSaPolicyName = Environment.GetEnvironmentVariable("ResponseQueueAuthPolName");

           DateTime now = DateTime.Now;
           TimeSpan span = now.AddYears(1) - now;
           
           var cmdConnectionStr = await sbService.GetSASTokenConnectionString(rootString, commandQName, cmdSaPolicyName, span);
           var respConnectionStr = await sbService.GetSASTokenConnectionString(rootString, responseQName, respSaPolicyName, span);

            var credential = new DefaultAzureCredential();

            var serviceBusNamespace = Environment.GetEnvironmentVariable("ServiceBusConnection__fullyQualifiedNamespace");
            var initiatedDeploymentQueueName = Environment.GetEnvironmentVariable("InitiatedDeploymentQueueName");
           

            var deploymentCommand = $"{cmdConnectionStr}";
            var client = new ServiceBusClient(serviceBusNamespace, credential);

            var sender = client.CreateSender(initiatedDeploymentQueueName);
            using var messageBatch = await sender.CreateMessageBatchAsync();
            messageBatch.TryAddMessage(new ServiceBusMessage(deploymentCommand));

            try
            {
                // Use the producer client to send the batch of messages to the Service Bus queue
                await sender.SendMessagesAsync(messageBatch);
            }
            finally
            {
                await sender.DisposeAsync();
                await client.DisposeAsync();
            }            

            return new OkObjectResult(deploymentCommand);
        }

    }
}
