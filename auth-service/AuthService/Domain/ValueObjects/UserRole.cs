namespace AuthService.Domain.ValueObjects;

public static class UserRole
{
    public const string Admin = "ADMIN";
    public const string User = "USER";

    public static bool IsValid(string role)
    {
        return role == Admin || role == User;
    }

    public static string GetDefault()
    {
        return User;
    }
}
