namespace BuildingBlocks.Shared.Results;

/// <summary>
/// 
/// </summary>
public class Result
{
    public bool IsSuccess => Errors.Count == 0;
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<Error> Errors { get; }

    protected Result(IReadOnlyList<Error> errors) 
    {
        Errors = errors;
    }

    public static Result Success() => new(new List<Error>());
    public static Result Failure(IEnumerable<Error> errors) => new(errors.ToList());
    public static Result Failure(params Error[] errors) => new(errors.ToList());

    public static implicit operator Result(Error error) => Failure(error);
    public static implicit operator Result(List<Error> errors) => Failure(errors);
    public static implicit operator Result(Error[] errors) => Failure(errors);

}
