# 1. Aşama: Derleme (Build)
# SDK (Software Development Kit) imajını kullan
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# .csproj dosyasını kopyalayarak restore işlemini önbelleğe al
# Düzeltme 1: api1/ klasörünü kaldırdık. Doğrudan kök dizinden kopyalama.
COPY api1.csproj . 

# Proje bağımlılıklarını geri yükle (restore)
# Düzeltme 2: restore komutundaki api1/ klasörünü kaldırdık.
RUN dotnet restore

# Geri kalan tüm kaynak kodunu kopyala
# Düzeltme 3: COPY komutunda kaynak (sol taraf) ve hedef (sağ taraf) klasör adlarını kaldırdık.
# Sadece . (bulunduğun dizin) kopyalanacak.
COPY . .

# Uygulamayı yayınla (publish)
# Düzeltme 4: publish komutundaki api1/ klasörünü kaldırdık.
RUN dotnet publish -c Release -o out

# -----------------------------------------------------------

# 2. Aşama: Çalıştırma (Runtime)
# ASPNET imajını kullan (daha küçük ve güvenli)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Çalışma klasörüne yayınlanmış dosyaları kopyala
COPY --from=build /app/out .

# Uygulamayı başlat
ENTRYPOINT ["dotnet", "api1.dll"]