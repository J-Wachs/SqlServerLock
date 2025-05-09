namespace SqlServerLock.Utils;

/// <summary>
/// Class that implements the Result-pattern to make it simple to 
/// return data and/or the result (errors).
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public IList<string> Messages { get; } = [];

    private Result(bool isSuccess, IList<string> messages)
    {
        IsSuccess = isSuccess;
        Messages = messages;

    }
    public static Result Success() => new(true, []);
    public static Result Failure(string message) => new(false, [message]);
    public static Result Failure(IList<string> messages) => new(false, messages);
}
