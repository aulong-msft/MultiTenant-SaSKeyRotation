Function Invoke-RotateServiceBusKeys
{
param($ResourceGroup, $SBNameSpace, $QueueName, $AuthRuleName)
$primary = Get-AzServiceBusKey -ResourceGroupName $ResourceGroup -Namespace $SBNameSpace -Queue $QueueName -Name $AuthRuleName | Select-Object -Property PrimaryKey

New-AzServiceBusKey -ResourceGroupName $ResourceGroup -Namespace $SBNameSpace -Queue $QueueName -Name $AuthRuleName -RegenerateKey SecondaryKey -KeyValue $primary.PrimaryKey

New-AzServiceBusKey -ResourceGroupName $ResourceGroup -Namespace $SBNameSpace -Queue $QueueName -Name $AuthRuleName -RegenerateKey PrimaryKey
}

Get-AzSubscription -SubscriptionId "779303a1-aaa1-4bda-976c-f67fafd98442" -TenantId "72f988bf-86f1-41af-91ab-2d7cd011db47" | Set-AzContext
# You can uncomment this line and run script to rotate in the POC env
 Invoke-RotateServiceBusKeys -ResourceGroup 'KeyRotationRG' -SBNameSpace 'KeyRotationNS' -QueueName 'commandqueue' -AuthRuleName 'customerCommandQueueListenSaS'
 Invoke-RotateServiceBusKeys -ResourceGroup 'KeyRotationRG' -SBNameSpace 'KeyRotationNS' -QueueName 'responsequeue' -AuthRuleName 'Responsequeue'

<#
This script rotates the primary into the secondary and regenerates the primary per the MS Docs.


Excerpt from MS Docs..
Regenerating Keys
It's recommended that you periodically regenerate the keys used in the Shared Access Authorization Policy. 
The primary and secondary key slots exist so that you can rotate keys gradually. If your application generally 
uses the primary key, you can copy the primary key into the secondary key slot, and only then regenerate the primary key. 
The new primary key value can then be configured into the client applications, which have continued access using the old primary 
key in the secondary slot. Once all clients are updated, you can regenerate the secondary key to finally retire the old primary key.

https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-sas

#>



