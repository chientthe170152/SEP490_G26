namespace MTCA.Application.Common.Models;

public interface IResultResponse
{
    bool IsSuccess { get; }
    IReadOnlyList<Error> Errors { get; }
}
