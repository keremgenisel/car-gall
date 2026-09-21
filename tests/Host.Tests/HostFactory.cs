using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarGallery.Host.Tests;

/// <summary>
/// Gerçek wwwroot, frontend build'inin çıktısıdır ve repoda durmaz. Bu testler lang/dir
/// enjeksiyonunu doğrular, paketlenmiş bundle'ı değil; bu yüzden sabit bir test kökü
/// kullanırlar ve frontend'in derlenmiş olmasına bağımlı DEĞİLLERDİR.
/// </summary>
public sealed class HostFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
}
