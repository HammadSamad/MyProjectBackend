using Microsoft.AspNetCore.Identity;

namespace Backend_Api.Helpers
{
    public class PasswordHasherHelper
    {
        private readonly PasswordHasher<object> _passwordHasher;

        public PasswordHasherHelper()
        {
            _passwordHasher = new PasswordHasher<object>();
        }

        // Hash password before saving to DB
        public string HashPassword(string password)
        {
            return _passwordHasher.HashPassword(null!, password);
        }

        // Verify password during login
        public bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            var result = _passwordHasher.VerifyHashedPassword(
                null!,
                hashedPassword,
                providedPassword
            );

            return result == PasswordVerificationResult.Success;
        }
    }
}
