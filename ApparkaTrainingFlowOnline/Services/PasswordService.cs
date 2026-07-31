using ApparkaTrainingFlowOnline.Models;
using Microsoft.AspNetCore.Identity;

namespace ApparkaTrainingFlowOnline.Services;

public class PasswordService
{
    private readonly PasswordHasher<AppUser> _hasher = new();
    public string Hash(AppUser user, string password) => _hasher.HashPassword(user, password);
    public bool Verify(AppUser user, string password) =>
        _hasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;
}
