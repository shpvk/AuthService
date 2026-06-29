using AuthService.Features;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace AuthService.EmailSender;

public class EmailSender : IEmailSender
{
    private readonly MailOptions _mailOptions;

    public EmailSender(IOptions<MailOptions> mailOptions)
    {
        _mailOptions = mailOptions.Value;
    }

    public async Task<Result<Unit>> SendAsync(MailData mailData)
    {
        var mail = new MimeMessage();

        mail.From.Add(new MailboxAddress(_mailOptions.FromDisplayName, _mailOptions.From));

        var tryParse = MailboxAddress.TryParse(mailData.Email, out var mailAddress);
        if (!tryParse)
        {
            return Result<Unit>
                .Failure([new Error("InvalidEmail", "Email address is invalid.")]);
        }
        
        mail.To.Add(mailAddress!);

        var body = new BodyBuilder()
        {
            HtmlBody = mailData.Body
        };

        mail.Body = body.ToMessageBody();
        mail.Subject = mailData.Subject;

        using var client = new SmtpClient();

        await client.ConnectAsync(
            _mailOptions.Host, 
            _mailOptions.Port, 
            SecureSocketOptions.StartTls);
        
        await client.AuthenticateAsync(_mailOptions.UserName, _mailOptions.Password);
        await client.SendAsync(mail);
        await client.DisconnectAsync(true);

        return Result<Unit>.Success(Unit.Value);
    }
}
