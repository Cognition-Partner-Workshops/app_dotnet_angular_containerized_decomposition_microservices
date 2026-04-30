using System.Text;
using KnowledgeAgent.Core.Interfaces;
using KnowledgeAgent.Core.Models;
using Microsoft.Extensions.Logging;

namespace KnowledgeAgent.Infrastructure.Services;

public class DocumentProcessor : IDocumentProcessor
{
    private readonly ILogger<DocumentProcessor> _logger;

    private static readonly HashSet<string> SupportedTextTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/plain",
        "text/markdown",
        "text/csv",
        "text/html",
        "application/json",
        "application/xml"
    };

    private static readonly HashSet<string> SupportedDocumentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/msword"
    };

    public DocumentProcessor(ILogger<DocumentProcessor> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExtractTextAsync(
        Stream documentStream, string contentType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Extracting text from document with content type: {ContentType}", contentType);

        if (SupportedTextTypes.Contains(contentType))
        {
            return await ExtractPlainTextAsync(documentStream);
        }

        if (SupportedDocumentTypes.Contains(contentType))
        {
            return await ExtractFromDocumentAsync(documentStream, contentType);
        }

        _logger.LogWarning("Unsupported content type: {ContentType}. Attempting plain text extraction.", contentType);
        return await ExtractPlainTextAsync(documentStream);
    }

    public IEnumerable<DocumentChunk> ChunkDocument(
        string text, Guid knowledgeArticleId, int maxChunkSize = 1000, int overlapSize = 200)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        var sentences = SplitIntoSentences(text);
        var currentChunk = new StringBuilder();
        var chunkIndex = 0;
        var previousChunkEnd = string.Empty;

        foreach (var sentence in sentences)
        {
            if (currentChunk.Length + sentence.Length > maxChunkSize && currentChunk.Length > 0)
            {
                var chunkContent = currentChunk.ToString().Trim();
                yield return new DocumentChunk
                {
                    Id = Guid.NewGuid(),
                    KnowledgeArticleId = knowledgeArticleId,
                    Content = chunkContent,
                    ChunkIndex = chunkIndex,
                    TokenCount = EstimateTokenCount(chunkContent),
                    Metadata = new Dictionary<string, string>
                    {
                        ["chunkIndex"] = chunkIndex.ToString(),
                        ["articleId"] = knowledgeArticleId.ToString()
                    }
                };

                chunkIndex++;
                previousChunkEnd = GetOverlapText(currentChunk.ToString(), overlapSize);
                currentChunk.Clear();
                currentChunk.Append(previousChunkEnd);
            }

            currentChunk.Append(sentence);
            currentChunk.Append(' ');
        }

        if (currentChunk.Length > 0)
        {
            var finalContent = currentChunk.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(finalContent))
            {
                yield return new DocumentChunk
                {
                    Id = Guid.NewGuid(),
                    KnowledgeArticleId = knowledgeArticleId,
                    Content = finalContent,
                    ChunkIndex = chunkIndex,
                    TokenCount = EstimateTokenCount(finalContent),
                    Metadata = new Dictionary<string, string>
                    {
                        ["chunkIndex"] = chunkIndex.ToString(),
                        ["articleId"] = knowledgeArticleId.ToString()
                    }
                };
            }
        }
    }

    private static async Task<string> ExtractPlainTextAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private async Task<string> ExtractFromDocumentAsync(Stream stream, string contentType)
    {
        // For PDF, DOCX, XLSX, PPTX - use a simplified text extraction approach
        // In production, integrate with libraries like iTextSharp, DocumentFormat.OpenXml, etc.
        _logger.LogInformation("Processing document type: {ContentType}", contentType);

        return contentType switch
        {
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" =>
                await ExtractFromDocxAsync(stream),
            "application/pdf" =>
                await ExtractFromPdfAsync(stream),
            _ => await ExtractPlainTextAsync(stream)
        };
    }

    private static async Task<string> ExtractFromDocxAsync(Stream stream)
    {
        // Simplified DOCX extraction - reads XML content from the document
        // For production, use DocumentFormat.OpenXml or similar library
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        try
        {
            using var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Read);
            var documentEntry = archive.GetEntry("word/document.xml");
            if (documentEntry == null) return string.Empty;

            using var entryStream = documentEntry.Open();
            using var reader = new StreamReader(entryStream);
            var xml = await reader.ReadToEndAsync();

            // Strip XML tags to get plain text
            return StripXmlTags(xml);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static async Task<string> ExtractFromPdfAsync(Stream stream)
    {
        // Simplified PDF text extraction
        // For production, use a PDF library like iTextSharp or PdfPig
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var content = await reader.ReadToEndAsync();

        // Basic PDF text extraction - extract text between stream markers
        var textBuilder = new StringBuilder();
        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("Tj") || line.Contains("TJ"))
            {
                var cleaned = line.Replace("Tj", "").Replace("TJ", "").Trim('(', ')', '[', ']', ' ');
                if (!string.IsNullOrWhiteSpace(cleaned))
                    textBuilder.AppendLine(cleaned);
            }
        }

        return textBuilder.Length > 0 ? textBuilder.ToString() : content;
    }

    private static string StripXmlTags(string xml)
    {
        var result = new StringBuilder();
        var inTag = false;

        foreach (var ch in xml)
        {
            if (ch == '<') { inTag = true; continue; }
            if (ch == '>') { inTag = false; result.Append(' '); continue; }
            if (!inTag) result.Append(ch);
        }

        // Clean up extra whitespace
        return System.Text.RegularExpressions.Regex.Replace(result.ToString(), @"\s+", " ").Trim();
    }

    private static IEnumerable<string> SplitIntoSentences(string text)
    {
        var sentenceEnders = new[] { '.', '!', '?', '\n' };
        var currentSentence = new StringBuilder();

        foreach (var ch in text)
        {
            currentSentence.Append(ch);

            if (sentenceEnders.Contains(ch) && currentSentence.Length > 1)
            {
                yield return currentSentence.ToString().Trim();
                currentSentence.Clear();
            }
        }

        if (currentSentence.Length > 0)
            yield return currentSentence.ToString().Trim();
    }

    private static string GetOverlapText(string text, int overlapSize)
    {
        if (text.Length <= overlapSize) return text;
        return text[^overlapSize..];
    }

    private static int EstimateTokenCount(string text)
    {
        // Rough estimation: ~4 characters per token
        return text.Length / 4;
    }
}
