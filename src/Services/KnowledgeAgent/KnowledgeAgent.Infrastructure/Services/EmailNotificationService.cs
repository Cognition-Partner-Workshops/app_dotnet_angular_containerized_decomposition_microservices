using System.Net;
using System.Net.Mail;
using System.Text;
using KnowledgeAgent.Core.Interfaces;
using KnowledgeAgent.Core.Models;
using KnowledgeAgent.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeAgent.Infrastructure.Services;

public class EmailNotificationService : IEmailNotificationService
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        IOptions<EmailOptions> options,
        ILogger<EmailNotificationService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAlertWithKnowledgeAsync(
        AppInsightAlert alert,
        IEnumerable<SearchResult> relevantArticles,
        IEnumerable<string> recipients,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending alert email for alert: {AlertId}", alert.AlertId);

        var subject = $"[Knowledge Agent Alert] {alert.AlertName} - {alert.Severity}";
        var body = BuildAlertEmailBody(alert, relevantArticles);
        var recipientList = recipients.Any() ? recipients.ToList() : _options.DefaultRecipients;

        await SendEmailAsync(subject, body, recipientList, cancellationToken);
    }

    public async Task SendDigestEmailAsync(
        IEnumerable<AppInsightAlert> alerts,
        IEnumerable<string> recipients,
        CancellationToken cancellationToken = default)
    {
        var alertList = alerts.ToList();
        _logger.LogInformation("Sending digest email with {Count} alerts", alertList.Count);

        var subject = $"[Knowledge Agent] Alert Digest - {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";
        var body = BuildDigestEmailBody(alertList);
        var recipientList = recipients.Any() ? recipients.ToList() : _options.DefaultRecipients;

        await SendEmailAsync(subject, body, recipientList, cancellationToken);
    }

    private async Task SendEmailAsync(
        string subject, string body, List<string> recipients, CancellationToken cancellationToken)
    {
        try
        {
            using var smtpClient = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
            {
                Credentials = new NetworkCredential(_options.SmtpUsername, _options.SmtpPassword),
                EnableSsl = _options.UseSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            foreach (var recipient in recipients)
            {
                mailMessage.To.Add(recipient);
            }

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation("Email sent successfully to {Count} recipients", recipients.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email notification");
            throw;
        }
    }

    private static string BuildAlertEmailBody(AppInsightAlert alert, IEnumerable<SearchResult> articles)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><style>");
        sb.AppendLine("body { font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }");
        sb.AppendLine(".container { max-width: 800px; margin: auto; background: white; border-radius: 8px; padding: 30px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
        sb.AppendLine(".alert-header { background: #d32f2f; color: white; padding: 15px 20px; border-radius: 6px; margin-bottom: 20px; }");
        sb.AppendLine(".section { margin: 20px 0; padding: 15px; background: #f8f9fa; border-radius: 6px; border-left: 4px solid #1976d2; }");
        sb.AppendLine(".article { margin: 10px 0; padding: 12px; background: white; border: 1px solid #e0e0e0; border-radius: 4px; }");
        sb.AppendLine(".score { color: #388e3c; font-weight: bold; }");
        sb.AppendLine("h2 { color: #1976d2; }");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine("<div class='container'>");

        // Alert Header
        sb.AppendLine($"<div class='alert-header'>");
        sb.AppendLine($"<h1>Application Insight Alert</h1>");
        sb.AppendLine($"<p><strong>Alert:</strong> {alert.AlertName}</p>");
        sb.AppendLine($"<p><strong>Severity:</strong> {alert.Severity}</p>");
        sb.AppendLine($"<p><strong>Time:</strong> {alert.FiredAt:yyyy-MM-dd HH:mm:ss} UTC</p>");
        sb.AppendLine("</div>");

        // Alert Details
        sb.AppendLine("<div class='section'>");
        sb.AppendLine("<h2>Alert Details</h2>");
        sb.AppendLine($"<p><strong>Description:</strong> {alert.Description}</p>");
        if (!string.IsNullOrEmpty(alert.ExceptionType))
            sb.AppendLine($"<p><strong>Exception Type:</strong> {alert.ExceptionType}</p>");
        if (!string.IsNullOrEmpty(alert.ExceptionMessage))
            sb.AppendLine($"<p><strong>Message:</strong> {alert.ExceptionMessage}</p>");
        if (!string.IsNullOrEmpty(alert.AffectedResource))
            sb.AppendLine($"<p><strong>Affected Resource:</strong> {alert.AffectedResource}</p>");
        sb.AppendLine("</div>");

        // Knowledge Articles
        var articleList = articles.ToList();
        if (articleList.Count > 0)
        {
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<h2>Related Knowledge Articles</h2>");
            sb.AppendLine("<p>The following knowledge articles may help resolve this issue:</p>");

            foreach (var article in articleList)
            {
                sb.AppendLine("<div class='article'>");
                sb.AppendLine($"<h3>{article.Title}</h3>");
                sb.AppendLine($"<p class='score'>Relevance Score: {article.Score:P0}</p>");
                sb.AppendLine($"<p>{TruncateContent(article.Content, 300)}</p>");
                sb.AppendLine("</div>");
            }

            sb.AppendLine("</div>");
        }
        else
        {
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<h2>Knowledge Articles</h2>");
            sb.AppendLine("<p>No directly relevant knowledge articles were found for this alert.</p>");
            sb.AppendLine("</div>");
        }

        sb.AppendLine("<hr><p style='color: #666; font-size: 12px;'>This email was generated by the SharePoint Knowledge Agent. " +
                      "Knowledge articles are sourced from your SharePoint document library.</p>");
        sb.AppendLine("</div></body></html>");

        return sb.ToString();
    }

    private static string BuildDigestEmailBody(List<AppInsightAlert> alerts)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><style>");
        sb.AppendLine("body { font-family: 'Segoe UI', Arial, sans-serif; padding: 20px; }");
        sb.AppendLine(".container { max-width: 800px; margin: auto; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; }");
        sb.AppendLine("th, td { padding: 10px; text-align: left; border-bottom: 1px solid #ddd; }");
        sb.AppendLine("th { background-color: #1976d2; color: white; }");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine("<div class='container'>");
        sb.AppendLine($"<h1>Alert Digest - {alerts.Count} Alerts</h1>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Time</th><th>Alert</th><th>Severity</th><th>Description</th></tr>");

        foreach (var alert in alerts)
        {
            sb.AppendLine($"<tr><td>{alert.FiredAt:HH:mm:ss}</td><td>{alert.AlertName}</td>" +
                          $"<td>{alert.Severity}</td><td>{TruncateContent(alert.Description, 100)}</td></tr>");
        }

        sb.AppendLine("</table></div></body></html>");
        return sb.ToString();
    }

    private static string TruncateContent(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;
        return content.Length <= maxLength ? content : content[..maxLength] + "...";
    }
}
