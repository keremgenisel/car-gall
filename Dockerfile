# car-gall — tek kurulumluk dağıtım imajı (ADR 0012).
#
# Aynı imaj İKİ komutla çalışır (ADR 0002):
#   docker run … <imaj> --migrate   → kurulum adımı: şemayı uygular ve çıkar
#   docker run … <imaj>             → uygulama: şemayı yalnız doğrular, eksikse başlamaz
#
# İmajda HİÇBİR SIR ve hiçbir kuruluma özel değer yoktur; hepsi çalıştırma anında ortam
# değişkeninden gelir (docs/deploy.md § Yapılandırma). Aynı imaj her galeride çalışır.

# ---------- 1. Ön yüz ----------
# Vite çıktısı Host'un wwwroot'una yazılır (frontend/vite.config.ts → ../src/Host/wwwroot).
FROM node:24-bookworm-slim AS frontend
WORKDIR /repo/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY frontend/ ./
RUN npm run build

# ---------- 2. Arka yüz ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /repo
COPY Directory.Build.props CarGallery.slnx ./
COPY src/ src/
# wwwroot .gitignore'da ve .dockerignore'da; yerel bir derlemenin artığı imaja sızmasın
# diye yalnız 1. aşamanın çıktısı kopyalanır.
COPY --from=frontend /repo/src/Host/wwwroot/ src/Host/wwwroot/
RUN dotnet publish src/Host/Host.csproj -c Release -o /app --nologo

# ---------- 3. Çalışma imajı ----------
# Debian tabanlı aspnet imajı tzdata ve ICU taşır: şube saat dilimi doğrulaması
# (0003/D8) tzdata olmadan GEÇERLİ bölge kimliğini de reddeder, Arapça/Türkçe kültür
# biçimleri ICU ister. İkisinin varlığı docs/deploy.md § Doğrulama'da ölçülür.
# Alpine/chiseled bilerek seçilmedi: ikisi de bu paketleri varsayılan olarak taşımaz.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# tini BİLEREK, ölçülmüş bir hatadan: dotnet PID 1 olunca çekirdek ona varsayılan
# eylemli sinyalleri iletmez ve çöküşün `abort`'u boşa düşer. Şema eksikken uygulama
# "başlamayı reddediyor" mesajını basıp SONSUZA KADAR takılı kalıyordu — konteyner
# `running`, çıkış kodu 0 (40 sn ölçüldü). `--init` ile aynı hata 2 sn'de 134 ile
# çıktı. Platformların çoğu `--init` eklemiyor; init imajın içinde olmak zorunda.
RUN apt-get update \
    && apt-get install -y --no-install-recommends tini \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=backend /app ./

# Kök olmayan kullanıcı (imajın kendi `app` kullanıcısı) ve 8080 portu.
USER app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_NOLOGO=1
EXPOSE 8080

ENTRYPOINT ["/usr/bin/tini", "--", "dotnet", "CarGallery.Host.dll"]
