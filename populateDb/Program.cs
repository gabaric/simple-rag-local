using Microsoft.Extensions.AI;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Shared;
using System.Text.Json;

# region createClients
// Create Qdrant client
var qClient = new QdrantClient("localhost");

// Create Ollama embedding client
IEmbeddingGenerator<string, Embedding<float>> generator =
    new OllamaEmbeddingGenerator(new Uri("http://localhost:11434/"), "nomic-embed-text");

// Create Ollama chat client
IChatClient chatClient =
    new OllamaChatClient(new Uri("http://localhost:11434/"), "phi3:medium");
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
    item.Embedding = (await generator.GenerateAsync(
        new List<string>() { item.Name + ": " + item.Description }))[0].Vector.ToArray();

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
    await qClient.CreateCollectionAsync("zelda-database", new VectorParams { Size = 768, Distance = Distance.Cosine });
    await qClient.UpsertAsync("zelda-database", qdrantRecords);
    Console.WriteLine("Finished inserting records!");

    #endregion
