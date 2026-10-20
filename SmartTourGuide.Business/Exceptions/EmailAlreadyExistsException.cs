namespace SmartTourGuide.Business.Exceptions;

public sealed class EmailAlreadyExistsException
    : BusinessException
{
    public EmailAlreadyExistsException()
        : base(
            code: "EMAIL_ALREADY_EXISTS",
            message:
                "An account with this email already exists.")
    {
    }
}