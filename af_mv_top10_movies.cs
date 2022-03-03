using System;
using System.Collections.Generic;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;

using System.Linq;
using Microsoft.Azure.Cosmos;
// using Shared;


namespace Company.Function
{
    public class MovieCount
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        public int ProductId { get; set; }
        public int Count { get; set; }

        public MovieCount()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
    public static class af_mv_top10_movies
    {


        
        [FunctionName("af_mv_top10_movies")]
        public static async Task Run([CosmosDBTrigger(
            databaseName: "czcontosomigration",
            collectionName: "RealtimeOrders",
            ConnectionStringSetting = "cosmos_DOCUMENTDB",
            CreateLeaseCollectionIfNotExists = true,
            LeaseCollectionName = "leases-af-movies")]IReadOnlyList<Document> input,
           
            ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                
                string _endpointUrl = System.Environment.GetEnvironmentVariable("cosmos_endpointUrl");
                string _primaryKey = System.Environment.GetEnvironmentVariable("cosmos_primaryKey");
                string _databaseId = "czcontosomigration";
                string _containerId = "mvMostPopularMovies";

                CosmosClient cosmosClient = new CosmosClient(_endpointUrl, _primaryKey);

                log.LogInformation("Documents modified " + input.Count);
                log.LogInformation("First document Id " + input[0].Id);

                             
                var db = cosmosClient.GetDatabase(_databaseId);
                var container = db.GetContainer(_containerId);
                log.LogInformation("connected to conainer");

                var tasks = new List<Task>();
                foreach (var doc in input)
                {
                    var json = doc.ToString();

                    log.LogInformation("Processing order...");

                    //get OrderItems
                    var items = doc.GetPropertyValue <Document[]>("Details");

                    log.LogInformation("order items count: " + items.Length);
                    
                    foreach (var item in items) {
                        var email = item.GetPropertyValue <string>("Email");
                        var ProductId = item.GetPropertyValue <int>("ProductId");
                        log.LogInformation("Purchased: " + ProductId);

                        //UPSERT
                        var query = new QueryDefinition("select * from mvMostPopularMovies s where s.ProductId = @ProductId").WithParameter("@ProductId", ProductId);

                        var resultSet = container.GetItemQueryIterator<MovieCount>(query, requestOptions: new QueryRequestOptions() { PartitionKey = new Microsoft.Azure.Cosmos.PartitionKey(ProductId), MaxItemCount = 1 });

                        while (resultSet.HasMoreResults)
                        {
                            var movieCount = (await resultSet.ReadNextAsync()).FirstOrDefault();

                            if (movieCount == null)
                            {
                                //todo: Add new doc code here
                                log.LogInformation("Creating new");
                                movieCount = new MovieCount();
                                movieCount.ProductId = ProductId;
                                movieCount.Count = 1;
                            }
                            else
                            {
                                //todo: Add existing doc code here
                                log.LogInformation("Updating existing");
                                movieCount.Count += 1;
                            }

                            //todo: Upsert document
                            log.LogInformation("Upserting materialized view document");
                            tasks.Add(container.UpsertItemAsync(movieCount, new Microsoft.Azure.Cosmos.PartitionKey(movieCount.ProductId)));
                        }
                    }
                }

                await Task.WhenAll(tasks);
            }
        }
    }
}
