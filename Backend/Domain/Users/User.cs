using Domain.Common;

namespace Domain.Users;

public class User
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Telefone { get; private set; } = string.Empty;
    public string SenhaHash { get; private set; } = string.Empty;

    private User() { }

    public static Result<User> Create(string nome, string telefone, string email, string senhaHash)
    {

        try {
            ValidateCreate(nome, telefone, email, senhaHash);
        }
        catch (Exception ex)
        {
            return Result<User>.Failure(ex.Message);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Telefone = telefone,
            Email = email,
            SenhaHash = senhaHash
        };

        return Result<User>.Success(user);
    }

    private static void ValidateCreate(string nome, string telefone, string email, string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome é obrigatório", nameof(nome));
        
        if (string.IsNullOrWhiteSpace(telefone))
            throw new ArgumentException("Telefone é obrigatório", nameof(telefone));
        
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("E-mail é obrigatório", nameof(email));
        
        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new ArgumentException("Senha é obrigatória", nameof(senhaHash));
        
        if (!email.Contains("@") || !email.Contains("."))
            throw new ArgumentException("E-mail inválido", nameof(email));
    }
}
