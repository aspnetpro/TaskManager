namespace TaskManager.Auth.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>
    /// Com hash null (usuário inexistente) compara contra um hash fixo e falha:
    /// o tempo de resposta não denuncia se o email tem conta.
    /// </summary>
    PasswordVerification Verify(string? passwordHash, string password);
}
