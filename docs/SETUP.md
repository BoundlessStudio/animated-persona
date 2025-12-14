# Setup Notes

1. **Auth0**
   - Create a Single Page Application.
   - Set callback URL to your SWA domain and `http://localhost:4280` (SWA CLI) or `http://localhost:5173` (Vite).
   - Enable an API with the configured audience; add that audience to `VITE_AUTH0_AUDIENCE` and `AUTH0_AUDIENCE`.

2. **Tavus**
   - Collect `TAVUS_API_KEY`, `TAVUS_PERSONA_ID`, and `TAVUS_REPLICA_ID` (if persona default replica not configured).
   - Conversations last 60 minutes; the frontend timer warns at 5 minutes remaining and shows a restart button after expiry.

3. **Fly Machines**
   - Set `FLY_API_TOKEN`, `FLY_APP_NAME`, and `FLY_REGION`.
   - The backend creates or starts a machine with a TCP service exposing port 3000 and returns `https://<app>.fly.dev` for the live preview iframe.
   - The `/api/fly/machines/exec/stream` endpoint emits server-sent events to stream logs from long running commands.

4. **Memory**
   - By default, memory is stored in-process. Provide `TABLE_STORAGE_CONNECTION_STRING` to persist user transcripts across restarts (table name defaults to `agentmemory`).

5. **Local run with SWA CLI**
   ```bash
   cd frontend && npm install
   cd ../api && dotnet restore
   swa start http://localhost:5173 --api-location ../api
   ```
