using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _root;

    public LocalFileStorageService(IOptions<FileStorageOptions> options)
    {
        var configured = options?.Value?.Root;

        // Nothing configured: keep uploads out of the build output, which a rebuild deletes.
        _root = string.IsNullOrWhiteSpace(configured)
            ? DefaultRoot()
            : Path.IsPathRooted(configured)
                ? Path.GetFullPath(configured)
                : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configured));
    }

    // Per-user data, outside the repo and bin, so uploads survive a rebuild with no configuration.
    private static string DefaultRoot()
    {
        var localAppData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);

        return string.IsNullOrWhiteSpace(localAppData)
            ? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "protected-files"))
            : Path.Combine(localAppData, "EduFlowAI", "protected-files");
    }

    public async Task<string> SaveAsync(Stream content, string relativeKey, CancellationToken cancellationToken = default)
    {
        var fullPath = Resolve(relativeKey);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        if (content.CanSeek)
            content.Position = 0;
        await content.CopyToAsync(fs, cancellationToken);

        return relativeKey;
    }

    public Task<Stream> OpenReadAsync(string relativeKey, CancellationToken cancellationToken = default)
    {
        var fullPath = Resolve(relativeKey);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string relativeKey, CancellationToken cancellationToken = default)
    {
        var fullPath = Resolve(relativeKey);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public bool Exists(string relativeKey) => File.Exists(Resolve(relativeKey));

    private string Resolve(string relativeKey)
    {
        if (string.IsNullOrWhiteSpace(relativeKey))
            throw new ArgumentException("Storage key cannot be null or empty.", nameof(relativeKey));

        var combined = Path.GetFullPath(Path.Combine(_root, relativeKey));

        // Stop keys like "../../secret" escaping the root.
        var rootWithSep = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;
        if (!combined.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Resolved storage path escapes the storage root.");

        return combined;
    }
}
