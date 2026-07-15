# BlogMVC

A blog application built with ASP.NET Core MVC and MongoDB.

## Features
- View, create, edit, and delete blog posts (web UI, cookie-based Identity login)
- REST API for post management (`api/blog`) and JWT login (`api/auth/login`)
- Unit and integration tests

## Prerequisites
- .NET 10
- Docker

## Getting Started

```bash
docker compose up -d
dotnet run --project BlogMVC/BlogMVC.csproj
```

Before running, provide `Jwt:Key` (and optionally `Jwt:Issuer`/`Jwt:Audience`) through your
configuration mechanism of choice (environment variables, user-secrets, Docker/Kubernetes
secrets, ...) — these values are intentionally not committed in `appsettings.json`.

## REST API Authentication

1. `POST api/auth/login` with `{ "email": "...", "password": "..." }` for an existing,
   confirmed Identity account (register one via the web UI first). Returns `{ "token": "..." }`.
2. Send that token as `Authorization: Bearer {token}` on the write endpoints of `api/blog`
   (POST, POST bulk, PUT, DELETE). Reading posts (GET) does not require a token.
3. Editing or deleting a post additionally requires the token's user to be that post's author.

## Running Tests

```bash
dotnet test BlogMVC.sln
```

## Tech Stack
- ASP.NET Core MVC
- MongoDB
- ASP.NET Core Identity + JWT bearer authentication
- xUnit, Moq
- GitHub Actions CI/CD
- Codecov

## License
MIT