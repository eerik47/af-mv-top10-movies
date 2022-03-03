# af-mv-top10-movies

Example of creating Materialized View on Cosmos DB


Create and fill `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "<YOUR CONNECTION STRING>",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "cosmos_DOCUMENTDB": "<COSMOS DB Connection String - where changefeed is>",
    "cosmos_endpointUrl": "<COSMOS DB URL - where lookup is done (probably same as above)>",
    "cosmos_primaryKey" : "<COSMOS DB Key - where lookup is done (probably same as above)>",
    "cosmos_databaseId": "<YOUR COSMOS DB Database>",
    "cosmos_containerId": "<YOUR COSMOS DB Container>"
  }
}
```