# Rotation Design Recommendations in FAQ Format


**1. What is the benefit of having a second layer of encryption?**

> The Service Bus communication is encrypted by default.  Clients are able to access data via a SAS Token.  The security provided via this infrastructure is solely reliant on the security of the provider.  If somehow, the provider is compromised all of the communication with the clients is also compromised.  In the recommendation for rotation of secrets, we are having the client create their asymmetric key pair and only share the public key with the provider.  In this situation, if the provid


**1. Why use Asymmetric Encryption instead of Symmetric Encryption**

> TODO


**1. Why use Certificates or PKI?**

> TODO


**1. How is the Asymmetric Key Pair created and shared?**

> TODO

