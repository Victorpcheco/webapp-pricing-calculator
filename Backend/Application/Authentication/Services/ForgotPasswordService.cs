using Application.Authentication.Commands;
using Application.Common;
using Application.Repositories;
using Domain.Common;

namespace Application.Authentication.Services;

public class ForgotPasswordService : IScopedService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly IPasswordResetTokenService _tokenService;

    public ForgotPasswordService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        IPasswordResetTokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _tokenService = tokenService;
    }

    public async Task<Result<bool>> RequestPasswordResetAsync(RequestPasswordResetCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        
        if (user == null)
        {
            return Result<bool>.Failure("E-mail não encontrado");
        }

        var token = await _tokenService.GenerateTokenAsync(user.Id, cancellationToken);
        
        await _emailService.SendPasswordResetEmailAsync(user.Email, token, cancellationToken);

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ResetPasswordAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);

        if (user == null)
        {
            return Result<bool>.Failure("E-mail não encontrado");
        }

        try
        {
            var isTokenValid = await _tokenService.ValidateTokenAsync(user.Id, command.Token, cancellationToken);

            if (!isTokenValid)
            {
                return Result<bool>.Failure("Código de recuperação inválido");
            }
        }
        catch (ArgumentException ex) when (ex.Message.Contains("expirado", StringComparison.OrdinalIgnoreCase))
        {
            return Result<bool>.Failure("Código de recuperação expirado");
        }
        catch (Exception)
        {
            return Result<bool>.Failure("Erro ao validar o código de recuperação");
        }

        var senhaHash = _passwordHasher.Hash(command.NovaSenha);
        
        try
        {
            user.UpdatePassword(senhaHash);
        }
        catch (ArgumentException ex)
        {
            return Result<bool>.Failure(ex.Message);
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result<bool>.Success(true);
    }
}
