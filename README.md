# BlogMVC

A blog REST API built with ASP.NET Core and MongoDB.

## Features
- REST API for post management (`api/blog`) and JWT login (`api/auth/login`)
- ASP.NET Core Identity (SQLite) for user accounts, exchanged for JWTs via `api/auth/login`
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

1. Register an Identity user (e.g. via ASP.NET Core Identity's `UserManager`, or a
   registration endpoint you add) and confirm the account.
2. `POST api/auth/login` with `{ "email": "...", "password": "..." }` for that account.
   Returns `{ "token": "..." }`.
3. Send that token as `Authorization: Bearer {token}` on the write endpoints of `api/blog`
   (POST, POST bulk, PUT, DELETE). Reading posts (GET) does not require a token.
4. Editing or deleting a post additionally requires the token's user to be that post's author.

## Running Tests

```bash
dotnet test BlogMVC.sln
```

## Tech Stack
- ASP.NET Core Web API
- MongoDB
- ASP.NET Core Identity + JWT bearer authentication
- xUnit, Moq
- GitHub Actions CI/CD
- Codecov

## License
MIT
