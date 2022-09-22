using System;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

using Azure.Security.KeyVault.Certificates;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using Azure.Security.KeyVault.Secrets;


namespace ClientFunctions
{
    public class ReadDeploymentStatus
    {
        public static string Encrypt(string plainText, X509Certificate2 cert)
        {
            try
            {
                RSACng publicKey = (RSACng)cert.GetRSAPublicKey();
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = publicKey.Encrypt(plainBytes, RSAEncryptionPadding.Pkcs1);
                string encryptedText = Convert.ToBase64String(encryptedBytes);
                return encryptedText;
            }
            catch (Exception e)
            {
                Console.WriteLine("Couldn't encrypt message.");
                return "";
            }
        }

        public static string Decrypt(string encryptedText, X509Certificate2 cert)
        {

            try
            {
                RSACng privateKey = (RSACng)cert.GetRSAPrivateKey();
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                byte[] decryptedBytes = privateKey.Decrypt(encryptedBytes, RSAEncryptionPadding.Pkcs1);
                string decryptedText = Encoding.UTF8.GetString(decryptedBytes);
                return decryptedText;
            }
            catch (Exception e)
            {
                Console.WriteLine("Couldn't decrypt message.");
                return "";
            }

        }
        // A function for the CLIENT to try reading from the Customer's deployment status
        [FunctionName("ReadDeploymentStatus")]
        public void Run([ServiceBusTrigger("%CommandQueueName%",
            Connection = "CommandQueueSaSConnectionStringPk")]string command, ILogger log)
        {

            Console.WriteLine($"received {command} as the message");
            var responseQueue = Environment.GetEnvironmentVariable("ReponseQueueName");
            var responseQueueConnectionString = Environment.GetEnvironmentVariable("ResponseQueueSaSConnectionStringPk");
            var keyVaultUrl = Environment.GetEnvironmentVariable("KeyVaultName");

            string vaultUrl = Environment.GetEnvironmentVariable("KeyVaultURL");


            // Create a new certificate client using the default credential from Azure.Identity 
            var certClient =
            new CertificateClient(vaultUri: new Uri(vaultUrl), credential: new DefaultAzureCredential());
            KeyVaultCertificateWithPolicy certificateWithPolicy = certClient.GetCertificate("clientpoc2");
            KeyVaultCertificate certificate =
            certClient.GetCertificateVersion(certificateWithPolicy.Name, certificateWithPolicy.Properties.Version);
            X509Certificate2 x509cer = new X509Certificate2(certificate.Cer);

            string decryptedMsg = Decrypt(command, x509cer);
            if (!decryptedMsg.Equals(""))
                Console.WriteLine($"Decrypted Msg: {decryptedMsg}");
            else
            {
                Console.WriteLine("Problem Decrypting Msg");
                return;
            }

            //KV code 
            //0.) Investigate function restarting
            //1.) Create system assigned managed identity to access the KV with AzureDefaultCredential
            //   var clientKV = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());

            //2.) Decrypt the message received from the command queue
            //3.) Store the new values in the KV

            // Retrieve secrets using the secret client
            // KeyVaultSecret CommandQueuePK = clientKV.GetSecret("CommandQueuePK");
            //  KeyVaultSecret ReponseQueuePK = clientKV.GetSecret("ReponseQueuePK");
            // KeyVaultSecret Encryptionkey = clientKV.GetSecret("ClientEncryptionKey");

            //            Console.WriteLine(ReponseQueuePK.Name);
            //           Console.WriteLine(ReponseQueuePK.Value);
            //  Console.WriteLine(Encryptionkey.Value);


            // Update Secrets using thr secret client
            /* var secretNewValue1 = new KeyVaultSecret("CommandQueuePK", "bhjd4DDgsa");
             secretNewValue1.Properties.ExpiresOn = DateTimeOffset.Now.AddYears(1);
             clientKV.SetSecret(secretNewValue1);

             var secretNewValue2 = new KeyVaultSecret("ReponseQueuePK", "bhjd4DDgsa");
             secretNewValue2.Properties.ExpiresOn = DateTimeOffset.Now.AddYears(1);
             clientKV.SetSecret(secretNewValue2);
             */
            //4.) Send an ACK message back to the response queuue
            // var client = new ServiceBusClient(ReponseQueuePK.Value);
            //  log.LogInformation($"ReadDeploymentStatus triggered with command: {command}");

            // var sender = client.CreateSender(responseQueue);
            // var message = new ServiceBusMessage(command);
            //  sender.SendMessageAsync(message).GetAwaiter().GetResult();

        }
    }
}
