using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Dealership.Domain;

/// <summary>
/// Şubenin koordinatı. `decimal` kolonlarda durur; PostGIS kullanılmaz (C8).
/// "En yakın şube" hesabı istemci tarafındadır ve bu spec'te kapsam dışıdır (X6).
/// </summary>
internal readonly record struct Coordinate
{
    internal const string OutOfRange = "coordinate.out_of_range";

    private Coordinate(decimal latitude, decimal longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    internal decimal Latitude { get; }

    internal decimal Longitude { get; }

    internal static Result<Coordinate> Create(decimal latitude, decimal longitude)
    {
        if (latitude is < -90m or > 90m || longitude is < -180m or > 180m)
        {
            return Result.Failure<Coordinate>(new Error(OutOfRange));
        }

        return Result.Success(new Coordinate(latitude, longitude));
    }
}
