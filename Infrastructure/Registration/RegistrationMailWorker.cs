using System.Net;
using System.Net.Mail;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Registration;

internal sealed class RegistrationMailWorker(
    IServiceScopeFactory scopes,
    IOptions<SmtpOptions> smtp,
    VerificationCodes codes,
    TimeProvider clock,
    ILogger<RegistrationMailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nextCleanup = DateTimeOffset.MinValue;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = clock.GetUtcNow();
                if (now >= nextCleanup)
                {
                    var cutoff = now - PendingRegistration.RetentionAfterExpiry;
                    await db.Set<PendingRegistration>().Where(row => row.ExpiresAt <= cutoff).ExecuteDeleteAsync(stoppingToken);
                    nextCleanup = now.AddMinutes(1);
                }

                if (await DeliverNextAsync(db, now, stoppingToken))
                {
                    continue;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning("Не удалось обработать очередь подтверждения почты ({ErrorType}).", exception.GetType().Name);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task<bool> DeliverNextAsync(AppDbContext db, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var pending = db.Set<PendingRegistration>();
        var claimed = await pending.FromSqlInterpolated($"""
            UPDATE pending_registrations SET delivery_due_at = {now.AddSeconds(45)}
            WHERE verification_id = (
                SELECT verification_id FROM pending_registrations
                WHERE delivery_due_at <= {now} AND expires_at > {now} AND attempts_left > 0
                ORDER BY delivery_due_at LIMIT 1 FOR UPDATE SKIP LOCKED
            )
            RETURNING *
            """).AsNoTracking().ToListAsync(cancellationToken);
        if (claimed.Count == 0)
        {
            return false;
        }

        var row = claimed[0];
        var options = smtp.Value;
        using var client = new SmtpClient(options.Host, options.Port) { EnableSsl = options.StartTls };
        if (!string.IsNullOrEmpty(options.Username))
        {
            client.Credentials = new NetworkCredential(options.Username, options.Password);
        }

        using var message = new MailMessage
        {
            From = new MailAddress(options.From),
            Subject = "Подтверждение почты в Кураторе",
            Body = $"Ваш код подтверждения: {codes.Get(row.CodeNonce)}. Код действует 10 минут с момента запроса.",
        };
        message.To.Add(new MailAddress(row.Email));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        await client.SendMailAsync(message, timeout.Token);
        await pending.Where(item => item.VerificationId == row.VerificationId && item.CodeNonce == row.CodeNonce)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.DeliveryDueAt, (DateTimeOffset?)null), cancellationToken);
        return true;
    }
}
