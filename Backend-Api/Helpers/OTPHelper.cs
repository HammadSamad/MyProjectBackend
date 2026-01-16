namespace Backend_Api.Helpers
{
    public class OTPHelper
    {
        public static string GenerateOTP(int length = 6)
        {
            var random = new Random();
            string otp = "";
            for (int i = 0; i < length; i++)
            {
                otp += random.Next(0, 10).ToString();
            }
            return otp;
        }
    }
}
