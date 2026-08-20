using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Infrastructure.ExternalServices;
using EduFlowAI.AI.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI.Tests;

public class GeminiChatClientTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _responseBody;

        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }

        public CapturingHandler(HttpStatusCode status, string responseBody)
        {
            _status = status;
            _responseBody = responseBody;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_responseBody, System.Text.Encoding.UTF8, "application/json")
            };
        }
    }

    private static GeminiChatClient BuildClient(CapturingHandler handler, GeminiOptions? options = null)
    {
        var httpClient = new HttpClient(handler);
        var opts = Options.Create(options ?? new GeminiOptions
        {
            ApiKey = "test-key",
            ChatModel = "gemini-2.5-flash",
            BaseUrl = "https://example.test/v1beta/"
        });
        return new GeminiChatClient(httpClient, opts);
    }

    [Fact]
    public async Task GenerateAsync_BuildsUrlFromOptions()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK,
            "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"hi\"}]}}]}");
        var client = BuildClient(handler);

        await client.GenerateAsync("sys", "user");

        Assert.Equal(
            "https://example.test/v1beta/models/gemini-2.5-flash:generateContent?key=test-key",
            handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task GenerateAsync_SendsSystemInstructionAndContents()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK,
            "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"hi\"}]}}]}");
        var client = BuildClient(handler);

        await client.GenerateAsync("SYS-PROMPT", "USER-MSG");

        Assert.Contains("system_instruction", handler.LastBody);
        Assert.Contains("contents", handler.LastBody);
        Assert.Contains("SYS-PROMPT", handler.LastBody);
        Assert.Contains("USER-MSG", handler.LastBody);
    }

    [Fact]
    public async Task GenerateAsync_ReturnsFirstCandidateText()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK,
            "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"the answer\"}]}}]}");
        var client = BuildClient(handler);

        var result = await client.GenerateAsync("sys", "user");

        Assert.Equal("the answer", result);
    }

    [Fact]
    public async Task GenerateAsync_EmptyCandidates_ReturnsEmptyString()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, "{\"candidates\":[]}");
        var client = BuildClient(handler);

        var result = await client.GenerateAsync("sys", "user");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task GenerateAsync_NoCandidatesField_ReturnsEmptyString()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, "{}");
        var client = BuildClient(handler);

        var result = await client.GenerateAsync("sys", "user");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task GenerateAsync_NonSuccessStatus_Throws()
    {
        var handler = new CapturingHandler(HttpStatusCode.NotFound, "not found");
        var client = BuildClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GenerateAsync("sys", "user"));
    }
}
