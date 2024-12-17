using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;

public class OllamaResponse
{
    [JsonPropertyName("embedding")]
    public float[] Embedding { get; set; }
}