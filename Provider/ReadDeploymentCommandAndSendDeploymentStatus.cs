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

namespace CustomerFunctions;

// Pre-function work
// 1) create a managed identity in the Customer subscription to interface with the service bus
// 2) wire the managed identity in the IAM service bus blade and give it the "Service Bus Data Owner" permissions
public class ReadDeploymentCommandAndSendDeploymentStatus
{


    public static string Encrypt(string plainText, X509Certificate2 cert)
    {
        RSACng publicKey = (RSACng)cert.GetRSAPublicKey();
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] encryptedBytes = publicKey.Encrypt(plainBytes, RSAEncryptionPadding.Pkcs1);
        string encryptedText = Convert.ToBase64String(encryptedBytes);
        return encryptedText;
    }

    public static string Decrypt(string encryptedText, X509Certificate2 cert)
{
    RSACng privateKey = (RSACng)cert.GetRSAPrivateKey();
    byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
    byte[] decryptedBytes = privateKey.Decrypt(encryptedBytes, RSAEncryptionPadding.Pkcs1);
    string decryptedText = Encoding.UTF8.GetString(decryptedBytes);
    return decryptedText;
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

        var cmdConnectionStr = sbService.GetSASTokenConnectionString(rootString, commandQName, cmdSaPolicyName, span);
        var respConnectionStr = sbService.GetSASTokenConnectionString(rootString, responseQName, respSaPolicyName, span);
        var credential = new DefaultAzureCredential();

        string serviceBusNamespace = Environment.GetEnvironmentVariable("ServiceBusConnection__fullyQualifiedNamespace");
        string commandQueue = Environment.GetEnvironmentVariable("CommandQueueName");
        var sbClient = new ServiceBusClient(serviceBusNamespace, credential);
        
        // Create a new certificate client using the default credential from Azure.Identity 
        var certClient =
        new CertificateClient(vaultUri: new Uri(vaultUrl), credential: new DefaultAzureCredential());
        KeyVaultCertificateWithPolicy certificateWithPolicy = certClient.GetCertificate("clientpoc");
        KeyVaultCertificate certificate =
        certClient.GetCertificateVersion(certificateWithPolicy.Name, certificateWithPolicy.Properties.Version);
        X509Certificate2 x509cer = new X509Certificate2(certificate.Cer);

        string encryptedCommandMessage = Encrypt(command, x509cer);

        var sender = sbClient.CreateSender(commandQueue);
        var message = new ServiceBusMessage(encryptedCommandMessage);
        sender.SendMessageAsync(message).GetAwaiter().GetResult();

    }
}