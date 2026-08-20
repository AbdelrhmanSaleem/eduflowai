using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.Documents.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI.Infrastructure.Storage;

public class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3FileStorageService(IAmazonS3 s3Client, IOptions<DocumentStorageOptions> options)
    {
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        _bucketName = options?.Value?.BucketName ?? "eduflowai-document-storage";
    }

    public async Task<string> SaveAsync(Stream content, string relativeKey, CancellationToken cancellationToken = default)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        if (string.IsNullOrWhiteSpace(relativeKey))
            throw new ArgumentException("Storage key cannot be empty.", nameof(relativeKey));

        if (content.CanSeek)
            content.Position = 0;

        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = relativeKey,
            InputStream = content,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
        };

        await _s3Client.PutObjectAsync(putRequest, cancellationToken);
        return relativeKey;
    }

    public async Task<Stream> OpenReadAsync(string relativeKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativeKey))
            throw new ArgumentException("Storage key cannot be empty.", nameof(relativeKey));

        try
        {
            var response = await _s3Client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = relativeKey
            }, cancellationToken);

            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new FileNotFoundException($"The requested file '{relativeKey}' was not found in S3 bucket '{_bucketName}'.", ex);
        }
    }

    public async Task DeleteAsync(string relativeKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativeKey))
            return;

        await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = relativeKey
        }, cancellationToken);
    }

    public bool Exists(string relativeKey)
    {
        if (string.IsNullOrWhiteSpace(relativeKey))
            return false;

        try
        {
            var metadata = _s3Client.GetObjectMetadataAsync(_bucketName, relativeKey).ConfigureAwait(false).GetAwaiter().GetResult();
            return metadata != null;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
