using CabService.NotificationService.DTOs;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Text.RegularExpressions;

namespace CabService.NotificationService.Services;

public class EmailService : IEmailService
{
    private readonly ISendGridClient _sendGridClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(
        ISendGridClient sendGridClient,
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _sendGridClient = sendGridClient;
        _configuration = configuration;
        _logger = logger;
        _fromEmail = _configuration["SendGrid:FromEmail"] ?? "noreply@cabservice.com";
        _fromName = _configuration["SendGrid:FromName"] ?? "Cab Service";
    }

    public async Task<bool> SendEmailAsync(EmailNotificationDto emailDto)
    {
        try
        {
            if (!await ValidateEmailAddressAsync(emailDto.To))
            {
                _logger.LogWarning("Invalid email address: {Email}", emailDto.To);
                return false;
            }

            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(emailDto.To);
            var subject = emailDto.Subject;
            var plainTextContent = emailDto.IsHtml ? null : emailDto.Body;
            var htmlContent = emailDto.IsHtml ? emailDto.Body : null;

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            // Add attachments if any
            if (emailDto.Attachments?.Any() == true)
            {
                foreach (var attachment in emailDto.Attachments)
                {
                    msg.AddAttachment(attachment.FileName, Convert.ToBase64String(attachment.Content), attachment.Type);
                }
            }

            // Add custom headers if any
            if (emailDto.Headers?.Any() == true)
            {
                foreach (var header in emailDto.Headers)
                {
                    msg.AddHeader(header.Key, header.Value);
                }
            }

            var response = await _sendGridClient.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully to {Email}", emailDto.To);
                return true;
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync();
                _logger.LogError("Failed to send email to {Email}. Status: {StatusCode}, Response: {Response}", 
                    emailDto.To, response.StatusCode, responseBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email}", emailDto.To);
            return false;
        }
    }

    public async Task<bool> SendBulkEmailAsync(IEnumerable<EmailNotificationDto> emailDtos)
    {
        try
        {
            var tasks = emailDtos.Select(SendEmailAsync);
            var results = await Task.WhenAll(tasks);
            
            var successCount = results.Count(r => r);
            var totalCount = results.Length;

            _logger.LogInformation("Bulk email send completed: {SuccessCount}/{TotalCount} successful", 
                successCount, totalCount);

            return successCount == totalCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk emails");
            return false;
        }
    }

    public Task<bool> ValidateEmailAddressAsync(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Task.FromResult(false);
            }

            // Basic email validation using regex
            var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            return Task.FromResult(emailRegex.IsMatch(email));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating email address: {Email}", email);
            return Task.FromResult(false);
        }
    }
}
