using System;
using System.Text.Json.Serialization;

namespace Shared
{
    public class OfferRecord
    {
        [JsonPropertyName("context")]
        public string Context { get; set; }

        [JsonPropertyName("name_offer")]
        public string NameOffer { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        // [JsonPropertyName("id_text_area")]
        // public Guid IdTextArea { get; set; }

        [JsonPropertyName("text_content")]
        public string TextContent { get; set; }
        public float[] Embedding {get; set;}
    }
}
