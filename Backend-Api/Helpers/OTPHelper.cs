using System;
using System.Security.Cryptography;
using System.Text;

namespace Backend_Api.Helpers
{
    public static class OTPHelper
    {
        // Generate numeric OTP
        public static string GenerateOTP(int length = 4)
        {
            if (length <= 0) length = 4;

            var otp = new StringBuilder();
            using (var rng = RandomNumberGenerator.Create())
            {
                for (int i = 0; i < length; i++)
                {
                    byte[] randomNumber = new byte[1];
                    rng.GetBytes(randomNumber);
                    int digit = randomNumber[0] % 10; // 0-9
                    otp.Append(digit);
                }
            }
            return otp.ToString();
        }
    }
}
