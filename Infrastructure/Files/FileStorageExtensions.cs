using Amazon.Runtime;
using Amazon.S3;
using Application.Files;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Files;

internal static class FileStorageExtensions
{
    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FileStorageOptions>()
            .Bind(configuration.GetSection(FileStorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IAmazonS3>(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<FileStorageOptions>>().Value;

            return new AmazonS3Client(
                new BasicAWSCredentials(settings.AccessKey, settings.SecretKey),
                new AmazonS3Config
                {
                    ServiceURL = settings.ServiceUrl,
                    AuthenticationRegion = settings.Region,
                    ForcePathStyle = settings.ForcePathStyle,
                    RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                    ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
                });
        });

        services.AddSingleton<IFileStorage, S3FileStorage>();

        return services;
    }
}
