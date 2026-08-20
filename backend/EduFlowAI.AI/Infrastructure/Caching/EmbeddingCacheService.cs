using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace EduFlowAI.AI.Infrastructure.Caching;

public class EmbeddingCacheService
{
    private readonly Dictionary<string, float[]> _cache = new();
    private readonly object _lockObject = new();

    public bool TryGetEmbedding(string text, out float[]? embedding)
    {
        lock (_lockObject)
        {
            var hash = ComputeHash(text);
            return _cache.TryGetValue(hash, out embedding);
        }
    }

    public void CacheEmbedding(string text, float[] embedding)
    {
        lock (_lockObject)
        {
            var hash = ComputeHash(text);
            if (!_cache.ContainsKey(hash))
            {
                _cache[hash] = embedding;
            }
        }
    }

    private static string ComputeHash(string text)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}