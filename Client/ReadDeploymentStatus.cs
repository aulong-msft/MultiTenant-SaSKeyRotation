using System;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus.Administration;

namespace ProviderFunctions
{
    public class ReadDeploymentStatus
    {
        // A function for the Provider to try reading from the Customer's deployment status
        [FunctionName("ReadDeploymentStatus")]

        public void Run([ServiceBusTrigger("%CommandQueueName%", 
            Connection = "CommandQueueSaSConnectionStringPk")]string command, ILogger log)
        {
            var responseQueue = Environment.GetEnvironmentVariable("ReponseQueueName");
            var responseQueueConnectionString  = Environment.GetEnvironmentVariable("ResponseQueueSaSConnectionStringPk");
            var client = new ServiceBusClient(responseQueueConnectionString);

            log.LogInformation($"ReadDeploymentStatus triggered with command: {command}");

            var sender = client.CreateSender(responseQueue);
            var message = new ServiceBusMessage(command);
            sender.SendMessageAsync(message).GetAwaiter().GetResult();

        }
    }
}
