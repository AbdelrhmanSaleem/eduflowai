using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Infrastructure.ExternalServices;
using EduFlowAI.AI.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI.Tests;

public class GeminiChatClientFallbackTests
{
    private const string OkBody = "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"ok\"}]}}]}";

    // Returns a scripted response per call and records every request URL, so a test can assert
    // which model and key each attempt used.
    private sealed class ScriptedHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode Status, string Body)> _responses;

        public List<Uri> Requests { get; } = new();

        public ScriptedHandler(params (HttpStatusCode Status, string Body)[] responses)
        {
            _responses = new Queue<(HttpStatusCode, string)>(responses);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);

            if (_responses.Count == 0)
                throw new InvalidOperationException("The client made more requests than the test scripted.");

            var (status, body) = _responses.Dequeue();
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }

    private static GeminiChatClient Build(ScriptedHandler handler, GeminiOptions options) =>
        new(new HttpClient(handler), Options.Create(options));

    private static GeminiOptions Opts(List<string> models, List<string>? keys = null) => new()
    {
        ApiKey = "k1",
        ApiKeys = keys ?? new List<string>(),
        ChatModels = models,
        BaseUrl = "https://example.test/v1beta/"
    };

    [Fact]
    public async Task Unavailable_model_falls_through_to_the_next()
    {
        var handler = new ScriptedHandler(
            (HttpStatusCode.NotFound, "no such model"),
            (HttpStatusCode.OK, OkBody));
        var client = Build(handler, Opts(new() { "bad-model", "good-model" }));

        var result = await client.GenerateAsync("sys", "user");

        Assert.Equal("ok", result);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("models/bad-model:", handler.Requests[0].ToString());
        Assert.Contains("models/good-model:", handler.Requests[1].ToString());
    }

    [Fact]
    public async Task A_rejected_key_moves_to_the_next_key_on_the_same_model()
    {
        var handler = new ScriptedHandler(
            (HttpStatusCode.Forbidden, "key disabled"),
            (HttpStatusCode.OK, OkBody));
        var client = Build(handler, Opts(new() { "only-model" }, keys: new() { "keyA", "keyB" }));

        var result = await client.GenerateAsync("sys", "user");

        Assert.Equal("ok", result);
        Assert.EndsWith("key=keyA", handler.Requests[0].ToString());
        Assert.EndsWith("key=keyB", handler.Requests[1].ToString());
    }

    [Fact]
    public async Task A_server_blip_retries_the_same_combo_then_succeeds()
    {
        var handler = new ScriptedHandler(
            (HttpStatusCode.InternalServerError, "blip"),
            (HttpStatusCode.OK, OkBody));
        var client = Build(handler, Opts(new() { "only-model" }));

        var result = await client.GenerateAsync("sys", "user");

        Assert.Equal("ok", result);
        Assert.All(handler.Requests, uri => Assert.Contains("models/only-model:", uri.ToString()));
    }

    [Fact]
    public async Task An_exhausted_model_gives_way_to_the_next_after_retrying()
    {
        var handler = new ScriptedHandler(
            (HttpStatusCode.TooManyRequests, "quota"),
            (HttpStatusCode.TooManyRequests, "quota"),
            (HttpStatusCode.OK, OkBody));
        var client = Build(handler, Opts(new() { "spent-model", "fresh-model" }));

        var result = await client.GenerateAsync("sys", "user");

        Assert.Equal("ok", result);
        Assert.EndsWith(":generateContent?key=k1", handler.Requests[^1].ToString());
        Assert.Contains("models/fresh-model:", handler.Requests[^1].ToString());
    }

    [Fact]
    public async Task All_combinations_failing_throws_naming_every_model_tried()
    {
        var handler = new ScriptedHandler(
            (HttpStatusCode.NotFound, "no"),
            (HttpStatusCode.NotFound, "no"));
        var client = Build(handler, Opts(new() { "model-a", "model-b" }));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GenerateAsync("sys", "user"));

        Assert.Contains("model-a", ex.Message);
        Assert.Contains("model-b", ex.Message);
    }

    // Extraction shares GenerateFromDocumentAsync, so it must inherit the same fallback.
    [Fact]
    public async Task Document_extraction_uses_the_fallback_too()
    {
        var handler = new ScriptedHandler(
            (HttpStatusCode.InternalServerError, "blip"),
            (HttpStatusCode.InternalServerError, "blip"),
            (HttpStatusCode.OK, OkBody));
        var client = Build(handler, Opts(new() { "first-model", "second-model" }));

        var result = await client.GenerateFromDocumentAsync("transcribe", new byte[] { 1, 2, 3 }, "application/pdf");

        Assert.Equal("ok", result);
        Assert.Contains("models/second-model:", handler.Requests[^1].ToString());
    }
}
