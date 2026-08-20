using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.Documents.Application.Interfaces;

namespace EduFlowAI.Documents.Infrastructure.Storage
{
    public class FileStorageService : IFileStorageService
    {
        // Define the storage path and the 10 MB size limit
        private readonly string _storageDirectory;
        private const int MaxFileSizeBytes = 10 * 1024 * 1024;

        // Map allowed extensions to their true binary signatures (magic bytes)
        private static readonly Dictionary<string, byte[][]> AllowedMagicBytes = new()
        {
            { ".pdf", new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } } },
            { ".jpg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".png", new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } }
        };

        public FileStorageService(IOptions<DocumentStorageOptions> options)
        {
            _storageDirectory = options.Value.Root;

            // Ensure the folder exists on startup
            if (!Directory.Exists(_storageDirectory))
            {
                Directory.CreateDirectory(_storageDirectory);
            }
        }

        public async Task<string> SaveFileAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            // 1. Basic Validation (Empty or too large)
            if (file == null || file.Length == 0)
                throw new ArgumentException("File cannot be null or empty.");

            if (file.Length > MaxFileSizeBytes)
                throw new InvalidOperationException("File size exceeds the 10 MB limit.");

            // 2. Extension Validation
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedMagicBytes.ContainsKey(extension))
                throw new InvalidOperationException("Invalid file type. Only PDF, JPG, JPEG, and PNG are allowed.");

            // 3. Magic Bytes (Signature) Validation
            using var stream = file.OpenReadStream();
            if (!await IsValidFileSignatureAsync(stream, extension, cancellationToken))
                throw new InvalidOperationException("File signature mismatch. The file might be corrupted or spoofed.");

            // Reset stream position back to 0 after reading the signature so we can save the whole file
            stream.Position = 0;

            // 4. Generate Secure Name and Save
            var secureFileName = $"{Guid.NewGuid()}{extension}";
            var fullFilePath = Path.Combine(_storageDirectory, secureFileName);

            using var fileStream = new FileStream(fullFilePath, FileMode.CreateNew, FileAccess.Write);
            await stream.CopyToAsync(fileStream, cancellationToken);

            return secureFileName;
        }

        //public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
        //{
        //    cancellationToken.ThrowIfCancellationRequested();
        //    var fullPath = ResolveStoragePath(storageKey);
        //    Stream stream = new FileStream(
        //        fullPath,
        //        FileMode.Open,
        //        FileAccess.Read,
        //        FileShare.Read,
        //        bufferSize: 64 * 1024,
        //        options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        //    return Task.FromResult(stream);
        //}

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fullPath = ResolveStoragePath(storageKey);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }

        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(storageKey))
            {
                throw new ArgumentException(
                    "Storage key cannot be empty.",
                    nameof(storageKey));
            }

            var safeFileName = Path.GetFileName(storageKey);

            if (!string.Equals(
                    safeFileName,
                    storageKey,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Invalid storage key.");
            }

            var fullFilePath = Path.Combine(_storageDirectory, safeFileName);

            if (!File.Exists(fullFilePath))
            {
                throw new FileNotFoundException(
                    "The requested file was not found.");
            }

            Stream stream = new FileStream(
                fullFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                options:
                    FileOptions.Asynchronous |
                    FileOptions.SequentialScan);

            return Task.FromResult(stream);
        }


        /// Reads the first few bytes of the file to guarantee it matches its stated extension.
        private async Task<bool> IsValidFileSignatureAsync(Stream stream, string extension, CancellationToken cancellationToken)
        {
            var allowedSignatures = AllowedMagicBytes[extension];
            var maxSignatureLength = allowedSignatures.Max(s => s.Length);

            var headerBytes = new byte[maxSignatureLength];
            await stream.ReadAsync(headerBytes, 0, headerBytes.Length, cancellationToken);

            return allowedSignatures.Any(signature =>
                headerBytes.Take(signature.Length).SequenceEqual(signature));
        }

        private string ResolveStoragePath(string storageKey)
        {
            if (string.IsNullOrWhiteSpace(storageKey))
                throw new ArgumentException("Storage key cannot be empty.", nameof(storageKey));

            // Older rows may contain the absolute path returned by the original implementation.
            var candidate = Path.IsPathRooted(storageKey)
                ? Path.GetFullPath(storageKey)
                : Path.GetFullPath(Path.Combine(_storageDirectory, storageKey));
            var root = Path.GetFullPath(_storageDirectory) + Path.DirectorySeparatorChar;

            if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("The storage key is outside secure storage.");

            return candidate;
        }
    }
}