using ElectrostoreAPI.Models;

namespace ElectrostoreAPI.Extensions;

public static class UsersPasswordsExtensions
{
    // a user who never had a password (external login only) gets an empty hash when he starts a password reset: nothing matches it
    public static bool HasPassword(this UsersPasswords? userPassword)
    {
        return userPassword is not null && !string.IsNullOrEmpty(userPassword.password_user);
    }

    public static bool IsPasswordValid(this UsersPasswords? userPassword, string password)
    {
        return userPassword.HasPassword() && BCrypt.Net.BCrypt.Verify(password, userPassword!.password_user);
    }
}
