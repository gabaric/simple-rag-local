using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Qdrant.Client;
using Shared;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

#region createClients
var qClient = new QdrantClient("localhost");

IEmbeddingGenerator<string, Embedding<float>> generator =
    new OllamaEmbeddingGenerator(new Uri("http://localhost:11434/"), "nomic-embed-text");

IChatClient chatClient =
    new OllamaChatClient(new Uri("http://localhost:11434/"), "phi3:mini");

Console.WriteLine("Ask a question. This bot is grounded in Zelda data due to RAG, so it's good at those topics.");
#endregion

while (true)
{
    Console.WriteLine();

    // Create chat history
    List<ChatMessage> chatHistory = new();

    // Get user prompt
    var userPrompt = Console.ReadLine();

    // Create an embedding version of the prompt
    var promptEmbedding = (await generator.GenerateAsync(new List<string>() { userPrompt }))[0].Vector.ToArray();

    // Run a vector search using the prompt embedding
    var returnedLocations = await qClient.QueryAsync(
        collectionName: "zelda-database",
        query: promptEmbedding,
        limit: 25
    );
    
    // Use this for generic chat
    //chatHistory.Add(new ChatMessage(ChatRole.User, userPrompt));

    // Use this for grounded chat
    // Add the returned records from the vector search to the prompt
    var builder = new StringBuilder();
    foreach (var location in returnedLocations)
    {
        builder.AppendLine($"{location.Payload["name"].StringValue}: {location.Payload["description"].StringValue}.");
    }

    // Assemble the full prompt to the chat AI model using instructions,
    // the original user prompt, and the retrieved relevant data
    chatHistory.Add(new ChatMessage(ChatRole.User,
                @$"Your are an intelligent, cheerful assistant who prioritizes answers to user questions using the data in this conversation. If you do not know the answer, say 'I don't know.'. 
                Answer the following question: 
                
                [Question]
                {userPrompt}

                Prioritize the following data to answer the question:
                [Data]
                {builder}
    "));

    // Stream the AI response and add to chat history
    Console.WriteLine("AI Response:");
    await foreach (var item in
        chatClient.CompleteStreamingAsync(chatHistory))
        {
            Console.Write(item.Text);
        }
    Console.WriteLine();
}
