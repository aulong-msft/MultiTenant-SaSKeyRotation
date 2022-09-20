using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;


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

            log.LogInformation("SendDeploymentCommand endpoint called.");

            string customerId = req.Query["customerId"];

            if (string.IsNullOrEmpty(customerId))
            {
                log.LogInformation("SendDeploymentCommand endpoint called with no customerId.");
                return new BadRequestObjectResult(nameof(customerId));
            }

            log.LogInformation($"SendDeploymentCommand endpoint called with customerId = {customerId}");

            var credential = new DefaultAzureCredential();

            var serviceBusNamespace = Environment.GetEnvironmentVariable("ServiceBusConnection__fullyQualifiedNamespace");
            var initiatedDeploymentQueueName = Environment.GetEnvironmentVariable("InitiatedDeploymentQueueName");
             string deploymentCommand = $"{customerId}";
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
