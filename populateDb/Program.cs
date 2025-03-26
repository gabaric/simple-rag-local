using Microsoft.Extensions.AI;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Shared;
using System.Text.Json;

#region createClients

// Create Qdrant client
var qClient = new QdrantClient("localhost");

// Create Ollama embedding client
IEmbeddingGenerator<string, Embedding<float>> generator =
    new OllamaEmbeddingGenerator(new Uri("http://localhost:11434/"), "nomic-embed-text");
#endregion

#region loadDataFromFiles

Console.WriteLine($"Loading records...");
var offerRecords = new List<OfferRecord>();

var folder_name = "v5";

var list_file_name = new List<string> { "orlade-contexte-enjeux-besoin", "orlade-convictions" ,"orlade-comprehension-situation" ,"orlade-apport-valeur"};

foreach (var file_name in list_file_name)
{
    string Raw = File.ReadAllText($"{folder_name}/{file_name}.json");
    offerRecords.AddRange(JsonSerializer.Deserialize<List<OfferRecord>>(Raw));
}

// // Add orlade-contexte-enjeux-besoin
// string cebRaw = File.ReadAllText($"{folder_name}/orlade-contexte-enjeux-besoin.json");
// offerRecords.AddRange(JsonSerializer.Deserialize<List<OfferRecord>>(cebRaw));

// // Add orlade-convictions
// string convictionsRaw = File.ReadAllText($"{folder_name}/orlade-convictions.json");
// offerRecords.AddRange(JsonSerializer.Deserialize<List<OfferRecord>>(convictionsRaw));

// // Add orlade-comp-situation
// string compSituationRaw = File.ReadAllText($"{folder_name}/orlade-comprehension-situation.json");
// offerRecords.AddRange(JsonSerializer.Deserialize<List<OfferRecord>>(compSituationRaw));

// // Add orlade-apport-valeur
// string apportValeurRaw = File.ReadAllText($"{folder_name}/orlade-apport-valeur.json");
// offerRecords.AddRange(JsonSerializer.Deserialize<List<OfferRecord>>(apportValeurRaw));

#endregion

#region vectorizeLoadedData

// Create qdrant collection
var qdrantRecords = new List<PointStruct>();

foreach (var item in offerRecords)
{
    // Create an assign an embedding for each record
    item.Embedding = (await generator.GenerateAsync(
        new List<string>() { $"Thème : {item.Context} \nTitre : {item.Title} \nTexte : {item.TextContent}."}))[0].Vector.ToArray();

    // Add each record and its embedding to the list that will be inserted into the databsae
    qdrantRecords.Add(new PointStruct()
    {
        Id = new PointId((uint)new Random().Next(0, 10000000)),
        Vectors = item.Embedding,
        Payload =
        {
            ["context"] = item.Context,
            ["name_offer"] = item.NameOffer,
            ["title"] = item.Title,
            ["text_content"] = item.TextContent
        }
    });
}
#endregion

#region insertDataIntoQdrantDB

// Create the db collection
await qClient.CreateCollectionAsync($"orlade-offers-database-{folder_name}", new VectorParams { Size = 768, Distance = Distance.Cosine });

// Insert the records into the database
await qClient.UpsertAsync($"orlade-offers-database-{folder_name}", qdrantRecords);
Console.WriteLine("Finished inserting records!");

#endregion
