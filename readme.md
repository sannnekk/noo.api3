# Noo.Api 3.0

Backend API for Noo project.

## Requirements

- .NET 9.0 or higher
- MySQL 8.4 or higher

## Installation

1. Clone or pull the repository
2. Open the solution in Visual Studio or VS Code
3. Restore the NuGet packages
   ```bash
   dotnet restore
   ```
4. Create a new MySQL database
   ```sql
   CREATE DATABASE `noo_example_db` CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
   ```
5. Update the connection string and all the other settings in `appsettings.json` file in each project (example below)
6. Run the migrations to create the database schema:
   ```bash
   dotnet ef database update
   ```
7. Run or Debug the application

   ```bash
   ./scripts.sh run dev
   ```

   Possible commands:

   - `test integration`
   - `test unit`
   - `run dev`
   - `run prod`

   To debug, use options from your IDE (Typically `F5` in VS code)

8. Open the Swagger UI in your browser (usually at `http://localhost:5001/api-docs`, the path can be configured in `appsettings.json`)

## Configuration

The configuration is done in the `appsettings.json` file. An example (all possible fields) of the configuration file is provided below. You can copy it to your own `appsettings.json` file and modify it as needed:

```json
{
  "App": {
    "Location": "https://localhost:5001",
    "AppVersion": "1.0.0",
    "AuthenticationType": "Bearer",
    "BaseUrl": "https://noo-school.ru",
    "AllowedOrigins": ["http://localhost:5189"],
    "UserOnlineThresholdMinutes": 15,
    "UserActiveThresholdDays": 14,
    "HashSecret": "s3cr3tH4shK3y!"
  },
  "Http": {
    "Port": 5001,
    "MaximumRequestSize": 10485760,
    "CacheSizeLimit": 104857600,
    "MaximumCacheBodySize": 1048576,
    "MinRequestBodyDataRate": 100,
    "MinRequestBodyDataRateGracePeriod": 10,
    "MinResponseDataRate": 100,
    "MinResponseDataRateGracePeriod": 10
  },
  "HttpClient": {
    "TimeoutSeconds": 30,
    "RetryCount": 3,
    "RetryBaseDelayMilliseconds": 200,
    "CircuitBreakerFailures": 5,
    "CircuitBreakerDurationSeconds": 30
  },
  "Logs": {
    "Mode": "Console", // Console, Telegram
    "LogLevel": "Information",
    "TelegramLogToken": "...",
    "TelegramLogChatIds": ["...", "..."]
  },
  "Db": {
    "User": "...",
    "Password": "...",
    "Host": "localhost",
    "Port": "3316",
    "Database": "noo_next",
    "CommandTimeout": 60,
    "DefaultCharset": "utf8mb4",
    "DefaultCollation": "utf8mb4_general_ci"
  },
  "Cache": {
    "ConnectionString": "localhost:6379",
    "DefaultCacheTime": 60,
    "Prefix": "noo:"
  },
  "Jwt": {
    "Secret": "...",
    "Issuer": "https://localhost:5001",
    "Audience": "https://localhost:5001",
    "ExpireDays": 60
  },
  "Telegram": {
    "Token": "...",
    "WaitAfterBatch": 100,
    "WaitForBatch": 1000
  },
  "Swagger": {
    "Title": "API v1",
    "Description": "Noo.Api module",
    "Endpoint": "/swagger/v1/swagger.json",
    "RoutePrefix": "api-docs",
    "EnableUI": true,
    "Version": "v1"
  },
  "Email": {
    "SmtpHost": "localhost",
    "SmtpPort": 1025,
    "SmtpUsername": "...",
    "SmtpPassword": "...",
    "SmtpTimeout": 15000,
    "FromEmail": "...",
    "FromName": "..."
  },
  "AllowedHosts": "*",
  "Events": {
    "QueueCapacity": 2048
  }
}
```

## Optional

Those are optional requirements that are needed for some features to work. All of them can be disabled.

- Docker
- Redis
- RabbitMQ (or other message broker, not decided yet) [Not implemented yet]
- SMTP Server [Not implemented yet]
- Telegram Bot for notifications [Not implemented yet]
- Telegram Bot for logs

## Code practices

The following practices MUST be followed in the code:

### Avoid cross product problem when selecting from db

It is very very important to avoid the cross product problem when selecting from the database. Example:
A CarManufacturer has a list of Card and a list of Employees. If you select all the cars and all the employees, you will get a cross product of the two lists, which will result in a huge amount of data being transferred from the database as it returns a combination of each car with each employee (cross product). If a manufacturer has 100 cars and 100 employees, the query will return 10,000 rows. This is a very common mistake, so be careful with it.

