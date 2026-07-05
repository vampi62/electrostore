using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

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
        if (!_configuration.GetValue<bool>("SMTP:Enable"))
        {
            _logger.LogDebug("SMTP disabled - e-mail ignored for {To}", to);
            return;
        }
        // Validate recipient address
        if (string.IsNullOrWhiteSpace(to))
        {
            _logger.LogError("Recipient e-mail address is null or empty");
            throw new ArgumentException("Recipient e-mail address cannot be null or empty", nameof(to));
        }
        if (to.EndsWith("@localhost", StringComparison.OrdinalIgnoreCase) ||
            to.EndsWith("@local", StringComparison.OrdinalIgnoreCase) ||
            to.EndsWith("@localhost.local", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("E-mail to {To} ignored because it is a local address", to);
            return;
        }
        // Validate sender address
        var from = _configuration["SMTP:From"] ?? _configuration["SMTP:Username"];
        if (string.IsNullOrWhiteSpace(from))
        {
            _logger.LogError("Sender e-mail address is null or empty");
            throw new InvalidOperationException("SMTP:From configuration is missing and no username is configured");
        }

        var host = _configuration["SMTP:Host"] ?? throw new InvalidOperationException("SMTP:Host is not configured");
        var port = _configuration.GetValue<int>("SMTP:Port", 587);
        var username = _configuration["SMTP:Username"] ?? throw new InvalidOperationException("SMTP:Username is not configured");
        var password = _configuration["SMTP:Password"] ?? throw new InvalidOperationException("SMTP:Password is not configured");

        // Port 465 = SSL implicite, other ports = STARTTLS
        var socketOptions = port == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = body };

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(host, port, socketOptions);
            if (!string.IsNullOrWhiteSpace(username))
            {
                await client.AuthenticateAsync(username, password);
            }
            await client.SendAsync(message);
            await client.DisconnectAsync(quit: true);
            _logger.LogInformation("E-mail sent to {To} with subject {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send e-mail to {To} with subject {Subject}", to, subject);
            throw;
        }
    }
}
