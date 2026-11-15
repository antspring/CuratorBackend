using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace IntegrationTests.Infrastructure;

public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string StorageBucket = "test-avatars";

    private const int StoragePort = 9000;
    private const string StorageAccessKey = "test-access-key";
    private const string StorageSecretKey = "test-secret-key";

    private readonly PostgreSqlContainer database = new PostgreSqlBuilder("postgres:18").Build();

    private readonly IContainer storage = new ContainerBuilder("rustfs/rustfs:1.0.5")
        .WithEnvironment("RUSTFS_ACCESS_KEY", StorageAccessKey)
        .WithEnvironment("RUSTFS_SECRET_KEY", StorageSecretKey)
        .WithPortBinding(StoragePort, true)
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilHttpRequestIsSucceeded(request => request.ForPort(StoragePort).ForPath("/health")))
        .Build();

    protected virtual int AuthPermitLimit => 1000;

    public string StorageUrl => $"http://{storage.Hostname}:{storage.GetMappedPublicPort(StoragePort)}";

    public async Task InitializeAsync()
    {
        await Task.WhenAll(database.StartAsync(), storage.StartAsync());

        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
        await CreatePublicBucketAsync(scope.ServiceProvider.GetRequiredService<IAmazonS3>());
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await Task.WhenAll(database.DisposeAsync().AsTask(), storage.DisposeAsync().AsTask());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Database", database.GetConnectionString());
        builder.UseSetting("Jwt:SigningKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        builder.UseSetting("RateLimiting:Auth:PermitLimit", AuthPermitLimit.ToString());
        builder.UseSetting("FileStorage:ServiceUrl", StorageUrl);
        builder.UseSetting("FileStorage:Region", "us-east-1");
        builder.UseSetting("FileStorage:Bucket", StorageBucket);
        builder.UseSetting("FileStorage:AccessKey", StorageAccessKey);
        builder.UseSetting("FileStorage:SecretKey", StorageSecretKey);
        builder.UseSetting("FileStorage:PublicBaseUrl", $"{StorageUrl}/{StorageBucket}");
        builder.UseSetting("FileStorage:ForcePathStyle", "true");
    }

    private static async Task CreatePublicBucketAsync(IAmazonS3 s3)
    {
        await s3.PutBucketAsync(new PutBucketRequest { BucketName = StorageBucket, UseClientRegion = false });
        await s3.PutBucketPolicyAsync(StorageBucket, $$"""
            {
              "Version": "2012-10-17",
              "Statement": [{
                "Effect": "Allow",
                "Principal": { "AWS": ["*"] },
                "Action": ["s3:GetObject"],
                "Resource": ["arn:aws:s3:::{{StorageBucket}}/*"]
              }]
            }
            """);
    }
}

public sealed class StrictRateLimitApiFactory : ApiFactory
{
    public const int PermitLimit = 2;

    protected override int AuthPermitLimit => PermitLimit;
}
