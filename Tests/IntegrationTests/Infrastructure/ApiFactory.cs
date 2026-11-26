using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
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

    private readonly IContainer mail = new ContainerBuilder("axllent/mailpit:v1.34.0")
        .WithPortBinding(1025, true)
        .WithPortBinding(8025, true)
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilHttpRequestIsSucceeded(request => request.ForPort(8025).ForPath("/readyz")))
        .Build();

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
        await Task.WhenAll(database.StartAsync(), storage.StartAsync(), mail.StartAsync());

        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
        await CreatePublicBucketAsync(scope.ServiceProvider.GetRequiredService<IAmazonS3>());
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await Task.WhenAll(database.DisposeAsync().AsTask(), storage.DisposeAsync().AsTask(), mail.DisposeAsync().AsTask());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Smtp:Host", mail.Hostname);
        builder.UseSetting("Smtp:Port", mail.GetMappedPublicPort(1025).ToString());
        builder.UseSetting("Smtp:StartTls", "false");
        builder.UseSetting("Smtp:From", "no-reply@curator.test");
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

    private sealed record MailSummary(string Id);
    private sealed record MailSearch(MailSummary[] Messages);
    private sealed record MailBody(string Text);

    public async Task<string> ReadCodeAsync(string email, int expectedMessages = 1)
    {
        using var http = new HttpClient { BaseAddress = new Uri($"http://{mail.Hostname}:{mail.GetMappedPublicPort(8025)}") };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while (true)
        {
            var query = Uri.EscapeDataString($"to:{email}");
            var result = await http.GetFromJsonAsync<MailSearch>($"/api/v1/search?query={query}", timeout.Token);
            if (result!.Messages.Length >= expectedMessages)
            {
                var body = await http.GetFromJsonAsync<MailBody>($"/api/v1/message/{result.Messages[0].Id}", timeout.Token);
                var code = Regex.Match(body!.Text, @"\b[0-9]{6}\b");
                Assert.True(code.Success);
                return code.Value;
            }
            await Task.Delay(100, timeout.Token);
        }
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
