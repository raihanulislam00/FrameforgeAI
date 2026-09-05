# Frameforge AI Video Maker

A full-stack AI video production workspace. Users can register, chat with Gemini about a video idea, review a structured plan, queue asynchronous video generation, and browse generated videos.

## Stack

- ASP.NET Core Web API on .NET 10
- Clean Architecture: API, Application, Domain, Infrastructure, Contracts
- PostgreSQL and Entity Framework Core
- JWT authentication with refresh tokens
- Gemini integration through a backend-only service
- Hosted background worker for video jobs
- SignalR hub at `/hubs/videos`
- Next.js App Router, React, and TypeScript frontend
- Local file storage with a mock video provider for development

## Project Layout

```text
backend/
  src/VideoMaker.API
  src/VideoMaker.Application
  src/VideoMaker.Contracts
  src/VideoMaker.Domain
  src/VideoMaker.Infrastructure
  tests/VideoMaker.Tests
frontend/
storage/
```

## Configuration

The backend reads environment variables using ASP.NET Core configuration. Local development values belong in `backend/.env`; the file is ignored by Git. Copy the template when needed:

```bash
cp backend/.env.example backend/.env
```

Set `GEMINI_API_KEY` in that file. Never put the key in the frontend or commit it. The mock provider remains available when no video provider is configured.

The frontend uses:

```text
NEXT_PUBLIC_API_URL=http://localhost:5101
```

Set this in `frontend/.env.local` if the API runs on another port.

## Run Locally

Start PostgreSQL first. The default connection is:

```text
Host=localhost;Port=5432;Database=videomaker;Username=postgres;Password=postgres
```

Then run the API and frontend in separate terminals:

```bash
set -a
source backend/.env
set +a
dotnet run --project backend/src/VideoMaker.API --launch-profile http
```

```bash
cd frontend
npm install
npm run dev
```

Open <http://localhost:3000>. Swagger is available at <http://localhost:5101/swagger>.

## Main API Routes

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `GET /api/auth/me`
- `GET /api/dashboard`
- `POST /api/ai/chat`
- `GET /api/conversations`
- `GET /api/videos`
- `POST /api/videos`
- `GET /api/videos/{id}`
- `GET /api/videos/{id}/status`
- `POST /api/videos/{id}/cancel`

All non-authenticated routes require a JWT bearer token. Video and conversation queries enforce ownership using the authenticated user ID.

## Background Generation

Creating a video stores a pending job and returns immediately. `VideoGenerationBackgroundWorker` claims pending jobs and invokes `MockVideoGenerationService`, which creates local SVG thumbnails and a sample MP4. A real provider can be added behind `IVideoGenerationService` without changing the API or frontend.

## Checks

```bash
dotnet restore backend/VideoMaker.slnx
dotnet build backend/VideoMaker.slnx
cd frontend && npm run build
```

## Current Development Notes

The local mock provider is intentionally used when no paid video provider is configured. Production deployment should add a managed PostgreSQL instance, a real object store, a real video provider, HTTPS, rate limiting, secret management, and integration tests.
