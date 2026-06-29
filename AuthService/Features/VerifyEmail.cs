using System.Text;
using AuthService.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace AuthService.Features;

public record VerifyEmailRequest(Guid UserId, string Token);

[ApiController]
[Route("api/auth/email-verification")]
public sealed class VerifyEmailController : ControllerBase
{
    private readonly VerifyEmailHandler _handler;

    public VerifyEmailController(VerifyEmailHandler handler)
    {
        _handler = handler;
    }
    
    [HttpGet]
    public async Task<Result<Guid>> VerifyEmail([FromQuery] VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        return await _handler.Handle(request, cancellationToken);
    }
}

public sealed class VerifyEmailHandler
{
    private readonly UserManager<User> _userManager;

    public VerifyEmailHandler(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<Guid>> Handle(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());

        if (user is null)
        {
            return Result<Guid>.Failure([new Error("User", "User not found")]);
        }

        var result =
            await _userManager.ConfirmEmailAsync(user,
                Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token)));
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(error => new Error(error.Code, error.Description));

            return Result<Guid>.Failure(errors);
        }

        return Result<Guid>.Success(user.Id);
    }
}
