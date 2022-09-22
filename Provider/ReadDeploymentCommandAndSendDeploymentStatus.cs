using System;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Wtw.SecretRotationPOC.ServiceBus;
using Azure.Security.KeyVault.Certificates;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace CustomerFunctions;

// Pre-function work
// 1) create a managed identity in the Customer subscription to interface with the service bus
// 2) wire the managed identity in the IAM service bus blade and give it the "Service Bus Data Owner" permissions
public class ReadDeploymentCommandAndSendDeploymentStatus
{


    public static string Encrypt(string plainText, X509Certificate2 cert)
    {
        try
        {
            RSACng publicKey = (RSACng)cert.GetRSAPublicKey();
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = publicKey.Encrypt(plainBytes, RSAEncryptionPadding.);
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


    static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
    {
        // Check arguments.
        if (plainText == null || plainText.Length <= 0)
            throw new ArgumentNullException("plainText");
        if (Key == null || Key.Length <= 0)
            throw new ArgumentNullException("Key");
        if (IV == null || IV.Length <= 0)
            throw new ArgumentNullException("IV");
        byte[] encrypted;

        // Create an Aes object
        // with the specified key and IV.
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Key;
            aesAlg.IV = IV;

            // Create an encryptor to perform the stream transform.
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            // Create the streams used for encryption.
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        //Write all data to the stream.
                        swEncrypt.Write(plainText);
                    }
                    encrypted = msEncrypt.ToArray();
                }
            }
        }

        // Return the encrypted bytes from the memory stream.
        return encrypted;
    }


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
        string vaultUrl = Environment.GetEnvironmentVariable("KeyVaultURL");
        string AESKey = Environment.GetEnvironmentVariable("AESKey");

        var cmdConnectionStr = sbService.GetSASTokenConnectionString(rootString, commandQName, cmdSaPolicyName, span);
        var respConnectionStr = sbService.GetSASTokenConnectionString(rootString, responseQName, respSaPolicyName, span);
        var credential = new DefaultAzureCredential();

        string serviceBusNamespace = Environment.GetEnvironmentVariable("ServiceBusConnection__fullyQualifiedNamespace");
        string commandQueue = Environment.GetEnvironmentVariable("CommandQueueName");
        var sbClient = new ServiceBusClient(serviceBusNamespace, credential);

        /* Create a new certificate client using the default credential from Azure.Identity 
        var certClient =
        new CertificateClient(vaultUri: new Uri(vaultUrl), credential: new DefaultAzureCredential());
        KeyVaultCertificateWithPolicy certificateWithPolicy = certClient.GetCertificate("clientpoc");
        KeyVaultCertificate certificate =
        certClient.GetCertificateVersion(certificateWithPolicy.Name, certificateWithPolicy.Properties.Version);
        X509Certificate2 x509cer = new X509Certificate2(certificate.Cer);*/

        byte[] encryptedCommandMessage;
        byte[] aeskey = Convert.FromBase64String(AESKey);
        
        using (Aes myAes = Aes.Create())
        {
            encryptedCommandMessage = EncryptStringToBytes_Aes(cmdConnectionStr.Result, aeskey, myAes.IV);
        }
        var sender = sbClient.CreateSender(commandQueue);
        var message = new ServiceBusMessage(new BinaryData(encryptedCommandMessage));
        sender.SendMessageAsync(message).GetAwaiter().GetResult();
    }
}