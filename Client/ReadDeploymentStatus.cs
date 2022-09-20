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
            //0.) Investigate function restarting
            //1.) Create system assigned managed identity to access the KV with AzureDefaultCredential
            var clientKV = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());

            //2.) Decrypt the message received from the command queue
            //3.) Store the new values in the KV

            // Retrieve secrets using the secret client
            KeyVaultSecret CommandQueuePK = clientKV.GetSecret("CommandQueuePK");
            KeyVaultSecret ReponseQueuePK = clientKV.GetSecret("ReponseQueuePK");
            
            Console.WriteLine(ReponseQueuePK.Name);
            Console.WriteLine(ReponseQueuePK.Value);

            // Update Secrets using thr secret client
            var secretNewValue1 = new KeyVaultSecret("CommandQueuePK", "bhjd4DDgsa");
            secretNewValue1.Properties.ExpiresOn = DateTimeOffset.Now.AddYears(1);
            clientKV.SetSecret(secretNewValue1);

            var secretNewValue2 = new KeyVaultSecret("ReponseQueuePK", "bhjd4DDgsa");
            secretNewValue2.Properties.ExpiresOn = DateTimeOffset.Now.AddYears(1);
            clientKV.SetSecret(secretNewValue2);

            //4.) Send an ACK message back to the response queuue
            var client = new ServiceBusClient(ReponseQueuePK.Value);
            log.LogInformation($"ReadDeploymentStatus triggered with command: {command}");

            var sender = client.CreateSender(responseQueue);
            var message = new ServiceBusMessage(command);
            sender.SendMessageAsync(message).GetAwaiter().GetResult();

        }
    }
}
