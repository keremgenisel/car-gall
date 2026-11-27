using System.Text;
using System.Xml;
using CarGallery.Host.Localization;
using CarGallery.Modules.Content.Contracts.Pages;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;

namespace CarGallery.Host.Seo;

/// <summary>
/// `0004` **R7** · **R8** · **D3** · **D4** · **D5** · **P9** — sitemap dizini, dil başına sitemap
/// ve robots.txt. Sitemap İSTEK ANINDA, vitrinin o anki hâlinden üretilir: olaylarla beslenen bir
/// depo yoktur, yani yayından kalkan ilan bir sonraki okumada listede yer almaz ve uygulama
/// açılmadan önce yayında olan ilan ilk okumada vardır. Kök <see cref="SiteRoot"/>'tan (C10).
/// Ayrı bir önbellek kuralı konmaz (D19).
/// </summary>
internal static class SeoEndpoints
{
    internal const string SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

    internal static IEndpointRouteBuilder MapSeo(this IEndpointRouteBuilder app)
    {
        app.MapGet("/robots.txt", (SiteRoot root) =>
            // D5 · tuzak 2 — panel ve bülten sayfaları KAPATILMAZ: `noindex`'lerini ancak okunurlarsa
            // gösterebilirler. Yalnız API kapalı; belge değildir ve tarama bütçesini harcar.
            Results.Text(
                $"User-agent: *\nDisallow: /api/\n\nSitemap: {root.Absolute("/sitemap.xml")}\n",
                "text/plain; charset=utf-8"));

        app.MapGet("/sitemap.xml", (SiteRoot root, LocalePrefixResolver locales) =>
            Xml(writer =>
            {
                writer.WriteStartElement("sitemapindex", SitemapNamespace);

                foreach (string dil in locales.Enabled)
                {
                    writer.WriteStartElement("sitemap", SitemapNamespace);
                    writer.WriteElementString("loc", SitemapNamespace, root.Absolute($"/sitemap-{dil}.xml"));
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }));

        app.MapGet("/sitemap-{locale}.xml", async (
            string locale,
            SiteRoot root,
            LocalePrefixResolver locales,
            IShowcaseSitemapQueries listings,
            IContentPublicQueries pages,
            CancellationToken cancellationToken) =>
        {
            if (!locales.IsEnabled(locale))
            {
                return Results.NotFound();
            }

            // D4 — ana sayfa, parametresiz liste, görünür her ilan (Rezerve dâhil) ve o dilde
            // GÖRÜNEN her içerik sayfası. `lastmod` basılmaz (X8); alternatifler belgede (D4).
            List<string> adresler =
            [
                root.Absolute(SeoHeadBuilder.HomePath(locale)),
                root.Absolute(SeoHeadBuilder.ListPath(locale)),
                // `0028` D12 — iletişim sayfası her etkin dilde (AC-11).
                root.Absolute(SeoHeadBuilder.ContactPath(locale)),
                .. (await listings.ListVisibleSlugsAsync(cancellationToken))
                    .Select(slug => root.Absolute($"{SeoHeadBuilder.ListPath(locale)}/{Uri.EscapeDataString(slug)}")),
                // `0028` D12 · CLARIFY B4 — ayrılmış adla saklanmış içerik sayfası girmez: o adres
                // ayrılmış sayfayı açar. `favorites`/`compare`'deki önceden var olan açığı da kapatır (AC-93, AC-94).
                .. (await pages.ListVisibleSlugsAsync(locale, cancellationToken))
                    .Where(slug => !ContentReservedSlugs.Contains(slug))
                    .Select(slug => root.Absolute(SeoHeadBuilder.ContentPath(locale, slug))),
            ];

            return Xml(writer =>
            {
                writer.WriteStartElement("urlset", SitemapNamespace);

                foreach (string adres in adresler)
                {
                    writer.WriteStartElement("url", SitemapNamespace);
                    writer.WriteElementString("loc", SitemapNamespace, adres);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            });
        });

        return app;
    }

    private static IResult Xml(Action<XmlWriter> write)
    {
        StringBuilder metin = new();

        using (XmlWriter writer = XmlWriter.Create(metin, new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = false }))
        {
            writer.WriteStartDocument();
            write(writer);
            writer.WriteEndDocument();
        }

        // StringBuilder'a yazılan bildirim "utf-16" der; yanıt UTF-8 olduğu için bildirim de öyle.
        string xml = metin.ToString().Replace("encoding=\"utf-16\"", "encoding=\"utf-8\"", StringComparison.Ordinal);

        return Results.Text(xml, "application/xml; charset=utf-8");
    }
}
