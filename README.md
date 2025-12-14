# Animated Persona Sample

This repo contains a minimal Azure Static Web Apps monorepo with a React + Tailwind frontend and Azure Functions (.NET 8 isolated worker) backend. It wires together Auth0 authentication, Tavus CVI conversations, a Vercel-style AI chat transcript UI, Fly.io Machines controls, and a basic agent loop with streaming logs.

## Structure
- `frontend/` – React + TypeScript (Vite) app that runs inside Azure Static Web Apps
- `api/` – Azure Functions .NET 8 isolated worker HTTP APIs
- `docs/` – setup notes

## Prerequisites
- Node 18+ and npm
- .NET 8 SDK
- Azure Functions Core Tools (for local API)
- Azure Static Web Apps CLI (`npm install -g @azure/static-web-apps-cli`)

## Environment variables

### Frontend (`frontend/.env`)
```
VITE_AUTH0_DOMAIN=your-tenant.us.auth0.com
VITE_AUTH0_CLIENT_ID=your-client-id
VITE_AUTH0_AUDIENCE=https://api.yourapp
VITE_API_BASE_URL=/api
```

### Backend (`api/local.settings.json` values)
```
AUTH0_DOMAIN=your-tenant.us.auth0.com
AUTH0_AUDIENCE=https://api.yourapp
TAVUS_API_KEY=your-tavus-api-key
TAVUS_PERSONA_ID=persona-id
TAVUS_REPLICA_ID=replica-id
FLY_API_TOKEN=fly-api-token
FLY_APP_NAME=fly-app
FLY_REGION=iad
OPENAI_API_KEY=sk-...
OPENAI_MODEL=codex
TABLE_STORAGE_CONNECTION_STRING=UseDevelopmentStorage=true
TABLE_STORAGE_TABLE=agentmemory
```

## Local development
1. Install dependencies
   ```bash
   cd frontend && npm install
   cd ../api && dotnet restore
   ```
2. Run Functions locally
   ```bash
   cd api
   func start
   ```
3. In a new terminal, run the Static Web Apps emulator
   ```bash
   cd frontend
   npm run dev
   ```
   Or use the SWA CLI to proxy both:
   ```bash
   swa start http://localhost:5173 --api-location ../api
   ```

The frontend automatically starts a Tavus conversation after Auth0 login, shows a 60-minute timer, and lets you start/stop Fly Machines and view live previews. Chat messages persist per Auth0 user.

## Deployment
- Configure Azure Static Web Apps with `app_location: frontend` and `api_location: api`.
- Set the environment variables above in your Azure Static Web App and Functions environment.
- Deploy with your preferred pipeline; SWA will build the frontend and publish the `api` Functions project.

## Tavus timer behavior
Conversations are assumed to last 60 minutes. The UI shows a countdown, warns at 5 minutes remaining, disables Hang Up after expiry, and offers **Start New Conversation** to restart immediately. The backend `end` endpoint calls the Tavus API when available and otherwise treats the session as client-complete.
