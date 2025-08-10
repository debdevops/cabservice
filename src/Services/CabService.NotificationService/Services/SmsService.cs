using CabService.NotificationService.DTOs;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using System.Text.RegularExpressions;

namespace CabService.NotificationService.Services;

public class SmsService : ISmsService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmsService> _logger;
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromPhoneNumber;

    public SmsService(
        IConfiguration configuration,
        ILogger<SmsService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _accountSid = _configuration["Twilio:AccountSid"] ?? throw new ArgumentNullException("Twilio:AccountSid");
        _authToken = _configuration["Twilio:AuthToken"] ?? throw new ArgumentNullException("Twilio:AuthToken");
        _fromPhoneNumber = _configuration["Twilio:FromPhoneNumber"] ?? throw new ArgumentNullException("Twilio:FromPhoneNumber");

        TwilioClient.Init(_accountSid, _authToken);
    }

    public async Task<bool> SendSmsAsync(SmsNotificationDto smsDto)
    {
        try
        {
            if (!await ValidatePhoneNumberAsync(smsDto.To))
            {
                _logger.LogWarning("Invalid phone number: {PhoneNumber}", smsDto.To);
                return false;
            }

            var message = await MessageResource.CreateAsync(
                body: smsDto.Message,
                from: new PhoneNumber(_fromPhoneNumber),
                to: new PhoneNumber(smsDto.To)
            );

            if (message.Status == MessageResource.StatusEnum.Queued || 
                message.Status == MessageResource.StatusEnum.Sent ||
                message.Status == MessageResource.StatusEnum.Delivered)
            {
                _logger.LogInformation("SMS sent successfully to {PhoneNumber}. Message SID: {MessageSid}", 
                    smsDto.To, message.Sid);
                return true;
            }
            else
            {
                _logger.LogError("Failed to send SMS to {PhoneNumber}. Status: {Status}, Error: {ErrorMessage}", 
                    smsDto.To, message.Status, message.ErrorMessage);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", smsDto.To);
            return false;
        }
    }

    public async Task<bool> SendBulkSmsAsync(IEnumerable<SmsNotificationDto> smsDtos)
    {
        try
        {
            var tasks = smsDtos.Select(SendSmsAsync);
            var results = await Task.WhenAll(tasks);
            
            var successCount = results.Count(r => r);
            var totalCount = results.Length;

            _logger.LogInformation("Bulk SMS send completed: {SuccessCount}/{TotalCount} successful", 
                successCount, totalCount);

            return successCount == totalCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk SMS messages");
            return false;
        }
    }

    public Task<bool> ValidatePhoneNumberAsync(string phoneNumber)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return Task.FromResult(false);
            }

            // Basic phone number validation - should start with + and contain only digits
            var phoneRegex = new Regex(@"^\+[1-9]\d{1,14}$");
            return Task.FromResult(phoneRegex.IsMatch(phoneNumber));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating phone number: {PhoneNumber}", phoneNumber);
            return Task.FromResult(false);
        }
    }
}
