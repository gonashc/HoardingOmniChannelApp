using FluentValidation;
using Hoarding.Application.Common.Interfaces;
using Hoarding.Domain.Entities;
using Hoarding.Domain.Enums;
using MediatR;

namespace Hoarding.Application.Features.Auth.Commands;

// ============================================================
// REGISTER USER - UC-33
// ============================================================
public record RegisterUserCommand(
    string Email,
    string Phone,
    string Password,
    string FullName,
    string Role,
    string? CompanyName,
    string? Gstin
) : IRequest<AuthResult>;

public record AuthResult(Guid UserId, string Email, string FullName, string Role, string Token);

public class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Phone).NotEmpty().Matches(@"^[+]?[0-9]{10,15}$");
        RuleFor(x => x.Password)
            .NotEmpty().MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain a number.");
        RuleFor(x => x.FullName).NotEmpty().MinimumLength(2);
        RuleFor(x => x.Role).Must(r => Enum.TryParse<UserRole>(r, true, out _))
            .WithMessage("Invalid role.");
    }
}

public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, AuthResult>
{
    private readonly IUserRepository _userRepo;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IUnitOfWork _uow;

    public RegisterUserHandler(IUserRepository userRepo, IPasswordHasher hasher, IJwtTokenGenerator jwt, IUnitOfWork uow)
    {
        _userRepo = userRepo;
        _hasher = hasher;
        _jwt = jwt;
        _uow = uow;
    }

    public async Task<AuthResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var existing = await _userRepo.GetByEmailAsync(request.Email, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException("Email is already registered.");

        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            Phone = request.Phone,
            PasswordHash = _hasher.Hash(request.Password),
            FullName = request.FullName,
            Role = Enum.Parse<UserRole>(request.Role, true),
            Profile = new UserProfile
            {
                CompanyName = request.CompanyName,
                Gstin = request.Gstin
            }
        };

        await _userRepo.AddAsync(user, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var token = _jwt.GenerateToken(user);
        return new AuthResult(user.Id, user.Email, user.FullName, user.Role.ToString(), token);
    }
}

// ============================================================
// LOGIN - UC-33
// ============================================================
public record LoginCommand(string EmailOrPhone, string Password) : IRequest<AuthResult>;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.EmailOrPhone).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginHandler : IRequestHandler<LoginCommand, AuthResult>
{
    private readonly IUserRepository _userRepo;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IUnitOfWork _uow;

    public LoginHandler(IUserRepository userRepo, IPasswordHasher hasher, IJwtTokenGenerator jwt, IUnitOfWork uow)
    {
        _userRepo = userRepo;
        _hasher = hasher;
        _jwt = jwt;
        _uow = uow;
    }

    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = request.EmailOrPhone.Contains('@')
            ? await _userRepo.GetByEmailAsync(request.EmailOrPhone, cancellationToken)
            : await _userRepo.GetByPhoneAsync(request.EmailOrPhone, cancellationToken);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");
        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is inactive.");
        if (!_hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        user.LastLoginAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var token = _jwt.GenerateToken(user);
        return new AuthResult(user.Id, user.Email, user.FullName, user.Role.ToString(), token);
    }
}
