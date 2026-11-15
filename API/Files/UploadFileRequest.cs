using System.ComponentModel.DataAnnotations;
using Domain.Files;
using Microsoft.AspNetCore.Mvc;

namespace API.Files;

public sealed class UploadFileRequest
{
    [Required]
    [AllowedValues(FilePurposes.Avatar, ErrorMessage = "Допустимое назначение файла: avatar.")]
    [FromForm(Name = "purpose")]
    public string Purpose { get; init; } = null!;

    [Required]
    [FromForm(Name = "file")]
    public IFormFile File { get; init; } = null!;
}
