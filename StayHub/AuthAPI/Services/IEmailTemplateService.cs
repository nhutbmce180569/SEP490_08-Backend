namespace AuthAPI.Services
{
    public interface IEmailTemplateService
    {
        string GenerateForgotPasswordEmailBody(string fullName, string otp);
        string GenerateTemporaryCredentialsEmailBody(string fullName, string email, string temporaryPassword);
    }
}
