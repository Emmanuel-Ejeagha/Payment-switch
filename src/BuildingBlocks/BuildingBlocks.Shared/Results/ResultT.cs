namespace BuildingBlocks.Shared.Results;

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(T? value, bool isSuccess, IReadOnlyList<Error> errors) : base(errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(value, true, new List<Error>());
    public new static Result<T> Failure(IEnumerable<Error> errors) => new(default, false, errors.ToList());
    public new static Result<T> Failure(params Error[] errors) => new(default, false, errors.ToList());

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
    public static implicit operator Result<T>(List<Error> errors) => Failure(errors);
    public static implicit operator Result<T>(Error[] errors) => Failure(errors);


}
