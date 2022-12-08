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
$keyVaultGroup = New-AzResourceGroup -Name "$prefix-key-vault-$id" -Location $location

#Create a new Key Vault
# splatting technique to store all required parameters values in table called hash table. Premium or standard
$keyVaultParameters = @{
    Name = "$prefix-key-vault-$id"
    ResourceGroupName = $keyVaultGroup.ResourceGroupName
    Location = $location
    Sku = "Premium"
}

$keyVault = New-AzKeyVault @keyVaultParameters

#Grant yourself access to the Key Vault
# Give your user principal access to all storage account permissions, on your Key Vault instance
# use two below lines only when you're using in your Azure Active Directory tenant is a guest account
# replace the UserPrincipalName with the UserPrincipalName of the guest account you're using and then run Set‑AzKeyVaultAccessPolicy
    $accessPolicy = @{
        VaultName = $keyVault.Name
        UserPrincipalName = "USER_PRINCIPAL_NAME"
        PermissionsToStorage = ("get","list","listsas","delete","set","update","regeneratekey","recover","backup","restore","purge")
    }

    Set-AzKeyVaultAccessPolicy @accessPolicy

$keyVault | Format-List

#Create a new storage account

$saAccountParameters = @{
    Name = "$($prefix)sa$id"
    ResourceGroupName = $keyVaultGroup.ResourceGroupName
    Location = $location
    SkuName = "Standard_LRS"
}

$storageAccount = New-AzStorageAccount @saAccountParameters

Get-AzStorageAccountKey -ResourceGroupName $storageAccount.ResourceGroupName -Name $storageAccount.StorageAccountName

$keyVaultSpAppId = "cfa8b339-82a2-471a-a3c9-0fc0be7a4093"

New-AzRoleAssignment -ApplicationId $keyVaultSpAppId -RoleDefinitionName 'Storage Account Key Operator Service Role' -Scope $storageAccount.Id

# Add your storage account to your Key Vault's managed storage accounts
    # last step in this process is to add this storage account as a managed storage account with Key Vault. 
    # Managed storage accounts right now are only visible through PowerShell and the Azure CLI. You can't actually see or 
    # alter these managed storage accounts within the portal.
$managedStorageAccount = @{
    VaultName = $keyVault.VaultName
    AccountName = $storageAccount.StorageAccountName
    AccountResourceId = $storageAccount.Id
    ActiveKeyName = "key1"
    RegenerationPeriod = [System.Timespan]::FromDays(90)
}

Add-AzKeyVaultManagedStorageAccount @managedStorageAccount

Get-AzKeyVaultManagedStorageAccount -VaultName $keyVault.VaultName

Update-AzKeyVaultManagedStorageAccountKey -VaultName $keyVault.VaultName -AccountName $storageAccount.StorageAccountName -KeyName "key1"

# cleanup resource group
Remove-AzResourceGroup -Name "$prefix-key-vault-$id"