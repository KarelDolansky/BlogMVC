# BlogMVC

A blog REST API built with ASP.NET Core and MongoDB.

## Features
- REST API for post management (`api/blog`) and JWT auth (`api/auth/register`, `api/auth/login`)
- ASP.NET Core Identity (SQLite) for user accounts, exchanged for JWTs via `api/auth/login`
- Permission-based post authorization (roles grant permissions — Administrator/Editor/Author can create,
  bulk creation excludes Author; editing/deleting requires ownership, except Administrator, who can manage
  any post; Commentator can't create/edit/delete)
- User listing and role assignment (`api/users`, `api/users/{id}/role`), restricted to Administrator
- Role administration (`api/roles`) — an Administrator can create roles and edit which permissions each one
  grants at runtime, no redeploy required
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
secrets, ...) — these values are intentionally not committed in `appsettings.json`. `Jwt:Key` must be at
least 32 bytes (UTF-8), as HMAC-SHA256 requires; the app refuses to start with a missing or shorter key.

`docker compose up -d` builds and runs the full stack — the `blogmvc` API container alongside MongoDB —
not just MongoDB, so it also needs `Jwt:Key`. Copy `.env.example` to `.env` and set `JWT_KEY` there; Compose
reads it automatically (`.env` is gitignored, so the real value never gets committed). The containerized
app's Identity data (SQLite) persists in a named `sqlite_data` volume, same as `mongo_data` does for posts —
both survive a `docker compose down`/`up` cycle.

## REST API Authentication

1. `POST api/auth/register` with `{ "email": "...", "password": "..." }` to create an Identity
   account (assigned the Commentator role; there is no email confirmation or admin approval step).
2. `POST api/auth/login` with `{ "email": "...", "password": "..." }` for that account.
   Returns `{ "token": "..." }`. A wrong password, an unknown email and a locked-out account all return
   the same 401 response, so the endpoint doesn't reveal which emails are registered. Both `api/auth`
   endpoints are rate-limited per client IP (10 requests per 60 seconds by default, configurable under
   `RateLimiting:Auth`); excess requests get 429 Too Many Requests.
3. Send that token as `Authorization: Bearer {token}` on the write endpoints of `api/blog`. Reading
   posts (GET) does not require a token. Creating a post (POST) requires the Administrator, Editor or
   Author role; bulk creation (POST bulk) requires Administrator or Editor — a Commentator token gets
   403 Forbidden on both. Editing/deleting (PUT, DELETE) requires the same Administrator/Editor/Author
   roles as creating (a Commentator token gets 403 Forbidden there too).
4. Editing or deleting a post requires the token's user to be that post's author — except an
   Administrator, who can edit or delete any post.
5. `PUT api/users/{id}/role` with `{ "role": "..." }` replaces a user's role; only an Administrator
   token can call it (403 Forbidden otherwise). Returns 404 if the user id doesn't exist, 400 if no role
   with that name exists (any role, not just a predefined one).
6. `GET api/users` lists every user with their id, username, and current role; same Administrator-only
   restriction as #5. Meant to feed a frontend role-management UI.
7. `GET api/roles` lists every role with its currently granted permissions; `GET api/roles/permissions`
   lists every permission a role can be granted. `POST api/roles` with
   `{ "name": "...", "permissions": [...] }` creates a role (409 on a duplicate name, 400 on an unrecognized
   permission). `PUT api/roles/{name}/permissions` with `{ "permissions": [...] }` replaces a role's entire
   permission set. `DELETE api/roles/{name}` deletes a role (409 if any user still holds it). All require an
   Administrator token.

## Architecture

For the request flow, folder structure, and layering, see [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

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