### Avoid N+1 problem when selecting from db

The N+1 problem occurs when you select a list of entities and then for each entity you make a separate query to the database to get related entities. This results in N+1 queries, where N is the number of entities in the list. This can lead to performance issues and should be avoided. Instead, use `Include` to load related entities in a single query. For example, if you have a list of cars and you want to load their manufacturers, use `Include` to load the manufacturers in the same query.

❗ If you are using `Include`, make sure to use it only when necessary, as it can lead to performance issues if used excessively. Use `ThenInclude` to load nested related entities. Strictly avoid cross product problem when using `Include`.

### Use `IEnumerable` for immutable collections and `ICollection` for mutable collections

When defining a collection in a class, use `IEnumerable<T>` for immutable collections and `ICollection<T>` for mutable collections. This will help to avoid confusion and make the code more explicit. If you need to modify the collection, use `ICollection<T>`, otherwise use `IEnumerable<T>`. This will also help to avoid issues with serialization and deserialization of the collections.
Always use IEnumerable in DTOs as they are ment to be immutable.

### Prefer performance over readability or architecture

The most important thing is performance. If you can make the code more performant by sacrificing readability or architecture, do it. The code should be readable, but performance is the priority. Next comes resource usage, and only then comes architecture.

### Use async/await for I/O operations

Always use `async` and `await` for I/O operations, such as database queries, file operations, network requests, etc. This will help to avoid blocking the thread and improve the performance of the application. DO NOT use `Task.Run` for I/O operations, as it will not improve performance and may even degrade it. Also do not use async/await for CPU-bound operations, as it will not improve performance and may even degrade it.

### Before creating a new feature, check if it is already implemented in Core

If you want to create a new feature, first check if it is already implemented in the Core module. If it is, use it instead of creating a new one. This will help to avoid code duplication and improve the maintainability of the code.
Sometimes the Core module may not have the feature you need, but it is present in some other module. In this case, abstract and move the feature in the Core so it can be reused in other modules.

### Use interfaces for dependencies

Always use interfaces for dependencies, especially for services and repositories. This will help to decouple the code and make it more testable. Use dependency injection to inject the dependencies into the classes that need them. Do not use concrete classes as dependencies, as it will make the code less flexible and harder to test.
The only exception is a transient small class that is tiny and DEFINETELY not going to grow in the future. In this case, you can use a concrete class as a dependency, but it is not recommended.

### Always prefer explicit over implicit

For example, consider EF Core. Having a model class it can automatically create a table for it with right column and table names. However you SHOULD NOT rely on this feature. Instead, you should always use `Column` attribute to specfy the name and type of the column, and `Table` attribute to specify the name of the table. This will help to avoid confusion and make the code more explicit. The same applies to other frameworks and libraries.
Exceptions:

- `var` keyword for local variables
- model-to-model relationships in EF Core, they will be generated automatically

### Do not concentrate too much logic in one method/class

If a method or class is doing too much, it is a sign that it needs to be refactored. Split the method or class into smaller ones that do one thing only. This will help to improve the readability and maintainability of the code.
❗First think or performance. If splitting the method or class will degrade performance, do not do it.

### Do not query the whole entity if you need only a few fields

When querying the database, do not query the whole entity if you need only a few fields. Instead, use `Select` to select only the fields you need. This will help to reduce the amount of data transferred from the database and improve the performance of the application.

### Use `NooException` and derived classes for exceptions

Always use `NooException` and its derived classes (eg. `NotFoundException`) for exceptions in the Noo project. This will help to unify the exception handling and make it easier to catch and handle exceptions in a consistent way. Do not use built-in exceptions or custom exceptions that do not derive from `NooException`.
It is used to log the errors. If a controller action throws an exception, it will be caught by the global exception handler. If it is a `NooException`, it indicated that everything is normal and the error is expected. If it is not a `NooException`, it indicates that something went wrong and the error will be automatically logged.

### Use wrappers for all the third-party libraries

Always use wrappers for all the third-party libraries, such as EF Core, Redis, RabbitMQ, etc. This will help to decouple the code from the third-party libraries and make it more testable. It will also help to avoid issues with the third-party libraries, such as breaking changes or bugs. The wrappers should be implemented in the Core module and used in other modules.

## To refactor

- [ ] Move `Core` to a separate project
- [ ] Remove existence checks on delete

## Ideas

- [ ] Use ETags for caching and concurrency control
- [ ] Use feature flags
- [x] Implement rate limiting
- [ ] Add open telemetry
