namespace CarGallery.BuildingBlocks;

/// <summary>
/// Beklenen başarısızlıklar istisna ile değil bu tiple taşınır
/// (docs/conventions.md § 5: "istisna akış kontrolü için kullanılmaz").
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess != error.IsNone)
        {
            throw new ArgumentException("Başarılı sonuç hata taşıyamaz, başarısız sonuç hatasız olamaz.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? value;

    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error) => this.value = value;

    public TValue Value => IsSuccess
        ? value!
        : throw new InvalidOperationException("Başarısız sonucun değeri okunamaz.");
}
