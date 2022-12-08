#Prefix for resources
$prefix = "lsc"

#Basic variables
$location = "eastus"
$id = Get-Random -Minimum 100 -Maximum 9999

#Log into Azure
Add-AzAccount

#Select the correct subscription
Get-AzSubscription -SubscriptionName "Pay-As-You-Go" | Select-AzSubscription

#Create a resource group for Key Vault
$keyVaultGroup = New-AzResourceGroup -Name "lsc-key-vault-7875" -Location $location

#Create a new Key Vault
$keyVaultParameters = @{
    Name = "$prefix-key-vault-$id"
    ResourceGroupName = $keyVaultGroup.ResourceGroupName
    Location = $location
    Sku = "Premium"
}

$keyVault = New-AzKeyVault @keyVaultParameters

#If you already have a Key Vault
$keyVault = Get-AzKeyVault -VaultName "lsc-key-vault-7875" -ResourceGroupName "lsc-key-vault-7875"

#Get the existing custom roles
Get-AzRoleDefinition | Where-Object {$_.IsCustom -eq $True} | Format-Table Name, IsCustom

#Create a new custom role definition for Key Vault
$subId = (Get-AzContext).Subscription.Id

$roleInfo = Get-Content .\Key_Vault\Securing_Azure_KV_Access\custom_role.json

$roleInfo -replace "SUBSCRIPTION_ID",$subId > .\Key_Vault\Securing_Azure_KV_Access\updated_role.json

$role = New-AzRoleDefinition -InputFile .\Key_Vault\Securing_Azure_KV_Access\updated_role.json

#Assign the custom role to an existing user
$user = Get-AzADUser -UserPrincipalName "Chris@learnsmartcodinggmail.onmicrosoft.com"

$assignmentInfo = @{
    ObjectId = $user.Id
    Scope = $keyVault.ResourceId
    RoleDefinitionId = $role.Id
}

New-AzRoleAssignment @assignmentInfo

Get-AzRoleAssignment -Scope $keyVault.ResourceId

