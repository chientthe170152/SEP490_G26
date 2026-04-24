namespace MTCA.Domain.Common.Exceptions;

public sealed class InvariantViolationException : DomainException
{
    public string? RuleName { get; }

    public InvariantViolationException(string message) : base(message) { }

    public InvariantViolationException(string ruleName, string message) : base(message)
    {
        RuleName = ruleName;
    }
}
