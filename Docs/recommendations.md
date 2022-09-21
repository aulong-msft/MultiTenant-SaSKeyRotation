# Rotation Design Recommendations in FAQ Format

## What is the benefit of having a second layer of encryption?

> The Service Bus communication is encrypted by default.  Clients are able to access data via a SAS Token.  The security provided via this infrastructure is solely reliant on the security of the provider.  If somehow, the provider is compromised all of the communication with the clients is also compromised.  In the recommendation for rotation of secrets, we are having the client create a verified CA cert (in our PoC we just use a self-signed cert) and only share the public cert with the provider.  In this situation, if the SaS tokens are compromised this mechanism can protect future secrets from being tampered with.

## Asymmetric Encryption, Symmetric Encryption, and Certs

The security industry best practice suggests to use both symmetic and asymmetic key enrcpytion to ensure the keys are generated and passed safely along to both the provider and the client. In our scenario, we have decided to use a cert; for customer this should be gathered from a trusted certificate authority. With this methodology we can ensure the integrity and confidentiality of cert/key exchanges in our multi-tenanted scenario. Azure Keyvault allows for the generation of public/private key pairs (otherwise known as asymmetric keys). Then the public key would need to be sent over to the provider's keyvault to be referencesd within the architecture.

## How is the Certificate created and shared?

In our PoC a cert is generated from a Powershell CMDLT, In a real scenario this cert should be obtained by a trusted source and given to the Provider to use for encrpyting the key rotation payload which contains the new SaS key pairs. Once the payload is received from the provider on the Service Bus, the client can decrypt the payload with the private cert which will be stored in the client's KV.

## Stories that need to be investigated

1. Investigate how to restart a function when the new SaS key rotates

1. Spike?? Introduce a client certificate input into the AMA onboarding screen to be placed into the Provier's KV

1. Spike?? Investifate how would the client's cert will get updated

1. Convert powershell script to rotate the Service Bus keys into Pulumi

1. Integrate encryption/decryption into the solution for both sending and receiving functions

## PoC Architecture

![PoC Infrastructure](/Docs/Screenshot%202022-09-21%20063758.png)