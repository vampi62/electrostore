using System.Net;
using System.Net.Mail;

namespace electrostore.Services.SmtpService;

public class SmtpService : ISmtpService
{
    private readonly IConfiguration _configuration;
    private readonly SmtpClient _smtpClient;

    public SmtpService(IConfiguration configuration)
    {
        _configuration = configuration;
        _smtpClient = bool.TryParse(_configuration["SMTP:Enable"], out var isEnabled) && isEnabled
            ? new SmtpClient(_configuration["SMTP:Host"])
            {
                Port = int.Parse(_configuration["SMTP:Port"] ?? "587"),
                Credentials = new NetworkCredential(_configuration["SMTP:Username"], _configuration["SMTP:Password"]),
                EnableSsl = true
            }
            : null!;
    }
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        // check if SMTP is Enable
        if (!bool.TryParse(_configuration["SMTP:Enable"], out var isEnabled) || !isEnabled)
        {
            return;
        }
        var smtpUsername = _configuration["SMTP:Username"];
        if (string.IsNullOrWhiteSpace(smtpUsername))
        {
            throw new ArgumentException("SMTP:Username configuration value is missing or empty.");
        }
        var mailMessage = new MailMessage
        {
            From = new MailAddress(smtpUsername),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        mailMessage.To.Add(to);
        // send email
        var sendEmailTask = SendMailAsync(mailMessage);
        try
        {
            await sendEmailTask;
        }
        catch (SmtpException ex)
        {
            // client error
            throw new InvalidOperationException("Failed to send email. Please check the email address and try again.", ex);
        }
        catch (Exception ex)
        {
            // other errors
            throw new InvalidOperationException("An error occurred while sending the email.", ex);
        }
    }
    
    public virtual Task SendMailAsync(MailMessage message) 
    {
        if (_smtpClient == null)
        {
            throw new InvalidOperationException("SMTP client is not configured.");
        }
        return _smtpClient.SendMailAsync(message);
    }
}