using AuthService.Features;

namespace AuthService.EmailSender;

public interface IEmailSender
{
    Task<Result<Unit>> SendAsync(MailData mailData);
}