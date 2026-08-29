# BlogMVC

A blog REST API built with ASP.NET Core and MongoDB.

## Features
- REST API for post management (`api/blog`) and JWT auth (`api/auth/register`, `api/auth/login`)
- ASP.NET Core Identity (SQLite) for user accounts, exchanged for JWTs via `api/auth/login`
- Optimistic concurrency on post edits via ETag/If-Match, so concurrent edits don't silently overwrite each other
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

1. `POST api/auth/register` with `{ "email": "...", "password": "..." }` to create an Identity
   account. The account is created locked out — an administrator must clear `LockoutEnd` on its
   `AspNetUsers` row before it can log in (there is no email confirmation flow).
2. `POST api/auth/login` with `{ "email": "...", "password": "..." }` for that account.
   Returns `{ "token": "..." }`.
3. Send that token as `Authorization: Bearer {token}` on the write endpoints of `api/blog`
   (POST, POST bulk, PUT, DELETE). Reading posts (GET) does not require a token.
4. Editing or deleting a post additionally requires the token's user to be that post's author.

## Concurrency

`GET api/blog/{id}` returns the post's version as an `ETag` response header. `PUT api/blog/{id}`
requires that value back as an `If-Match` request header: missing it returns 400, and a
mismatched (stale) value returns 412 Precondition Failed instead of silently overwriting a
concurrent edit.

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
