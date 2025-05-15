using electrostore.Dto;

namespace electrostore.Services.SMTPService;

public interface ISMTPService
{
    public Task SendEmailAsync(string to, string subject, string body);
}