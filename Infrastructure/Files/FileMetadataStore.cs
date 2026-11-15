using Application.Files;
using Domain.Files;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Files;

internal sealed class FileMetadataStore(AppDbContext db) : IFileMetadataStore
{
    public async Task AddAsync(StoredFile file, CancellationToken cancellationToken)
    {
        db.Files.Add(file);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<StoredFile?> FindOwnedAsync(Guid fileId, Guid ownerId, CancellationToken cancellationToken) =>
        db.Files.AsNoTracking().SingleOrDefaultAsync(file => file.Id == fileId && file.OwnerId == ownerId, cancellationToken);
}
