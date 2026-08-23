using Application.Authentication.Commands;
using Application.Authentication.Services;
using Application.Common;
using Application.Repositories;
using Domain.Entities.Users;
using Domain.Entities.Users.Events;
using FluentAssertions;
using Moq;

namespace Tests.Authentication;

public class AuthenticationServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly AuthenticationService _sut;

    public AuthenticationServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
        _eventPublisherMock = new Mock<IEventPublisher>();

        _sut = new AuthenticationService(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenGeneratorMock.Object,
            _eventPublisherMock.Object);
    }

    [Fact]
    public async Task Registrar_ComDadosValidos_DeveRetornarSucessoEPublicarEvento()
    {
        // Arrange
        var command = new RegisterUserCommand("John Doe", "1234567890", "john@example.com", "P@ssword123");
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordHasherMock.Setup(x => x.Hash(command.SenhaHash))
            .Returns("Hashed_p@ssw0rd1!");

        // Act
        var result = await _sut.RegisterAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _userRepositoryMock.Verify(x => x.AddAsync(It.Is<User>(u => 
            u.Email == command.Email && 
            u.Nome == command.Nome &&
            u.SenhaHash == "Hashed_p@ssw0rd1!"), It.IsAny<CancellationToken>()), Times.Once);

        _eventPublisherMock.Verify(x => x.PublishAsync(It.Is<UsuarioRegistradoEvent>(e => 
            e.Email == command.Email && 
            e.UserId != Guid.Empty), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("", "1234567890", "john@example.com", "P@ssw0rd")]
    [InlineData("John Doe", "", "john@example.com", "P@ssw0rd")]
    [InlineData("John Doe", "1234567890", "invalid-email", "P@ssw0rd")]
    [InlineData("John Doe", "1234567890", "john@example.com", "")]
    public async Task Registrar_ComDadosInvalidos_DeveRetornarFalha(string name, string phone, string email, string password)
    {
        // Arrange
        var command = new RegisterUserCommand(name, phone, email, password);

        // Act
        var result = await _sut.RegisterAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _eventPublisherMock.Verify(x => x.PublishAsync(It.IsAny<UsuarioRegistradoEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Registrar_ComEmailExistente_DeveRetornarFalha()
    {
        // Arrange
        var command = new RegisterUserCommand("John", "123", "existing@example.com", "P@ssword123");
        var existingUser = User.Create("Existing", "1234567890", "existing@example.com", "Hashed_p@ssw0rd1!").Value;
        
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _sut.RegisterAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _eventPublisherMock.Verify(x => x.PublishAsync(It.IsAny<UsuarioRegistradoEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Login_ComCredenciaisValidas_DeveRetornarSucessoComToken()
    {
        // Arrange
        var command = new LoginCommand("john@example.com", "P@ssword123");
        var user = User.Create("John", "1234567890", "john@example.com", "Hashed_p@ssw0rd1!").Value;
        
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        
        _passwordHasherMock.Setup(x => x.Verify(command.SenhaHash, user.SenhaHash))
            .Returns(true);
            
        _jwtTokenGeneratorMock.Setup(x => x.GenerateToken(user.Id, user.Email,user.Nome))
            .Returns("valid_jwt_token");

        // Act
        var result = await _sut.LoginAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("valid_jwt_token");
        result.Value.UserId.Should().Be(user.Id);

        _eventPublisherMock.Verify(x => x.PublishAsync(It.Is<UsuarioLogadoEvent>(e => 
            e.UserId == user.Id && e.Email == user.Email), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_ComSenhaInvalida_DeveRetornarFalhaEPublicarEventoDeFalha()
    {
        // Arrange
        var command = new LoginCommand("john@example.com", "WrongSenha");
        var user = User.Create("John", "1234567890", "john@example.com", "Hashed_p@ssw0rd1!").Value;
        
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
            
        _passwordHasherMock.Setup(x => x.Verify(command.SenhaHash, user.SenhaHash))
            .Returns(false);

        // Act
        var result = await _sut.LoginAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();

        _eventPublisherMock.Verify(x => x.PublishAsync(It.Is<FalhaLoginUsuarioEvent>(e => 
            e.Email == command.Email), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_ComEmailInexistente_DeveRetornarFalhaEPublicarEventoDeFalha()
    {
        // Arrange
        var command = new LoginCommand("nonexistent@example.com", "P@ssword123");
        
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.LoginAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();

        _eventPublisherMock.Verify(x => x.PublishAsync(It.Is<FalhaLoginUsuarioEvent>(e => 
            e.Email == command.Email), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Seguranca_LogsDeAuditoria_NaoDevemConterSenhaEmTextoPlano()
    {
        // Arrange
        var command = new LoginCommand("john@example.com", "MySuperSecretSenha");
        var user = User.Create("John", "1234567890", "john@example.com", "Hashed_p@ssw0rd1!").Value;
        
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
            
        _passwordHasherMock.Setup(x => x.Verify(command.SenhaHash, user.SenhaHash))
            .Returns(false);

        // Act
        await _sut.LoginAsync(command);

        // Assert
        _eventPublisherMock.Verify(x => x.PublishAsync(It.Is<FalhaLoginUsuarioEvent>(e => 
            e.Motivo.Contains(command.SenhaHash)), It.IsAny<CancellationToken>()), Times.Never, 
            "The audit event MUST NOT contain the plaintext password");
    }
}
