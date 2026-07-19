using System.Net;

namespace AuthAPI.Services.Implements
{
    public class EmailTemplateService : IEmailTemplateService
    {
        public string GenerateForgotPasswordEmailBody(string fullName, string otp)
        {
            return $@"
    <div style='font-family: ""Helvetica Neue"", Helvetica, Arial, sans-serif; background-color: #f4f5f7; padding: 40px 20px; color: #333333;'>
        <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 40px; border-radius: 8px; box-shadow: 0 4px 10px rgba(0,0,0,0.05);'>
            
            <h2 style='color: #2c3e50; text-align: center; border-bottom: 2px solid #f0f2f5; padding-bottom: 20px; margin-top: 0;'>Password Reset Request</h2>
            
            <p style='font-size: 16px; line-height: 1.6; margin-top: 20px;'>Hello <strong>{fullName}</strong>,</p>
            
            <p style='font-size: 16px; line-height: 1.6;'>We received a request to reset the password for your account associated with this email address. Please use the verification code below to proceed:</p>
            
            <div style='text-align: center; margin: 35px 0;'>
                <span style='font-size: 32px; font-weight: bold; color: #0056b3; letter-spacing: 8px; padding: 15px 30px; background-color: #f8f9fa; border-radius: 8px; border: 2px dashed #0056b3; display: inline-block;'>{otp}</span>
            </div>
            
            <p style='font-size: 15px; color: #e74c3c; text-align: center; font-weight: bold; margin-bottom: 30px;'>
                ⏱️ This code is valid for exactly 5 minutes.
            </p>
            
            <p style='font-size: 14px; line-height: 1.6; color: #666666;'>
                If you did not request a password reset, you can safely ignore this email. Your password will remain unchanged, and your account is secure.
            </p>
            
            <hr style='border: none; border-top: 1px solid #eeeeee; margin: 30px 0;' />
            
            <p style='font-size: 13px; color: #999999; text-align: center; margin-bottom: 0;'>
                Best regards,<br>
                <strong>The StayHub Team</strong>
            </p>
        </div>
    </div>";
        }

        public string GenerateRegisterOtpEmailBody(string fullName, string otp)
        {
            return $@"
    <div style='font-family: ""Helvetica Neue"", Helvetica, Arial, sans-serif; background-color: #f4f5f7; padding: 40px 20px; color: #333333;'>
        <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 40px; border-radius: 8px; box-shadow: 0 4px 10px rgba(0,0,0,0.05);'>
            
            <h2 style='color: #2c3e50; text-align: center; border-bottom: 2px solid #f0f2f5; padding-bottom: 20px; margin-top: 0;'>Welcome to StayHub!</h2>
            
            <p style='font-size: 16px; line-height: 1.6; margin-top: 20px;'>Hello <strong>{fullName}</strong>,</p>
            
            <p style='font-size: 16px; line-height: 1.6;'>Thank you for registering an account on StayHub. To complete your account registration and verify your email address, please use the verification code below:</p>
            
            <div style='text-align: center; margin: 35px 0;'>
                <span style='font-size: 32px; font-weight: bold; color: #10b981; letter-spacing: 8px; padding: 15px 30px; background-color: #ecfdf5; border-radius: 8px; border: 2px dashed #10b981; display: inline-block;'>{otp}</span>
            </div>
            
            <p style='font-size: 15px; color: #059669; text-align: center; font-weight: bold; margin-bottom: 30px;'>
                ⏱️ This verification code is valid for exactly 10 minutes.
            </p>
            
            <p style='font-size: 14px; line-height: 1.6; color: #666666;'>
                If you did not request to create an account on StayHub, you can safely ignore this email.
            </p>
            
            <hr style='border: none; border-top: 1px solid #eeeeee; margin: 30px 0;' />
            
            <p style='font-size: 13px; color: #999999; text-align: center; margin-bottom: 0;'>
                Best regards,<br>
                <strong>The StayHub Team</strong>
            </p>
        </div>
    </div>";
        }

        public string GenerateTemporaryCredentialsEmailBody(string fullName, string email, string temporaryPassword)
        {
            var safeEmail = WebUtility.HtmlEncode(email);
            var safeFullName = WebUtility.HtmlEncode(fullName);
            var safePassword = WebUtility.HtmlEncode(temporaryPassword);

            return $@"
<div style='font-family: Arial, sans-serif; background: #f4f6f8; padding: 32px 16px; color: #1f2937;'>
  <div style='max-width: 600px; margin: 0 auto; background: #ffffff; padding: 32px; border-radius: 12px;'>
    <h2 style='margin-top: 0; color: #0f172a;'>Welcome to StayHub</h2>
    <p>Hello <strong>{safeFullName}</strong>,</p>
    <p>An administrator has created a StayHub account for you.</p>
    <div style='margin: 24px 0; padding: 20px; background: #f8fafc; border-radius: 8px;'>
      <p style='margin: 0 0 12px;'><strong>Email:</strong> {safeEmail}</p>
      <p style='margin: 0;'><strong>Temporary password:</strong> <span style='font-family: monospace; font-size: 18px;'>{safePassword}</span></p>
    </div>
    <p style='color: #b45309;'><strong>For security, you must change this temporary password when you first sign in.</strong></p>
    <p>Do not share this password with anyone.</p>
  </div>
</div>";
        }
    }
}
