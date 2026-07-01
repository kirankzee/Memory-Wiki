using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using MemoryWiki.Application.Common.Interfaces;
using MemoryWiki.Domain.Entities;
using MemoryWiki.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MemoryWiki.Infrastructure.Storage;

/// <summary>
/// S3-compatible object storage (MinIO / AWS S3). Treats "/"-delimited keys as a
/// virtual filesystem so the memory tree can be browsed with ls/cat/grep.
/// </summary>
public sealed class S3ObjectStorage(IAmazonS3 client, IOptions<StorageOptions> options, ILogger<S3ObjectStorage> logger)
    : IObjectStorage, ITranscriptContentReader
{
    private readonly StorageOptions _opt = options.Value;

    public async Task EnsureBucketAsync(CancellationToken ct = default)
    {
        var exists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(client, _opt.Bucket);
        if (!exists)
        {
            await client.PutBucketAsync(new PutBucketRequest { BucketName = _opt.Bucket }, ct);
            logger.LogInformation("Created bucket {Bucket}.", _opt.Bucket);
        }
    }

    public async Task UploadTextAsync(string key, string content, string contentType = "text/markdown", CancellationToken ct = default)
    {
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _opt.Bucket,
            Key = key,
            ContentBody = content,
            ContentType = contentType
        }, ct);
    }

    public async Task<StoredObject?> DownloadTextAsync(string key, CancellationToken ct = default)
    {
        try
        {
            using var response = await client.GetObjectAsync(_opt.Bucket, key, ct);
            using var reader = new StreamReader(response.ResponseStream, Encoding.UTF8);
            var content = await reader.ReadToEndAsync(ct);
            return new StoredObject(key, content, response.ContentLength, response.LastModified);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await client.GetObjectMetadataAsync(_opt.Bucket, key, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<MemoryNode>> ListAsync(string prefix, CancellationToken ct = default)
    {
        var request = new ListObjectsV2Request
        {
            BucketName = _opt.Bucket,
            Prefix = prefix,
            Delimiter = "/"
        };

        var nodes = new List<MemoryNode>();
        ListObjectsV2Response response;
        do
        {
            response = await client.ListObjectsV2Async(request, ct);

            // Sub-directories (common prefixes).
            foreach (var cp in response.CommonPrefixes)
            {
                var name = cp.TrimEnd('/').Split('/').Last();
                nodes.Add(new MemoryNode(name, "/" + cp.TrimEnd('/'), true, 0, null));
            }

            // Files at this level.
            foreach (var obj in response.S3Objects.Where(o => o.Key != prefix))
            {
                var name = obj.Key.Split('/').Last();
                if (string.IsNullOrEmpty(name)) continue;
                nodes.Add(new MemoryNode(name, "/" + obj.Key, false, obj.Size, obj.LastModified));
            }

            request.ContinuationToken = response.NextContinuationToken;
        } while (response.IsTruncated);

        return nodes;
    }

    public async Task<IReadOnlyList<string>> ListAllKeysAsync(string prefix, CancellationToken ct = default)
    {
        var request = new ListObjectsV2Request { BucketName = _opt.Bucket, Prefix = prefix };
        var keys = new List<string>();
        ListObjectsV2Response response;
        do
        {
            response = await client.ListObjectsV2Async(request, ct);
            keys.AddRange(response.S3Objects.Select(o => o.Key));
            request.ContinuationToken = response.NextContinuationToken;
        } while (response.IsTruncated);
        return keys;
    }

    // ITranscriptContentReader
    public async Task<string?> ReadAsync(string objectKey, CancellationToken ct = default)
        => (await DownloadTextAsync(objectKey, ct))?.Content;
}
