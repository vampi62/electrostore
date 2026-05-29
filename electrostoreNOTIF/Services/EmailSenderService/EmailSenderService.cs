using System.Net;
using System.Net.Mail;

namespace ElectrostoreNOTIF.Services.EmailSenderService;

public class EmailSenderService : IEmailSenderService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailSenderService> _logger;

    public EmailSenderService(IConfiguration configuration, ILogger<EmailSenderService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        if (!bool.TryParse(_configuration["SMTP:Enable"], out var isEnabled) || !isEnabled)
        {
            _logger.LogDebug("SMTP disabled — e-mail ignored for {To}", to);
            return;
        }

        var host = _configuration["SMTP:Host"] ?? throw new InvalidOperationException("SMTP:Host configuration is missing");
        var port = int.Parse(_configuration["SMTP:Port"] ?? "587");
        var username = _configuration["SMTP:Username"];
        var password = _configuration["SMTP:Password"];
        var from = _configuration["SMTP:From"] ?? username ?? "noreply@electrostore.local";

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true
        };

        var message = new MailMessage(from, to, subject, body) { IsBodyHtml = true };

        try
        {
            await client.SendMailAsync(message);
            _logger.LogInformation("E-mail sent to {To} with subject {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send e-mail to {To} with subject {Subject}", to, subject);
            throw;
        }
    }
}
