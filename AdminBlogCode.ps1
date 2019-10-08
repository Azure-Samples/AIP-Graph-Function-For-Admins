$AzSubscriptionId = 'AZURE_SUBSCRIPTION_ID'
$userId = 'USER_EMAIL'
$AppName = 'YOUR_APP_NAME'
$FunctionName = 'FUNCTION_NAME'
$FunctionKey = 'FUNCTION_KEY'
$adminEmail = 'COMPLIANCE_ADMIN_EMAIL'

$AzRmAccount = Add-AzureRmAccount -SubscriptionId $AzSubscriptionId
$AzTenantId = (Get-AzureRmSubscription -SubscriptionId $AzSubscriptionId).TenantId
$MGTokenCache = $AzRmAccount.Context.TokenCache
$MGCachedTokens = $MGTokenCache.ReadItems() `
        | where { $_.TenantId -eq $AzTenantId } `
        | Sort-Object -Property ExpiresOn -Descending
$myGraphToken = $MGCachedTokens | Select-Object {$_.AccessToken}

$response = Invoke-RestMethod -Method Get `
                  -Uri ("https://" + $AppName + ".azurewebsites.net/api/" + $FunctionName + "?name=" + $userId + "&adminUPN=" + $adminEmail + "&code=" + $FunctionKey) `
                  -Headers @{ "Authorization" = "Bearer " + $myGraphToken.'$_.AccessToken'}

$response.results