namespace ElectrostoreAPI.Constants;

public static class AuthDelays
{
    // minimum delay between two emails of the same kind (password reset / email verification) sent to a user
    public static readonly TimeSpan EmailResendCooldown = TimeSpan.FromSeconds(60);

    public static readonly TimeSpan EmailVerificationTokenLifetime = TimeSpan.FromHours(24);
}
