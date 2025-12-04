# MyApiProject

Bu proje, **chat uygulamaları için backend API** sağlayan bir ASP.NET Core Web API projesidir.  
Hem **WPF** hem de **web tabanlı** frontend uygulamaları bu API üzerinden iletişim kurabilir.

---

## Özellikler

- REST API endpointleri:
  - `/api/user` → Kullanıcı işlemleri (login, register)
  - `/api/friend` → Arkadaş ekleme ve listeleme
  - `/api/message` → Mesaj gönderme ve alma
- SignalR Hub:
  - Gerçek zamanlı mesajlaşma (hem WPF hem web client ile uyumlu)
- MySQL veya SQLite veritabanı desteği
- JWT ile güvenli authentication

---

## Gereksinimler

- .NET 8.0 SDK veya üstü
- Visual Studio 2022 veya üstü
- MySQL veya SQLite
- (Opsiyonel) SignalR destekli frontend (WPF veya Web)

---

## Çalıştırma (Development)

1. HTTPS sertifikasını yükle:
```bash
dotnet dev-certs https --trust
