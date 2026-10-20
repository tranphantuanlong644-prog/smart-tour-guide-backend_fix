namespace SmartTourGuide.Business.Exceptions;

public sealed class InvalidCredentialsException
    : BusinessException
{
    public InvalidCredentialsException()
        : base(
            code: "INVALID_CREDENTIALS",
            message:
                "The email or password is incorrect.")
    {
    }
}