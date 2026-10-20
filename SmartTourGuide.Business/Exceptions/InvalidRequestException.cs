namespace SmartTourGuide.Business.Exceptions;

public sealed class InvalidRequestException
    : BusinessException
{
    public InvalidRequestException(
        string message)
        : base(
            code: "INVALID_REQUEST",
            message: message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            message);
    }
}