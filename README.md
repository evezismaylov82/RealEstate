# RealEstateAPI

RealEstateAPI — daşınmaz əmlak elanlarının idarə olunması üçün hazırlanmış backend Web API layihəsidir. Layihə ASP.NET Core və C# istifadə edilərək hazırlanıb.

Layihədə istifadəçilər qeydiyyatdan keçə, sistemə daxil ola, daşınmaz əmlak elanları yarada, elanlara baxa, axtarış və filter edə, rezervasiya və rəy əməliyyatlarından istifadə edə bilərlər.

## İstifadə olunan texnologiyalar

- C#
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- JWT Authentication
- Refresh Token
- AutoMapper
- Swagger
- Repository Pattern
- Unit of Work
- N-Tier Architecture
- LINQ

## Layihənin arxitekturası

Layihə N-Tier (qatlı) arxitektura əsasında hazırlanıb və əsasən 4 qatdan ibarətdir:

### 1. API Layer

Bu qat istifadəçi ilə sistem arasında əlaqəni təmin edir.

Burada:
- Controller-lər
- Endpoint-lər
- Authentication və Authorization
- Middleware-lər
- Swagger

yerləşir.

Controller-lərdən gələn HTTP sorğuları qəbul edilir və uyğun Service-ə göndərilir.

### 2. Application Layer

Bu qat layihənin əsas biznes məntiqinin yerləşdiyi hissədir.

Burada:
- Service-lər
- DTO-lar
- Interface-lər
- Validation
- AutoMapper profilləri

yerləşir.

Service-lər Controller-dən gələn sorğunu qəbul edir və lazım olan əməliyyatı yerinə yetirir.

### 3. Domain Layer

Bu qat layihənin əsas modellərini və biznes obyektlərini saxlayır.

Burada:
- Entity-lər
- Enum-lar
- Entity əlaqələri
- Əsas domain qaydaları

yerləşir.

Məsələn:
User, Property, Category, Booking və Review kimi əsas entity-lər bu qatda yerləşir.

### 4. Infrastructure Layer

Bu qat database və xarici resurslarla əlaqəni təmin edir.

Burada:
- Entity Framework Core
- DbContext
- Repository-lər
- Unit of Work
- Database konfiqurasiyası
- Migration-lar
- Fayl və şəkil əməliyyatları

yerləşir.

## Əsas funksionallıqlar

### User System

- User qeydiyyatı
- Login
- Email confirmation
- Password reset
- JWT Authentication
- Refresh Token
- User role sistemi

### Real Estate Management

- Yeni əmlak elanının yaradılması
- Elanın yenilənməsi
- Elanın silinməsi
- Elanlara baxış
- Kateqoriyalar
- Şəkil əlavə edilməsi
- Slug sistemi
- Filter və axtarış
- Pagination

### Booking System

İstifadəçi daşınmaz əmlaka baxış üçün vaxt seçə bilər.

Sistem eyni vaxt üçün mövcud rezervasiyanı yoxlayır və vaxtın uyğun olub-olmadığını müəyyən edir.

### Review System

İstifadəçilər daşınmaz əmlak haqqında rəy yaza və qiymətləndirmə edə bilərlər.

## HTTP metodları

API-də əsas HTTP metodlarından istifadə olunur:

- GET — məlumatları əldə etmək üçün
- POST — yeni məlumat yaratmaq üçün
- PUT — mövcud məlumatı yeniləmək üçün
- DELETE — məlumatı silmək üçün

## Authentication

Layihədə istifadəçilərin təhlükəsiz şəkildə sistemə daxil olması üçün JWT Authentication istifadə olunur.

Login zamanı istifadəçiyə Access Token və Refresh Token təqdim olunur.

Access Token istifadəçinin qorunan endpoint-lərə girişini təmin edir.

Refresh Token isə Access Token-in müddəti bitdikdə yeni token əldə etmək üçün istifadə olunur.

## Repository Pattern

Repository Pattern database əməliyyatlarını ayrıca idarə etmək üçün istifadə olunur.

Repository vasitəsilə məlumatların:
- əlavə edilməsi
- əldə edilməsi
- yenilənməsi
- silinməsi

əməliyyatları həyata keçirilir.

Bu yanaşma database məntiqinin Service və Controller-lərdən ayrılmasına kömək edir.

## Unit of Work

Unit of Work bir neçə database əməliyyatının birlikdə idarə olunmasına kömək edir.

Repository-lər vasitəsilə edilən dəyişikliklər sonda Unit of Work vasitəsilə database-ə göndərilə bilər.

## DTO

DTO (Data Transfer Object) client və API arasında məlumatların ötürülməsi üçün istifadə olunur.

DTO istifadə etməklə Entity-lərin birbaşa client-ə göndərilməsinin qarşısı alınır və yalnız lazım olan məlumatlar ötürülür.

## Swagger

Swagger API endpoint-lərini test etmək və API sənədlərini görmək üçün istifadə olunur.

Swagger vasitəsilə:
- GET
- POST
- PUT
- DELETE

endpoint-lərini birbaşa browser üzərindən test etmək mümkündür.

## Database

Layihədə məlumatların saxlanılması üçün SQL Server və Entity Framework Core istifadə olunur.

Entity-lər arasında müxtəlif əlaqələr yaradılıb və database əlaqələri Entity Framework Core vasitəsilə idarə olunur.

## Layihənin məqsədi

Bu layihənin əsas məqsədi real layihələrdə istifadə olunan backend prinsiplərini tətbiq etmək və ASP.NET Core Web API istifadə edərək tam funksional daşınmaz əmlak sistemi yaratmaqdır.
## Project Status

This project is currently under development.
