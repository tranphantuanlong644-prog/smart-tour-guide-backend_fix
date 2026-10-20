namespace SmartTourGuide.Business.Exceptions;

public sealed class AccountInactiveException
    : BusinessException
{
    public AccountInactiveException()
        : base(
            code: "ACCOUNT_INACTIVE",
            message:
                "This account is inactive.")
    {
    }
}