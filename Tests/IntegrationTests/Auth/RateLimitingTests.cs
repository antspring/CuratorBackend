using System.Net;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Auth;

public class RateLimitingTests(StrictRateLimitApiFactory factory) : IClassFixture<StrictRateLimitApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Login_OverLimit_ReturnsTooManyRequestsWithRetryAfter()
    {
        for (var attempt = 0; attempt < StrictRateLimitApiFactory.PermitLimit; attempt++)
        {
            await client.LoginAsync(ApiClient.NewEmail());
        }

        var response = await client.LoginAsync(ApiClient.NewEmail());

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.NotNull(response.Headers.RetryAfter?.Delta);
        var problem = await response.ReadAsync<ProblemDto>();
        Assert.Equal("rate_limit_exceeded", problem.Code);
        Assert.False(string.IsNullOrEmpty(problem.Type));
        Assert.False(string.IsNullOrEmpty(problem.TraceId));
    }
}
