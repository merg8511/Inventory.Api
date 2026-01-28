namespace Inventory.Application.Common.Models;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public IReadOnlyList<ValidationError> ValidationErrors { get; }
    
    private Result(bool isSuccess, T? value, string? errorCode, string? errorMessage, 
        IReadOnlyList<ValidationError>? validationErrors)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors ?? Array.Empty<ValidationError>();
    }
    
    public static Result<T> Success(T value) => 
        new(true, value, null, null, null);
        
    public static Result<T> Failure(string errorCode, string message) => 
        new(false, default, errorCode, message, null);
        
    public static Result<T> ValidationFailure(IReadOnlyList<ValidationError> errors) => 
        new(false, default, "VALIDATION_ERROR", "One or more validation errors occurred", errors);
}

public record ValidationError(string Field, string Message);

public class Result
{
    public bool IsSuccess { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    
    private Result(bool isSuccess, string? errorCode, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }
    
    public static Result Success() => new(true, null, null);
    public static Result Failure(string errorCode, string message) => new(false, errorCode, message);
}
