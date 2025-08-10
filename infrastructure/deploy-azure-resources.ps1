# Azure Cab Service Infrastructure Deployment Script
param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$true)]
    [string]$Location = "East US",
    
    [Parameter(Mandatory=$true)]
    [string]$EnvironmentName = "dev"
)

# Variables
$subscriptionId = (az account show --query id -o tsv)
$resourcePrefix = "cabservice-$EnvironmentName"

Write-Host "Deploying Cab Service Infrastructure to Azure..." -ForegroundColor Green
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor Yellow
Write-Host "Location: $Location" -ForegroundColor Yellow
Write-Host "Environment: $EnvironmentName" -ForegroundColor Yellow

# Create Resource Group
Write-Host "Creating Resource Group..." -ForegroundColor Blue
az group create --name $ResourceGroupName --location $Location

# Create Azure Cosmos DB Account
Write-Host "Creating Azure Cosmos DB Account..." -ForegroundColor Blue
$cosmosAccountName = "$resourcePrefix-cosmos"
az cosmosdb create `
    --name $cosmosAccountName `
    --resource-group $ResourceGroupName `
    --locations regionName=$Location failoverPriority=0 isZoneRedundant=False `
    --default-consistency-level "Session" `
    --enable-automatic-failover false `
    --enable-multiple-write-locations false

# Create Cosmos DB Databases and Containers
Write-Host "Creating Cosmos DB Databases and Containers..." -ForegroundColor Blue
$databases = @("PassengerDB", "DriverDB", "BookingDB", "PaymentDB", "NotificationDB")
$containers = @(
    @{db="PassengerDB"; container="passengers"; partitionKey="/id"},
    @{db="DriverDB"; container="drivers"; partitionKey="/id"},
    @{db="DriverDB"; container="deviceTokens"; partitionKey="/userId"},
    @{db="BookingDB"; container="bookings"; partitionKey="/passengerId"},
    @{db="PaymentDB"; container="payments"; partitionKey="/bookingId"},
    @{db="NotificationDB"; container="notifications"; partitionKey="/userId"},
    @{db="NotificationDB"; container="templates"; partitionKey="/type"}
)

foreach ($db in $databases) {
    az cosmosdb sql database create --account-name $cosmosAccountName --resource-group $ResourceGroupName --name $db
}

foreach ($container in $containers) {
    az cosmosdb sql container create `
        --account-name $cosmosAccountName `
        --resource-group $ResourceGroupName `
        --database-name $container.db `
        --name $container.container `
        --partition-key-path $container.partitionKey `
        --throughput 400
}

# Create Service Bus Namespace
Write-Host "Creating Service Bus Namespace..." -ForegroundColor Blue
$serviceBusNamespace = "$resourcePrefix-servicebus"
az servicebus namespace create `
    --name $serviceBusNamespace `
    --resource-group $ResourceGroupName `
    --location $Location `
    --sku Standard

# Create Service Bus Topics and Subscriptions
Write-Host "Creating Service Bus Topics and Subscriptions..." -ForegroundColor Blue
$topics = @("booking-events", "payment-events", "driver-events", "notification-events")
$subscriptions = @(
    @{topic="booking-events"; subscription="notification-processor"},
    @{topic="booking-events"; subscription="payment-processor"},
    @{topic="payment-events"; subscription="notification-processor"},
    @{topic="driver-events"; subscription="location-processor"},
    @{topic="driver-events"; subscription="notification-processor"}
)

foreach ($topic in $topics) {
    az servicebus topic create --name $topic --namespace-name $serviceBusNamespace --resource-group $ResourceGroupName
}

foreach ($sub in $subscriptions) {
    az servicebus topic subscription create `
        --name $sub.subscription `
        --topic-name $sub.topic `
        --namespace-name $serviceBusNamespace `
        --resource-group $ResourceGroupName
}

# Create Application Insights
Write-Host "Creating Application Insights..." -ForegroundColor Blue
$appInsightsName = "$resourcePrefix-insights"
az monitor app-insights component create `
    --app $appInsightsName `
    --location $Location `
    --resource-group $ResourceGroupName `
    --kind web

