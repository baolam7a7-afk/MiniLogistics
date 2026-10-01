namespace MiniLogistics.Web.Models.Auth;

public class ForgotPasswordResult
{
    public bool Ok { get; set; }
    public string? Message { get; set; }
    public string? ResetToken { get; set; }
    public string? Error { get; set; }
}

public class AuthMessage
{
    public string? Message { get; set; }
    public string? ResetToken { get; set; }
}
