using Domain.Files;

namespace Application.Files;

public interface IFileMetadataStore
{
    Task AddAsync(StoredFile file, CancellationToken cancellationToken);

    Task<StoredFile?> FindOwnedAsync(Guid fileId, Guid ownerId, CancellationToken cancellationToken);
}
