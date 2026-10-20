namespace SmartTourGuide.Business.Exceptions;

public abstract class BusinessException
    : Exception
{
    protected BusinessException(
        string code,
        string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            code);

        Code = code;
    }

    public string Code { get; }
}