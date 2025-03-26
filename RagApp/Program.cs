// using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Qdrant.Client;
// using Shared;
// using System.Net.Http.Json;
using System.Text;
// using System.Text.Json;

#region createClients
var qClient = new QdrantClient("localhost");

var folder_name = "v4";

IEmbeddingGenerator<string, Embedding<float>> generator =
    new OllamaEmbeddingGenerator(new Uri("http://localhost:11434/"), "nomic-embed-text");

IChatClient chatClient =
    new OllamaChatClient(new Uri("http://localhost:11434/"), "phi3:mini");

Console.WriteLine("Ask a question. This bot is grounded in Orlade offers due to RAG, so it's good at those topics.");
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
        collectionName: $"orlade-offers-database-{folder_name}",
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
        builder.AppendLine($"Thème : {location.Payload["context"].StringValue} \nTitre : {location.Payload["title"].StringValue} \nTexte : {location.Payload["text_content"].StringValue}.");
    }

    Console.WriteLine(builder);

    // Assemble the full prompt to the chat AI model using instructions,
    // the original user prompt, and the retrieved relevant data// Your answers must be a maximum of 300 characters.


    chatHistory.Add(new ChatMessage(ChatRole.User,
                @$"You are an intelligent, cheerful assistant who prioritizes answers using the data provided in this conversation.
                You help the Orlade company create new offers based on their old ones. 
                Orlade is a consulting company working with RTE on various projects. 
                'we' is the Orlade compagny.
                Your objectifs are to create texte similar to the DATA in order to win new offers related to the Specifications.

                If you do not know the answer, say 'I don't know.'

                Answer the following question:

                [Question]
                {userPrompt}

                Prioritize the following data to answer the question:
                [DATA]
                {builder}"

));

    // Stream the AI response and add to chat history
    Console.WriteLine("AI Response:");
    await foreach (var item in
        chatClient.CompleteStreamingAsync(chatHistory))
        {
            Console.Write(item.Text);
        }
    Console.WriteLine();
}
