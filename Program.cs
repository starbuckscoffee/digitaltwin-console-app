using System;
using Azure.DigitalTwins.Core;
using Azure.Identity;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using Azure;

namespace clientapp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string adtInstanceUrl = "https://hgadt01.api.sea.digitaltwins.azure.net";
            var credential = new DefaultAzureCredential();
            var client = new DigitalTwinsClient(new Uri(adtInstanceUrl), credential);
            Console.WriteLine($"Connected to your ADT instance: success");
            //
            // Upload DTDL model and cread DTDL model instance
            //
            Console.WriteLine($"Upload SampleModel.json");
            string dtdl = File.ReadAllText("SampleModel.json");
            var models = new List<string> {dtdl};
            try{
                await client.CreateModelsAsync(models);
                Console.WriteLine($"Created DTDL modoe: Sample.Model");
            } catch (RequestFailedException e) {
                Console.WriteLine($"Upload Model Error: {e.Status}: {e.Message}");
            }                
            //
            //  Read list of ADT models
            //
            AsyncPageable<DigitalTwinsModelData> modelDataList = client.GetModelsAsync();
            await foreach(DigitalTwinsModelData md in modelDataList)
            {
                Console.WriteLine($"Model: {md.Id}");                
            }
            Console.WriteLine(@"---End of List of Model IDs ---");
            //
            //Create 3 Innstances of DTLD model  
            //
            var twinData = new BasicDigitalTwin();
            twinData.Metadata.ModelId="dtmi:com:example:SampleModel;1";
            twinData.Contents.Add("data", $"Hello");

            string prefix = "sampleTwin-";
            for (int i= 0; i<3; i++){
                try{    
                    twinData.Id = $"{prefix}{i}";
                    await client.CreateOrReplaceDigitalTwinAsync<BasicDigitalTwin>(twinData.Id, twinData);
                    Console.WriteLine($"Create twin Instance: {twinData.Id}");
                } catch (RequestFailedException e) {
                    Console.WriteLine($"Create twin Failed :  {e.Status}  :  {e.Message}");
                }
            }
            //
            // Create relationshp among 3 instances
            //
            Console.WriteLine(@"---Start to create relationship --");
            await CreateRelationshipAsync( client, "sampleTwin-0", "sampleTwin-1");
            await CreateRelationshipAsync( client, "sampleTwin-0", "sampleTwin-2");
            //
            //  List Relationship os sampleTwin-0 / 1 / 2 
            //
            Console.WriteLine(@"---Start to show relationship ---");
            await ListRelationishipAsync( client, "sampleTwin-0");
        }
        public static async Task CreateRelationshipAsync(DigitalTwinsClient client, string srcId, string targetId  ){

            var relationship = new BasicRelationship {
                Name = "contains",
                TargetId = targetId
            };            
            try{
                string relId = $"{srcId}-contains-{targetId}";
                await client.CreateOrReplaceRelationshipAsync( srcId, relId, relationship);
                Console.WriteLine($"Create relationship [contains]] suucessfully ");
            } catch (RequestFailedException e) {
                Console.WriteLine($"Create Relationship Error:  {e.Status} : {e.Message} ");
            }
        }

        public static async Task ListRelationishipAsync( DigitalTwinsClient client, string srcId)
        {
            try{
                AsyncPageable<BasicRelationship> results = client.GetRelationshipsAsync<BasicRelationship>(srcId);
                Console.WriteLine($"Twin {srcId}  is connected to: ");
                await foreach (BasicRelationship rel in results)
                {
                    Console.WriteLine($"  --{rel.Name}-->{rel.TargetId}");
                }
            } catch (RequestFailedException e) {
                Console.WriteLine($"Relationship Retrieval Error :  {e.Status} :  {e.Message}");
            }            
        }

    }
}
