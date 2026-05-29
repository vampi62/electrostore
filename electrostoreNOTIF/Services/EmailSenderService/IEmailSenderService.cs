

namespace ElectrostoreNOTIF.Services.EmailSenderService;

public interface IEmailSenderService
{
    public Task SendAsync(string to, string subject, string body);
}