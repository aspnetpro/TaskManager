namespace TaskManager.Auth.Application.Features.Auth;

/// <summary>Comprimento acima de complexidade (NIST SP 800-63B): sem regra de "1 símbolo e 1 maiúscula".</summary>
public static class PasswordRules
{
    public const int MinLength = 12;
    public const int MaxLength = 128;
}
