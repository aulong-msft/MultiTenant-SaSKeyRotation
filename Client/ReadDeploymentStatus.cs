using System;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

using System.Security.Cryptography;
using System.Text;
using Azure.Security.KeyVault.Secrets;
using System.IO;

namespace ClientFunctions
{
    public class ReadDeploymentStatus
    {

        static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }


        // A function for the CLIENT to try reading from the Customer's deployment status
        [FunctionName("ReadDeploymentStatus")]
        public void Run([ServiceBusTrigger("%CommandQueueName%",
            Connection = "CommandQueueSaSConnectionStringPk")]byte[] command, ILogger log)
        {

            Console.WriteLine($"received {command} as the message");
            var responseQueue = Environment.GetEnvironmentVariable("ReponseQueueName");
            var responseQueueConnectionString = Environment.GetEnvironmentVariable("ResponseQueueSaSConnectionStringPk");
            var keyVaultUrl = Environment.GetEnvironmentVariable("KeyVaultName");
            string AESKey = Environment.GetEnvironmentVariable("AESKey");
            string IV = Environment.GetEnvironmentVariable("IV");
            string vaultUrl = Environment.GetEnvironmentVariable("KeyVaultURL");

            string decryptedCommandMessage;
            byte[] aeskey = Encoding.ASCII.GetBytes(AESKey);
            byte[] iv = Encoding.ASCII.GetBytes(IV);

            using (Aes myAes = Aes.Create())
            {
                decryptedCommandMessage = DecryptStringFromBytes_Aes(command, aeskey, iv);
            }
                Console.WriteLine(decryptedCommandMessage);

            //KV code 
            // Create system assigned managed identity to access the KV with AzureDefaultCredential
            var clientKV = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());

            // Retrieve secrets using the secret client
            KeyVaultSecret CommandQSasString = clientKV.GetSecret("CommandQSasString");
            KeyVaultSecret ReponseQueuePK = clientKV.GetSecret("ReponseQueuePK");

            Console.WriteLine(CommandQSasString.Value);
            // Update Secrets using thr secret client
            var secretNewValue1 = new KeyVaultSecret("CommandQSasString", decryptedCommandMessage);
            //secretNewValue1.Properties.ExpiresOn = DateTimeOffset.Now.AddYears(1);
            clientKV.SetSecret(secretNewValue1);
            
            Console.WriteLine(secretNewValue1.Value);
            
            //4.) Send an ACK message back to the response queuue
            var client = new ServiceBusClient(ReponseQueuePK.Value);
            log.LogInformation($"ReadDeploymentStatus triggered with command: {command}");

            var sender = client.CreateSender(responseQueue);
            var message = new ServiceBusMessage(command);
            sender.SendMessageAsync(message).GetAwaiter().GetResult();

        }
    }
}
