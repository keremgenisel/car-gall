namespace CarGallery.Modules.Media.Domain;

/// <summary>
/// Üretilmiş bir türev. Varlığın parçasıdır (EF owned collection), ayrı agregat değildir
/// (`docs/conventions.md` § 10).
///
/// <para><c>Width</c>/<c>Height</c> GERÇEKLEŞEN boyuttur, hedeflenen değil: AC-14 gereği
/// kaynak hedeften küçükse büyütme yapılmaz ve türev kaynak genişliğinde üretilir. Hedefi
/// saklamak, "1600" yazıp 900 piksel servis etmek demekti.</para>
/// </summary>
internal sealed class MediaDerivative
{
    internal required string VariantCode { get; init; }

    internal required int Width { get; init; }

    internal required int Height { get; init; }

    internal required string ObjectKey { get; init; }
}
