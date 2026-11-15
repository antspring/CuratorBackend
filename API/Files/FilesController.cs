using API.Authentication;
using API.Errors;
using Application.Files;
using Domain.Files;
using Microsoft.AspNetCore.Mvc;

namespace API.Files;

[ApiController]
[Route("api/v1/files")]
public sealed class FilesController(FileService fileService) : ControllerBase
{
    private const long MaxRequestBytes = StoredFile.MaxSizeBytes + 64 * 1024;

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    [RejectOversizedRequest(MaxRequestBytes)]
    [ProducesResponseType<FileResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<FileResponse>> Upload([FromForm] UploadFileRequest request, CancellationToken cancellationToken)
    {
        await using var content = request.File.OpenReadStream();

        var command = new UploadFileCommand(
            User.GetUserId(),
            request.Purpose,
            request.File.FileName,
            request.File.Length,
            content);

        var result = await fileService.UploadAsync(command, cancellationToken);

        return result switch
        {
            UploadFileResult.Uploaded uploaded =>
                Created($"/api/v1/files/{uploaded.File.Id}", FileResponse.From(uploaded.File)),
            UploadFileResult.Empty =>
                this.FieldErrors("file", ["Файл пуст."]),
            UploadFileResult.TooLarge =>
                this.ProblemWithCode(StatusCodes.Status413PayloadTooLarge, ErrorCodes.FileTooLarge, "Файл больше 10 MiB."),
            UploadFileResult.UnsupportedType =>
                this.ProblemWithCode(StatusCodes.Status415UnsupportedMediaType, ErrorCodes.UnsupportedMediaType, "Допустимы только изображения JPEG и PNG."),
            _ => throw new InvalidOperationException($"Неизвестный результат загрузки: {result.GetType().Name}"),
        };
    }

    [HttpGet("{fileId:guid}")]
    [ProducesResponseType<FileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileResponse>> Get(Guid fileId, CancellationToken cancellationToken)
    {
        var file = await fileService.FindOwnedAsync(fileId, User.GetUserId(), cancellationToken);

        return file is null
            ? this.ProblemWithCode(StatusCodes.Status404NotFound, ErrorCodes.ResourceNotFound, "Файл не найден.")
            : Ok(FileResponse.From(file));
    }
}
