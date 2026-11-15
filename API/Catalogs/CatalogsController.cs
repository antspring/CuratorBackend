using System.ComponentModel.DataAnnotations;
using API.Common;
using API.Errors;
using Application.Catalogs;
using Microsoft.AspNetCore.Mvc;

namespace API.Catalogs;

[ApiController]
[Route("api/v1/catalogs")]
public sealed class CatalogsController(CatalogService catalogService) : ControllerBase
{
    [HttpGet("{kind}")]
    [ProducesResponseType<PageResponse<LookupResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PageResponse<LookupResponse>>> List(
        string kind,
        [FromQuery(Name = "q")] [StringLength(150)] string? search,
        [FromQuery(Name = "limit")] [Range(PageResponse.MinLimit, PageResponse.MaxLimit)] int limit = PageResponse.DefaultLimit,
        [FromQuery(Name = "cursor")] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var result = await catalogService.ListAsync(new ListCatalogQuery(kind, search, limit, cursor), cancellationToken);

        return result switch
        {
            ListCatalogResult.Found found => Ok(new PageResponse<LookupResponse>(
                found.Page.Items.Select(entry => new LookupResponse(entry.Id, entry.Name)).ToList(),
                found.Page.NextCursor)),
            ListCatalogResult.UnknownCatalog =>
                this.ProblemWithCode(StatusCodes.Status404NotFound, ErrorCodes.ResourceNotFound, $"Справочник '{kind}' не найден."),
            ListCatalogResult.InvalidCursor =>
                this.ProblemWithCode(StatusCodes.Status400BadRequest, ErrorCodes.InvalidCursor, "Курсор повреждён или относится к другим фильтрам."),
            _ => throw new InvalidOperationException($"Неизвестный результат чтения справочника: {result.GetType().Name}"),
        };
    }
}