# Create App Service Plans
Write-Host "Creating App Service Plans..." -ForegroundColor Blue
$appServicePlanName = "$resourcePrefix-plan"
az appservice plan create `
    --name $appServicePlanName `
    --resource-group $ResourceGroupName `
    --location $Location `
    --sku S1 `
    --number-of-workers 2

# Create App Services
Write-Host "Creating App Services..." -ForegroundColor Blue
$services = @("passenger", "driver", "booking", "payment", "notification")
foreach ($service in $services) {
    $appName = "$resourcePrefix-$service-api"
    az webapp create `
        --name $appName `
        --resource-group $ResourceGroupName `
        --plan $appServicePlanName `
        --runtime "DOTNET:8.0"
    
    # Configure app settings
    az webapp config appsettings set `
        --name $appName `
        --resource-group $ResourceGroupName `
        --settings `
        "CosmosDb:ConnectionString=@Microsoft.KeyVault(SecretUri=https://$resourcePrefix-keyvault.vault.azure.net/secrets/CosmosDbConnectionString/)" `
        "ServiceBus:ConnectionString=@Microsoft.KeyVault(SecretUri=https://$resourcePrefix-keyvault.vault.azure.net/secrets/ServiceBusConnectionString/)" `
        "ApplicationInsights:ConnectionString=@Microsoft.KeyVault(SecretUri=https://$resourcePrefix-keyvault.vault.azure.net/secrets/AppInsightsConnectionString/)"
}

# Create Azure Functions App
Write-Host "Creating Azure Functions App..." -ForegroundColor Blue
$functionsAppName = "$resourcePrefix-functions"
$storageAccountName = ($resourcePrefix -replace "-", "") + "storage"

# Create storage account for functions
az storage account create `
    --name $storageAccountName `
    --resource-group $ResourceGroupName `
    --location $Location `
    --sku Standard_LRS

az functionapp create `
    --name $functionsAppName `
    --resource-group $ResourceGroupName `
    --consumption-plan-location $Location `
    --runtime dotnet-isolated `
    --runtime-version 8 `
    --storage-account $storageAccountName `
    --functions-version 4

# Create Key Vault
Write-Host "Creating Key Vault..." -ForegroundColor Blue
$keyVaultName = "$resourcePrefix-keyvault"
az keyvault create `
    --name $keyVaultName `
    --resource-group $ResourceGroupName `
    --location $Location `
    --sku standard

# Store connection strings in Key Vault
$cosmosConnectionString = (az cosmosdb keys list --name $cosmosAccountName --resource-group $ResourceGroupName --type connection-strings --query "connectionStrings[0].connectionString" -o tsv)
$serviceBusConnectionString = (az servicebus namespace authorization-rule keys list --name RootManageSharedAccessKey --namespace-name $serviceBusNamespace --resource-group $ResourceGroupName --query primaryConnectionString -o tsv)
$appInsightsConnectionString = (az monitor app-insights component show --app $appInsightsName --resource-group $ResourceGroupName --query connectionString -o tsv)

az keyvault secret set --vault-name $keyVaultName --name "CosmosDbConnectionString" --value $cosmosConnectionString
az keyvault secret set --vault-name $keyVaultName --name "ServiceBusConnectionString" --value $serviceBusConnectionString
az keyvault secret set --vault-name $keyVaultName --name "AppInsightsConnectionString" --value $appInsightsConnectionString

# Create API Management
Write-Host "Creating API Management..." -ForegroundColor Blue
$apimName = "$resourcePrefix-apim"
az apim create `
    --name $apimName `
    --resource-group $ResourceGroupName `
    --location $Location `
    --publisher-email "admin@cabservice.com" `
    --publisher-name "Cab Service" `
    --sku-name Developer `
    --sku-capacity 1

# Create Storage Account for Frontend
Write-Host "Creating Storage Account for Frontend..." -ForegroundColor Blue
$frontendStorageAccount = ($resourcePrefix -replace "-", "") + "frontend"
az storage account create `
    --name $frontendStorageAccount `
    --resource-group $ResourceGroupName `
    --location $Location `
    --sku Standard_LRS `
    --kind StorageV2

# Enable static website hosting
az storage blob service-properties update `
    --account-name $frontendStorageAccount `
    --static-website `
    --index-document index.html `
    --404-document 404.html

# Create CDN Profile and Endpoint
Write-Host "Creating CDN Profile and Endpoint..." -ForegroundColor Blue
$cdnProfileName = "$resourcePrefix-cdn"
$cdnEndpointName = "$resourcePrefix-endpoint"

az cdn profile create `
    --name $cdnProfileName `
    --resource-group $ResourceGroupName `
    --location $Location `
    --sku Standard_Microsoft

$storageEndpoint = (az storage account show --name $frontendStorageAccount --resource-group $ResourceGroupName --query "primaryEndpoints.web" -o tsv)
$originHostName = ($storageEndpoint -replace "https://", "" -replace "/", "")

az cdn endpoint create `
    --name $cdnEndpointName `
    --profile-name $cdnProfileName `
    --resource-group $ResourceGroupName `
    --origin $originHostName `
    --origin-host-header $originHostName

Write-Host "Azure Infrastructure Deployment Completed!" -ForegroundColor Green
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor Yellow
Write-Host "Cosmos DB: $cosmosAccountName" -ForegroundColor Yellow
Write-Host "Service Bus: $serviceBusNamespace" -ForegroundColor Yellow
Write-Host "App Insights: $appInsightsName" -ForegroundColor Yellow
Write-Host "Key Vault: $keyVaultName" -ForegroundColor Yellow
Write-Host "API Management: $apimName" -ForegroundColor Yellow
Write-Host "Frontend Storage: $frontendStorageAccount" -ForegroundColor Yellow
Write-Host "CDN Endpoint: $cdnEndpointName" -ForegroundColor Yellow
