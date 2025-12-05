# Build aþamasý
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Sadece csproj dosyasýný kopyalayarak baðýmlýlýklarý geri yükle
COPY api1/*.csproj ./api1/
RUN dotnet restore ./api1/api1.csproj

# Geri kalan kaynak kodunu kopyala ve yayýnla
COPY api1/. ./api1/
RUN dotnet publish ./api1/api1.csproj -c Release -o out

# Runtime aþamasý
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Yayýnlanmýþ dosyalarý kopyala
COPY --from=build /app/api1/out .

# Render'da dinamik PORT'u dinlemesi için ASPNETCORE_URLS'i ayarla.
# Bu, Kestrel'i ortam deðiþkeni olan $PORT'u dinlemeye zorlar.
ENV ASPNETCORE_URLS=http://+:$PORT

# Not: EXPOSE 5000'i kaldýrabiliriz, çünkü dinamik port kullanacaðýz,
# ancak konteynerin içinden herhangi bir portu dýþarý açmak Render için yeterlidir.
EXPOSE 8080 

# Uygulamayý baþlatma komutu
ENTRYPOINT ["dotnet", "api1.dll"]