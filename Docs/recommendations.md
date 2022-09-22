# Rotation Design Recommendations in FAQ Format

## What is the benefit of having a second layer of encryption?

> The Service Bus communication is encrypted by default.  Clients are able to access data via a SAS Token.  The security provided via this infrastructure is solely reliant on the security of the provider.  If somehow, the provider is compromised all of the communication with the clients is also compromised.  In the recommendation for rotation of secrets, we are having the client create a verified CA cert (in our PoC we just use a self-signed cert) and only share the public cert with the provider.  In this situation, if the SaS tokens are compromised this mechanism can protect future secrets from being tampered with.

## Asymmetric Encryption, Symmetric Encryption, and Certs

The security industry best practice suggests to use both symmetic and asymmetic key enrcpytion to ensure the keys are generated and passed safely along to both the provider and the client. In our scenario, we have decided to use a cert; for customer this should be gathered from a trusted certificate authority. With this methodology we can ensure the integrity and confidentiality of cert/key exchanges in our multi-tenanted scenario. Azure Keyvault allows for the generation of public/private key pairs (otherwise known as asymmetric keys). Then the public key would need to be sent over to the provider's keyvault to be referencesd within the architecture.

## How is a Key or Certificate created and shared?

In our PoC a cert is generated from a Powershell CMDLT, In a real scenario this cert should be obtained by a trusted source and given to the Provider to use for encrpyting the key rotation payload which contains the new SaS key pairs. Once the payload is received from the provider on the Service Bus, the client can decrypt the payload with the private cert which will be stored in the client's KV.

## PoC Architecture

![PoC Infrastructure](/Docs/Screenshot%202022-09-21%20063758.png)

The following architecture above has helped envision and generate a plan of action when we were desinging the key rotation session.
The flow for the key rotation goes as follows:

### Step 0 in the Diagram 

This can be thought of as the Pulumi/ Pre-work setup to kick off key rotation

1. First a key specific message will flow down to update the keys. It communicated that a 30 day cadence for update will occur, and we will use the same 30 day period to send an update key message as well.

1. Create a powershell script to rotate the service bus message keys and include that into a key rotation pipeline. Example for this powershell script can be found in the root called regeneratePrimaryKey.ps1 

### Step 1 in the Diagram

This step refers to encryption pre-work and is still under investigation

1.  Create a Powershell script to generate a symmetic key. This key will be used for the encryption and decryption process of the newly updated SAS keys (both for service bus queues and ACR pull key) which will be added as an encrypted service bus message and delivered some queue (perhaps the command queue) for customers to poll and retreive the new key value.

1. Share the key within the customer KV/ provider’s KV (within the secrets section). This can potentially be done in a Pulumi script, generated on the customers behalf, or apart of onboarding a customer in AMA.

Items to be considered:
Also encrypt the key used for getCustomerCommandQueueSaSToken function aswell as the ACR pull key

### Step 2 in the Diagram

1. For PoC purposes this is a no-op to just kick off the operations to simulate an "update keys" command to come into the system. Currently its an HTTP triggered function wired up with a managed identity which will place a message in the "InitiateDeployment Queue".

### Step 3 and 4 in the Diagram

This function is a Service bus triggered function - The initiate deployment status function consumes the message from the initiatedDeployment Queue 

1. this is where the keys will be pulled from the Service Bus (freshly rotated)

1. bundle the keys in a secret message format (SecretMessage.cs)

1. encrpyt the payload and then send the message into the command queue (investigate encrypting this payload with AES encryption (symmetric encryption)

### Step 5-8 in the Diagram

This client function will consume the message, decrypt the payload, save off the keys into a KV (or whereever - configs w.e) and sends back an ack to the response queue.

