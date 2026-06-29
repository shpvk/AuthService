using Microsoft.AspNetCore.Mvc;
using AuthService.Domain;
using FluentValidation;
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
[Route("api/auth")]
public sealed class RegisterUser : ControllerBase
{
    private readonly RegisterUserHandler _handler;
    
    public RegisterUser(RegisterUserHandler handler)
    {
        _handler = handler;
    }
    
    [HttpPost]
    [Route("/registration")]
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
    private readonly UserManager<User> _userManager;

    public RegisterUserHandler(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<Guid>> Handle(RegisterUserRequest request, CancellationToken cancellationToken)
    {
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

        return Result<Guid>.Success(user.Id);
    }
}
