# BlogMVC

A blog application built with ASP.NET Core MVC and MongoDB.

## Features
- View, create, edit, and delete blog posts
- REST API for post management
- Unit and integration tests

## Prerequisites
- .NET 10
- Docker

## Getting Started

```bash
docker compose up -d
dotnet run --project BlogMVC/BlogMVC.csproj
```

## Running Tests

```bash
dotnet test BlogMVC.sln
```

## Tech Stack
- ASP.NET Core MVC
- MongoDB
- xUnit, Moq
- GitHub Actions CI/CD
- Codecov

## License
MIT