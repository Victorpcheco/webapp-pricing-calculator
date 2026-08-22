using Application.Authentication.Commands;
using Application.Authentication.Services;
using Application.Common;
using Application.Repositories;
using Domain.Entities.Users;
using FluentAssertions;
using Moq;
using Domain.Common;
using Domain.Entities.Users.Events;

namespace Tests.Authentication;

public class ForgotPasswordServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IPasswordResetTokenService> _tokenServiceMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly ForgotPasswordService _sut;

    public ForgotPasswordServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _emailServiceMock = new Mock<IEmailService>();
        _tokenServiceMock = new Mock<IPasswordResetTokenService>();
        _eventPublisherMock = new Mock<IEventPublisher>();

        _sut = new ForgotPasswordService(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _emailServiceMock.Object,
            _tokenServiceMock.Object,
            _eventPublisherMock.Object);
    }

    [Fact]
    public async Task RequestPasswordReset_ComEmailValidoEExistente_DeveGerarTokenEEnviarEmail()
    {
        // Arrange
        var command = new RequestPasswordResetCommand("john@example.com");
        var user = User.Create("John", "1234567890", "john@example.com", "P@ssword123").Value;
        var token = "123456";

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _tokenServiceMock.Setup(x => x.GenerateTokenAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        // Act
        var result = await _sut.RequestPasswordResetAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _tokenServiceMock.Verify(x => x.GenerateTokenAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.Verify(x => x.SendPasswordResetEmailAsync(user.Email, token, It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(x => x.PublishAsync(It.Is<SenhaResetSolicitadoEvent>(e => e.Email == user.Email), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestPasswordReset_ComEmailInexistente_DeveRetornarFalha()
    {
        // Arrange
        var command = new RequestPasswordResetCommand("nonexistent@example.com");

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.RequestPasswordResetAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNullOrEmpty();

        _tokenServiceMock.Verify(x => x.GenerateTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailServiceMock.Verify(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _eventPublisherMock.Verify(x => x.PublishAsync(It.IsAny<FalhaResetSenhaEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_ComTokenValido_DeveAtualizarSenhaComSucesso()
    {
        // Arrange
        var command = new ResetPasswordCommand("john@example.com", "123456", "N3wP@ssw0rd!");
        var user = User.Create("John", "1234567890", "john@example.com", "OldP@ssword123").Value;
        var hashedNewPassword = "hashed_new_password";

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _tokenServiceMock.Setup(x => x.ValidateTokenAsync(user.Id, command.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _passwordHasherMock.Setup(x => x.Hash(command.NovaSenha))
            .Returns(hashedNewPassword);

        // Act
        var result = await _sut.ResetPasswordAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _userRepositoryMock.Verify(x => x.UpdateAsync(It.Is<User>(u => 
            u.Id == user.Id && 
            u.SenhaHash == hashedNewPassword), It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(x => x.PublishAsync(It.Is<SenhaResetConcluidoEvent>(e => e.UserId == user.Id && e.Email == user.Email), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_ComTokenInvalido_DeveRetornarFalha()
    {
        // Arrange
        var command = new ResetPasswordCommand("john@example.com", "invalid_token", "N3wP@ssw0rd!");
        var user = User.Create("John", "1234567890", "john@example.com", "OldP@ssword123").Value;

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _tokenServiceMock.Setup(x => x.ValidateTokenAsync(user.Id, command.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.ResetPasswordAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNullOrEmpty();

        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _eventPublisherMock.Verify(x => x.PublishAsync(It.IsAny<FalhaResetSenhaEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_ComTokenExpirado_DeveRetornarFalha()
    {
        // Arrange
        var command = new ResetPasswordCommand("john@example.com", "expired_token", "N3wP@ssw0rd!");
        var user = User.Create("John", "1234567890", "john@example.com", "OldP@ssword123").Value;

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _tokenServiceMock.Setup(x => x.ValidateTokenAsync(user.Id, command.Token, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Token expirado"));

        // Act
        var result = await _sut.ResetPasswordAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("expirado");

        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _eventPublisherMock.Verify(x => x.PublishAsync(It.IsAny<FalhaResetSenhaEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
