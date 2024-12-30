﻿using Newtonsoft.Json;

namespace LocalAI.NET.Oobabooga.Models.Chat
{
    public class OobaboogaChatCompletionResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("object")]
        public string Object { get; set; } = "chat.completion";

        [JsonProperty("created")]
        public long Created { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; } = string.Empty;

        [JsonProperty("choices")]
        public List<ChatCompletionChoice> Choices { get; set; } = new();
    }

    public class ChatCompletionChoice
    {
        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("message")]
        public OobaboogaChatMessage Message { get; set; } = new();

        [JsonProperty("finish_reason")]
        public string? FinishReason { get; set; }
    }
}