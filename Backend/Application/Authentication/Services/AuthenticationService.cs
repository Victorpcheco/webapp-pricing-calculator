using Application.Authentication.Commands;
using Application.Common;
using Application.Repositories;
using Domain.Common;
using Domain.Users;
using Domain.Users.Events;

namespace Application.Authentication.Services;

public class AuthenticationService : IScopedService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IEventPublisher _eventPublisher;

    public AuthenticationService(
        IUserRepository userRepository, 
        IPasswordHasher passwordHasher, 
        IJwtTokenGenerator jwtTokenGenerator,
        IEventPublisher eventPublisher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<Guid>> RegisterAsync(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (existingUser != null)
        {
            return Result<Guid>.Failure("Email já está em uso");
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            return Result<Guid>.Failure("Dados inválidos");
        }

        var passwordHash = _passwordHasher.Hash(command.Password);

        var userResult = User.Create(command.Name, command.Phone, command.Email, passwordHash);
        if (userResult.IsFailure)
        {
            return Result<Guid>.Failure(userResult.Error);
        }

        var user = userResult.Value;
        
        await _userRepository.AddAsync(user, cancellationToken);
        await _eventPublisher.PublishAsync(new UsuarioRegistradoEvent(user.Id, user.Email), cancellationToken);

        return Result<Guid>.Success(user.Id);
    }

    public async Task<Result<AuthenticationResult>> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user == null)
        {
            await _eventPublisher.PublishAsync(new FalhaLoginUsuarioEvent(command.Email, "Usuário ou senha inválidos"), cancellationToken);
            return Result<AuthenticationResult>.Failure("Usuário ou senha inválidos");
        }

        var isPasswordValid = _passwordHasher.Verify(command.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            await _eventPublisher.PublishAsync(new FalhaLoginUsuarioEvent(command.Email, "Usuário ou senha inválidos"), cancellationToken);
            return Result<AuthenticationResult>.Failure("Usuário ou senha inválidos");
        }

        var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Email);
        await _eventPublisher.PublishAsync(new UsuarioLogadoEvent(user.Id, user.Email), cancellationToken);

        return Result<AuthenticationResult>.Success(new AuthenticationResult(user.Id, token));
    }
}
