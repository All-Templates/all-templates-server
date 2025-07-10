# AllTemplates backend
## Разработка
### Зависимости
- .NET SDK
- Minio
- PostgreSQL

### Подготовка
1. Создание базы данных `AllTemplates`
2. Подготовка Secret Manager
    ```
    dotnet user-secrets init
    dotnet user-secrets set "ConStr" "Строка подключения к БД"

    dotnet user-secrets set "Minio:Endpoint" "{host}:{port}"
    dotnet user-secrets set "Minio:AccessKey" "User Name"
    dotnet user-secrets set "Minio:SecretKey" "User Password"
    ```

### Сборка
```
dotnet build
```
## Использование
```
dotnet run
```

## Развёртка
TODO