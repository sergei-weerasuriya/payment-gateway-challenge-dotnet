namespace PaymentGateway.Application.Common;

public readonly struct Result<TSuccess, TFailure>
{
    private readonly TSuccess? _success;
    private readonly TFailure? _failure;
    private readonly bool _isSuccess;

    private Result(TSuccess success)
    {
        _success = success;
        _failure = default;
        _isSuccess = true;
    }

    private Result(TFailure failure)
    {
        _success = default;
        _failure = failure;
        _isSuccess = false;
    }

    public bool IsSuccess => _isSuccess;

    public bool IsFailure => !_isSuccess;

    public static Result<TSuccess, TFailure> Success(TSuccess value) => new(value);

    public static Result<TSuccess, TFailure> Failure(TFailure error) => new(error);

    public static implicit operator Result<TSuccess, TFailure>(TSuccess value) => Success(value);

    public static implicit operator Result<TSuccess, TFailure>(TFailure error) => Failure(error);

    public TResult Match<TResult>(Func<TSuccess, TResult> onSuccess, Func<TFailure, TResult> onFailure)
    {
        return _isSuccess
            ? onSuccess(_success!)
            : onFailure(_failure!);
    }

    public void Match(Action<TSuccess> onSuccess, Action<TFailure> onFailure)
    {
        if (_isSuccess)
        {
            onSuccess(_success!);
        }
        else
        {
            onFailure(_failure!);
        }
    }

    public TSuccess GetValueOrThrow()
    {
        if (!_isSuccess)
        {
            throw new InvalidOperationException("Cannot get value from a failed result.");
        }
        return _success!;
    }

    public TSuccess? GetValueOrDefault(TSuccess? defaultValue = default)
    {
        return _isSuccess ? _success : defaultValue;
    }
}