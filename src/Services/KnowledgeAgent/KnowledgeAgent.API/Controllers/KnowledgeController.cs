using KnowledgeAgent.API.DTOs;
using KnowledgeAgent.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeAgent.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KnowledgeController : ControllerBase
{
    private readonly IKnowledgeArticleService _knowledgeArticleService;
    private readonly ILogger<KnowledgeController> _logger;

    public KnowledgeController(
        IKnowledgeArticleService knowledgeArticleService,
        ILogger<KnowledgeController> logger)
    {
        _knowledgeArticleService = knowledgeArticleService;
        _logger = logger;
    }

    [HttpPost("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchKnowledge(
        [FromBody] SearchRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return BadRequest("Query cannot be empty");

        _logger.LogInformation("Knowledge search request: {Query}", request.Query);

        var results = await _knowledgeArticleService.SearchKnowledgeAsync(
            request.Query, request.TopK, cancellationToken);

        return Ok(results);
    }

    [HttpGet("articles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllArticles(CancellationToken cancellationToken)
    {
        var articles = await _knowledgeArticleService.GetAllAsync(cancellationToken);
        return Ok(articles);
    }

    [HttpGet("articles/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArticle(Guid id, CancellationToken cancellationToken)
    {
        var article = await _knowledgeArticleService.GetByIdAsync(id, cancellationToken);

        if (article == null)
            return NotFound();

        return Ok(article);
    }
}
