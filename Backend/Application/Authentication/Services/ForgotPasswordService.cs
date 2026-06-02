using Application.Authentication.Commands;
using Application.Common;
using Application.Repositories;
using Domain.Common;
using Domain.Entities.Users.Events;

namespace Application.Authentication.Services;

public class ForgotPasswordService : IScopedService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly IPasswordResetTokenService _tokenService;
    private readonly IEventPublisher _eventPublisher;

    public ForgotPasswordService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        IPasswordResetTokenService tokenService,
        IEventPublisher eventPublisher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _tokenService = tokenService;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<bool>> RequestPasswordResetAsync(RequestPasswordResetCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        
        if (user == null)
        {
            await _eventPublisher.PublishAsync(new FalhaResetSenhaEvent(command.Email, "E-mail não encontrado na solicitação de reset"), cancellationToken);
            return Result<bool>.Failure("E-mail não encontrado");
        }

        var token = await _tokenService.GenerateTokenAsync(user.Id, cancellationToken);
        
        await _emailService.SendPasswordResetEmailAsync(user.Email, token, cancellationToken);
        
        await _eventPublisher.PublishAsync(new SenhaResetSolicitadoEvent(user.Email), cancellationToken);

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ResetPasswordAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);

        if (user == null)
        {
            await _eventPublisher.PublishAsync(new FalhaResetSenhaEvent(command.Email, "E-mail não encontrado na conclusão de reset"), cancellationToken);
            return Result<bool>.Failure("E-mail não encontrado");
        }

        try
        {
            var isTokenValid = await _tokenService.ValidateTokenAsync(user.Id, command.Token, cancellationToken);

            if (!isTokenValid)
            {
                await _eventPublisher.PublishAsync(new FalhaResetSenhaEvent(command.Email, "Código de recuperação inválido"), cancellationToken);
                return Result<bool>.Failure("Código de recuperação inválido");
            }
        }
        catch (ArgumentException ex) when (ex.Message.Contains("expirado", StringComparison.OrdinalIgnoreCase))
        {
            await _eventPublisher.PublishAsync(new FalhaResetSenhaEvent(command.Email, "Código de recuperação expirado"), cancellationToken);
            return Result<bool>.Failure("Código de recuperação expirado");
        }
        catch (Exception)
        {
            await _eventPublisher.PublishAsync(new FalhaResetSenhaEvent(command.Email, "Erro ao validar o código de recuperação"), cancellationToken);
            return Result<bool>.Failure("Erro ao validar o código de recuperação");
        }

        var senhaHash = _passwordHasher.Hash(command.NovaSenha);
        
        try
        {
            user.UpdatePassword(senhaHash);
        }
        catch (ArgumentException ex)
        {
            await _eventPublisher.PublishAsync(new FalhaResetSenhaEvent(command.Email, $"Erro na atualização: {ex.Message}"), cancellationToken);
            return Result<bool>.Failure(ex.Message);
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        await _eventPublisher.PublishAsync(new SenhaResetConcluidoEvent(user.Id, user.Email), cancellationToken);

        return Result<bool>.Success(true);
    }
}
