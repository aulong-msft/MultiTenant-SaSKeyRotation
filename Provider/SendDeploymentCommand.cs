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

// 1) create SaS key from the initiateDeploymentQueue (or namespace level?)
// 2) set up connetion string (?) for SaS key
// 3) send an "update" message to the initiateDeploymentQueue

namespace ProviderFunctions
{
    // This function is used to place a message from the provider's tenant
    // into a service bus in the customer's tenant by using a SP
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
            var deploymentCommand = $"Start deployment for {customerId}";

            var tenantId = Environment.GetEnvironmentVariable("CustomerTenantId");
            var clientId = Environment.GetEnvironmentVariable("ClientId");
            var clientSecret = Environment.GetEnvironmentVariable("ClientSecret");
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

            var serviceBusNamespace = Environment.GetEnvironmentVariable("ServiceBusNamespace");
            var deploymentCommandQueueName = Environment.GetEnvironmentVariable("CommandQueueName");
            var client = new ServiceBusClient(serviceBusNamespace, credential);

            var sender = client.CreateSender(deploymentCommandQueueName);
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
