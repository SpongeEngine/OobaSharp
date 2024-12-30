﻿using FluentAssertions;
using LocalAI.NET.Oobabooga.Client;
using LocalAI.NET.Oobabooga.Models;
using LocalAI.NET.Oobabooga.Models.Chat;
using LocalAI.NET.Oobabooga.Models.Common;
using LocalAI.NET.Oobabooga.Tests.Common;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;
using Xunit.Abstractions;

namespace LocalAI.NET.Oobabooga.Tests.Unit.Client
{
    public class OobaboogaStreamingTests : UnitTestBase
    {
        private readonly OobaboogaClient _client;

        public OobaboogaStreamingTests(ITestOutputHelper output) : base(output)
        {
            _client = new OobaboogaClient(new OobaboogaOptions
            {
                BaseUrl = BaseUrl
            }, Logger);
        }

        [Fact]
        public async Task StreamChatComplete_ShouldWork()
        {
            // Arrange
            var streamResponses = new[]
            {
                "data: {\"choices\": [{\"index\": 0, \"delta\": {\"role\": \"assistant\"}}]}\n\n",
                "data: {\"choices\": [{\"index\": 0, \"delta\": {\"content\": \"Hello\"}}]}\n\n",
                "data: {\"choices\": [{\"index\": 0, \"delta\": {\"content\": \" world\"}}]}\n\n",
                "data: {\"choices\": [{\"index\": 0, \"delta\": {\"content\": \"!\"}}]}\n\n",
                "data: [DONE]\n\n"
            };

            Server
                .Given(Request.Create()
                    .WithPath("/v1/chat/completions")
                    .WithBody(body => body.Contains("\"stream\":true"))
                    .UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithBody(string.Join("", streamResponses))
                    .WithHeader("Content-Type", "text/event-stream"));

            // Act
            var messages = new List<OobaboogaChatMessage> 
            { 
                new() { Role = "user", Content = "Hi" } 
            };
            var receivedMessages = new List<OobaboogaChatMessage>();
        
            await foreach (var message in _client.StreamChatCompletionAsync(messages))
            {
                receivedMessages.Add(message);
            }

            // Assert
            receivedMessages.Should().HaveCount(3);
            string.Concat(receivedMessages.Select(m => m.Content))
                .Should().Be("Hello world!");
        }

        [Fact]
        public async Task StreamCompletion_WithStopSequence_ShouldWork()
        {
            // Arrange
            var tokens = new[] { "Hello", " world", "!" };
            var streamResponses = tokens.Select(token => 
                $"data: {{\"choices\": [{{\"text\": \"{token}\"}}]}}\n\n");

            Server
                .Given(Request.Create()
                    .WithPath("/v1/completions")
                    .WithBody(body => body.Contains("\"stop\":[\".\""))
                    .UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithBody(string.Join("", streamResponses))
                    .WithHeader("Content-Type", "text/event-stream"));

            // Act
            var receivedTokens = new List<string>();
            await foreach (var token in _client.StreamCompletionAsync(
                "Test prompt",
                new OobaboogaCompletionOptions { StopSequences = new[] { "." } }))
            {
                receivedTokens.Add(token);
            }

            // Assert
            receivedTokens.Should().BeEquivalentTo(tokens);
        }

        [Fact]
        public async Task StreamChatComplete_WithError_ShouldFail()
        {
            // Arrange
            Server
                .Given(Request.Create()
                    .WithPath("/v1/chat/completions")
                    .WithBody(body => body.Contains("\"stream\":true"))
                    .UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(500)
                    .WithBody("Internal error"));

            // Act & Assert
            var messages = new List<OobaboogaChatMessage> 
            { 
                new() { Role = "user", Content = "Hi" } 
            };

            // Using Func<Task> for async assertions
            Func<Task> act = async () => 
            {
                await foreach (var _ in _client.StreamChatCompletionAsync(messages))
                {
                    // Should throw before yielding any results
                }
            };

            // Use Should().ThrowAsync<T>() for async operations
            await act.Should().ThrowAsync<OobaboogaException>();
        }
    }
}