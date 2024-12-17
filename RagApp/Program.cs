using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Qdrant.Client;
using Shared;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

// Create Ollama and Qdrant clients
var qClient = new QdrantClient("localhost");

Console.WriteLine("Ask a question. This bot is grounded in Zelda data due to RAG, so it's good at those topics.");

IEmbeddingGenerator<string, Embedding<float>> generator =
    new OllamaEmbeddingGenerator(new Uri("http://localhost:11434/"), "nomic-embed-text");

IChatClient chatClient =
    new OllamaChatClient(new Uri("http://localhost:11434/"), "phi3:mini");

while (true)
{
    // Start the conversation with context for the AI model
    List<ChatMessage> chatHistory = new();

    // Get user prompt and add to chat history
    Console.WriteLine();
    var userPrompt = Console.ReadLine();

    var promptEmbedding = (await generator.GenerateAsync(new List<string>() { userPrompt }))[0].Vector.ToArray();

    var returnedLocations = await qClient.QueryAsync(
        collectionName: "zelda-database",
        query: promptEmbedding,
        limit: 25
    );

    var builder = new StringBuilder();
    foreach (var location in returnedLocations)
    {
        builder.AppendLine($"{location.Payload["name"].StringValue}: {location.Payload["description"].StringValue}.");
    }
    
    // Use this for generic chat
    //chatHistory.Add(new ChatMessage(ChatRole.User, userPrompt));

    // Use this for grounded chat
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
