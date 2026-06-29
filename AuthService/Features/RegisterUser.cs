using System.Text;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using AuthService.Domain;
using AuthService.EmailSender;
using FluentValidation;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Features;

public record RegisterUserRequest(string Email, string Password);

public class RegisterUserRequestValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6)
            .MaximumLength(100);
    }
}

[ApiController]
[Route("api/auth/registration")]
public sealed class RegisterUser : ControllerBase
{
    private readonly RegisterUserHandler _handler;
    
    public RegisterUser(RegisterUserHandler handler)
    {
        _handler = handler;
    }
    
    [HttpPost]
    public async Task<ActionResult<Guid>> Register([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _handler.Handle(request, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }
}

public sealed class RegisterUserHandler
{
    private const string ConfirmationGifUrl = "https://dperfusvywdyyhutzydq.supabase.co/storage/v1/object/public/gifs/7ec96a340acee2913e598014d18692de.gif";

    private readonly UserManager<User> _userManager;
    private readonly IValidator<RegisterUserRequest> _validator;
    private readonly IEmailSender _emailSender;

    public RegisterUserHandler(
        UserManager<User> userManager, 
        IEmailSender emailSender, 
        IValidator<RegisterUserRequest> validator)
    {
        _userManager = userManager;
        _validator = validator;
        _emailSender = emailSender;
    }

    public async Task<Result<Guid>> Handle(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var validationErrors = validationResult.Errors
                .Select(error => new Error(error.PropertyName, error.ErrorMessage));

            return Result<Guid>.Failure(validationErrors);
        }

        var user = new User
        {
            Email = request.Email,
            UserName = request.Email
        };
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(error => new Error(error.Code, error.Description));

            return Result<Guid>.Failure(errors);
        }

        
        var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(confirmationToken));

        var confirmationLink =
            "http://localhost:5126/api/auth/email-verification/"
            + "?userId=" + user.Id
            + "&token=" + encodedToken;
        
        
        
        
        var emailBody = BuildConfirmationEmailBody(confirmationLink);

        var emailResult = await _emailSender.SendAsync(new MailData(
            request.Email,
            "Confirm your AuthService account",
            emailBody));
        if (emailResult.IsFailure)
        {
            return Result<Guid>.Failure(emailResult.Errors);
        }

        return Result<Guid>.Success(user.Id);
    }

    private static string BuildConfirmationEmailBody(string confirmationLink)
    {
        var encodedLink = WebUtility.HtmlEncode(confirmationLink);

        return $$"""
                 <!doctype html>
                 <html lang="en">
                 <head>
                   <meta charset="utf-8">
                   <meta name="viewport" content="width=device-width, initial-scale=1">
                   <title>Confirm your email</title>
                 </head>
                 <body style="margin:0;padding:0;background:#f4f7fb;font-family:Arial,Helvetica,sans-serif;color:#1f2937;">
                   <div style="display:none;max-height:0;overflow:hidden;">
                     Confirm your email address to finish creating your AuthService account.
                   </div>
                   <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f4f7fb;padding:32px 16px;">
                     <tr>
                       <td align="center">
                         <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:560px;background:#ffffff;border-radius:16px;overflow:hidden;border:1px solid #e5e7eb;">
                           <tr>
                             <td style="background:#111827;padding:28px 32px;text-align:center;">
                               <div style="font-size:22px;font-weight:700;color:#ffffff;letter-spacing:.2px;">AuthService</div>
                               <div style="font-size:14px;color:#cbd5e1;margin-top:6px;">Email confirmation</div>
                             </td>
                           </tr>
                           <tr>
                             <td style="padding:32px 32px 18px;text-align:center;">
                               <img src="{{ConfirmationGifUrl}}" alt="Email confirmation" width="220" style="display:block;margin:0 auto 24px;max-width:220px;width:100%;height:auto;border:0;">
                               <h1 style="margin:0 0 12px;font-size:26px;line-height:1.25;color:#111827;">Confirm your email</h1>
                               <p style="margin:0;font-size:16px;line-height:1.6;color:#4b5563;">
                                 Thanks for signing up. Click the button below to activate your account.
                               </p>
                             </td>
                           </tr>
                           <tr>
                             <td align="center" style="padding:12px 32px 30px;">
                               <a href="{{encodedLink}}" style="display:inline-block;background:#2563eb;color:#ffffff;text-decoration:none;font-size:16px;font-weight:700;padding:14px 24px;border-radius:10px;">
                                 Confirm email
                               </a>
                             </td>
                           </tr>
                           <tr>
                             <td style="padding:0 32px 32px;">
                               <p style="margin:0 0 10px;font-size:13px;line-height:1.5;color:#6b7280;">
                                 If the button does not work, copy and paste this link into your browser:
                               </p>
                               <p style="margin:0;font-size:13px;line-height:1.5;word-break:break-all;color:#2563eb;">
                                 <a href="{{encodedLink}}" style="color:#2563eb;text-decoration:underline;">{{encodedLink}}</a>
                               </p>
                             </td>
                           </tr>
                         </table>
                       </td>
                     </tr>
                   </table>
                 </body>
                 </html>
                 """;
    }
}
