# 1. Build aþamasý
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Proje dosyalarýný kopyala ve restore et
COPY *.csproj ./
RUN dotnet restore

# Tüm proje dosyalarýný kopyala ve build et
COPY . ./
RUN dotnet publish -c Release -o out

# 2. Runtime aþamasý
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# API portunu aç
EXPOSE 5000

# Uygulamayý çalýþtýr
ENTRYPOINT ["dotnet", "api1.dll"]
