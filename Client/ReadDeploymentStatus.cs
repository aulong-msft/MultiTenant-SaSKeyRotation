using System;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus.Administration;
using Azure.Security.KeyVault.Secrets;

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
            var keyVaultUrl = Environment.GetEnvironmentVariable("KeyVaultName");
            
            //KV code 
            //1.) create system assigned managed identity to access the KV
            //2.) create secret in KV with primary key from SB
            //3.) create a secret client to grab out the secret
            //4.) todo figure out how (in code) to reroll the secret and place it back into the KV
            
            var clientKV = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
            // Create a new secret using the secret client.
            KeyVaultSecret CommandQueuePK = clientKV.GetSecret("CommandQueuePK");
            KeyVaultSecret ReponseQueuePK = clientKV.GetSecret("ReponseQueuePK");

            log.LogInformation($"KeyVaultSecret triggered");

            // Retrieve a secret using the secret client.
            Console.WriteLine(ReponseQueuePK.Name);
            Console.WriteLine(ReponseQueuePK.Value);

            //service bus to read the command queue with a SAS key
            var client = new ServiceBusClient(ReponseQueuePK.Value);

            log.LogInformation($"ReadDeploymentStatus triggered with command: {command}");

            var sender = client.CreateSender(responseQueue);
            var message = new ServiceBusMessage(command);
            sender.SendMessageAsync(message).GetAwaiter().GetResult();

        }
    }
}
