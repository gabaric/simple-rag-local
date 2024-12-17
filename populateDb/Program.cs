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
var zeldaRecords = new List<ZeldaRecord>();

// Add locations
string locationsRaw = File.ReadAllText("zelda-locations.json");
zeldaRecords.AddRange(JsonSerializer.Deserialize<List<ZeldaRecord>>(locationsRaw));

// Add bosses
string bossesRaw = File.ReadAllText("zelda-bosses.json");
zeldaRecords.AddRange(JsonSerializer.Deserialize<List<ZeldaRecord>>(bossesRaw));

// Add characters
string charactersRaw = File.ReadAllText("zelda-characters.json");
zeldaRecords.AddRange(JsonSerializer.Deserialize<List<ZeldaRecord>>(charactersRaw));

// Add dungeons
string dungeonsRaw = File.ReadAllText("zelda-dungeons.json");
zeldaRecords.AddRange(JsonSerializer.Deserialize<List<ZeldaRecord>>(dungeonsRaw));

// Add games
string gamesRaw = File.ReadAllText("zelda-games.json");
zeldaRecords.AddRange(JsonSerializer.Deserialize<List<ZeldaRecord>>(gamesRaw));

#endregion

#region vectorizeLoadedData

// Create qdrant collection
var qdrantRecords = new List<PointStruct>();

foreach (var item in zeldaRecords)
{
    // Create an assign an embedding for each record
    item.Embedding = (await generator.GenerateAsync(
        new List<string>() { item.Name + ": " + item.Description }))[0].Vector.ToArray();

    // Add each record and its embedding to the list that will be inserted into the databsae
    qdrantRecords.Add(new PointStruct()
    {
        Id = new PointId((uint)new Random().Next(0, 10000000)),
        Vectors = item.Embedding,
        Payload =
        {
            ["name"] = item.Name,
            ["description"] = item.Description
        }
    });
}
#endregion

#region insertDataIntoQdrantDB

// Create the db collection
await qClient.CreateCollectionAsync("zelda-database", new VectorParams { Size = 768, Distance = Distance.Cosine });

// Insert the records into the database
await qClient.UpsertAsync("zelda-database", qdrantRecords);
Console.WriteLine("Finished inserting records!");

#endregion
