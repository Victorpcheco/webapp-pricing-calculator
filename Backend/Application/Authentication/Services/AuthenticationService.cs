using Application.Authentication.Commands;
using Application.Common;
using Application.Repositories;
using Domain.Common;
using Domain.Entities.Users;
using Domain.Entities.Users.Events;

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
        var usuarioExistente = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (usuarioExistente != null)
        {
            return Result<Guid>.Failure("Email já está em uso");
        }

        if (string.IsNullOrWhiteSpace(command.SenhaHash))
        {
            return Result<Guid>.Failure("Dados inválidos");
        }

        var senhaHash = _passwordHasher.Hash(command.SenhaHash);

        var userResult = User.Create(command.Nome, command.Telefone, command.Email, senhaHash);
        if (userResult.IsFailure)
        {
            return Result<Guid>.Failure(userResult.Error);
        }

        var usuario = userResult.Value;
        
        await _userRepository.AddAsync(usuario, cancellationToken);
        await _eventPublisher.PublishAsync(new UsuarioRegistradoEvent(usuario.Id, usuario.Email), cancellationToken);

        return Result<Guid>.Success(usuario.Id);
    }

    public async Task<Result<AuthenticationResult>> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var usuario = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (usuario == null)
        {
            await _eventPublisher.PublishAsync(new FalhaLoginUsuarioEvent(command.Email, "Usuário ou senha inválidos"), cancellationToken);
            return Result<AuthenticationResult>.Failure("Usuário ou senha inválidos");
        }

        var isPasswordValid = _passwordHasher.Verify(command.SenhaHash, usuario.SenhaHash);
        if (!isPasswordValid)
        {
            await _eventPublisher.PublishAsync(new FalhaLoginUsuarioEvent(command.Email, "Usuário ou senha inválidos"), cancellationToken);
            return Result<AuthenticationResult>.Failure("Usuário ou senha inválidos");
        }

        var token = _jwtTokenGenerator.GenerateToken(usuario.Id, usuario.Email, usuario.Nome);
        await _eventPublisher.PublishAsync(new UsuarioLogadoEvent(usuario.Id, usuario.Email), cancellationToken);

        return Result<AuthenticationResult>.Success(new AuthenticationResult(usuario.Id, token));
    }
}
