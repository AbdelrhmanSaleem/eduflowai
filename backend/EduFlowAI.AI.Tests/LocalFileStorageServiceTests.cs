using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using EduFlowAI.AI.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI.Tests;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _root;
    private readonly LocalFileStorageService _storage;

    public LocalFileStorageServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "eduflow-storage-tests", Guid.NewGuid().ToString("N"));
        _storage = new LocalFileStorageService(Options.Create(new FileStorageOptions { Root = _root }));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task SaveThenOpenRead_RoundTripsContent()
    {
        var key = "knowledge-base/doc-1.pdf";
        await _storage.SaveAsync(new MemoryStream(Encoding.UTF8.GetBytes("%PDF hello")), key);

        Assert.True(_storage.Exists(key));

        await using var read = await _storage.OpenReadAsync(key);
        using var reader = new StreamReader(read);
        Assert.Equal("%PDF hello", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task Delete_RemovesFile()
    {
        var key = "knowledge-base/doc-2.pdf";
        await _storage.SaveAsync(new MemoryStream(new byte[] { 1, 2, 3 }), key);
        Assert.True(_storage.Exists(key));

        await _storage.DeleteAsync(key);

        Assert.False(_storage.Exists(key));
    }

    [Fact]
    public async Task Delete_MissingKey_IsNoOp()
    {
        await _storage.DeleteAsync("knowledge-base/never-existed.pdf"); // must not throw
    }

    [Fact]
    public async Task Save_PathTraversalKey_IsRejected()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _storage.SaveAsync(new MemoryStream(new byte[] { 1 }), "../escape.pdf"));
    }

    // Uploads used to default under the build output, so a rebuild deleted every file.
    [Fact]
    public void An_unconfigured_root_is_not_inside_the_build_output()
    {
        var storage = new LocalFileStorageService(
            Options.Create(new FileStorageOptions()));

        var key = $"knowledge-base/{Guid.NewGuid()}.md";
        storage.SaveAsync(new MemoryStream("hello"u8.ToArray()), key).GetAwaiter().GetResult();

        try
        {
            Assert.True(storage.Exists(key));

            var stored = ResolvedPath(storage, key);
            Assert.False(
                stored.StartsWith(AppContext.BaseDirectory, StringComparison.OrdinalIgnoreCase),
                $"knowledge-base files must not live under the build output, but got: {stored}");
        }
        finally
        {
            storage.DeleteAsync(key).GetAwaiter().GetResult();
        }
    }

    // Hosting needs to point this at a mounted volume, so an explicit absolute root must win.
    [Fact]
    public void An_absolute_configured_root_is_used_as_given()
    {
        var custom = Path.Combine(Path.GetTempPath(), $"eduflow-storage-{Guid.NewGuid():N}");

        try
        {
            var storage = new LocalFileStorageService(
                Options.Create(new FileStorageOptions { Root = custom }));

            var key = "knowledge-base/doc.md";
            storage.SaveAsync(new MemoryStream("hello"u8.ToArray()), key).GetAwaiter().GetResult();

            Assert.True(File.Exists(Path.Combine(custom, "knowledge-base", "doc.md")));
        }
        finally
        {
            if (Directory.Exists(custom))
                Directory.Delete(custom, recursive: true);
        }
    }

    private static string ResolvedPath(LocalFileStorageService storage, string key)
    {
        var root = (string)storage
            .GetType()
            .GetField("_root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(storage)!;

        return Path.GetFullPath(Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar)));
    }
}
