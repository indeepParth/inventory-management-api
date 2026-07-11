# Inventory Web

React + TypeScript + Vite frontend for the Inventory Management backend.

## Local development

Create a local environment file:

```bash
cp .env.example .env.local
```

Set the backend API base URL:

```bash
VITE_API_BASE_URL=https://localhost:5001
```

Install dependencies:

```bash
npm install
```

Start the local dev server:

```bash
npm run dev
```

Build for production:

```bash
npm run build
```

The production build writes static assets to:

```bash
dist/
```

## Environment variables

The frontend reads the backend API URL from `VITE_API_BASE_URL`.

Local example:

```bash
VITE_API_BASE_URL=https://localhost:5001
```

Production example:

```bash
VITE_API_BASE_URL=https://api.example.com
```

Set production values in the hosting provider, deployment environment, or CI
before running `npm run build`. Vite embeds `VITE_` variables into browser
assets at build time, so do not put secrets in them.

## Production build

From `inventory-web`:

```bash
npm install
npm run build
```

Deploy the generated `dist/` directory with any static web host. The backend API
base URL remains configurable through `VITE_API_BASE_URL`; no production URL is
hardcoded in the source.

This frontend is built separately from the backend. Backend Docker configuration
is intentionally unchanged.

## Docker static hosting

The frontend has its own Docker image and is not merged into the backend
container. The image builds the Vite app with Node, then serves the generated
`dist/` files from nginx with SPA fallback to `index.html`.

Build the frontend image from `inventory-web`:

```bash
docker build --build-arg VITE_API_BASE_URL=https://localhost:5001 -t inventory-web:local .
```

Run it locally:

```bash
docker run --rm -p 8080:80 inventory-web:local
```

Open:

```bash
http://localhost:8080
```

For production, replace `VITE_API_BASE_URL` with the public backend API URL at
image build time. Do not pass secrets through `VITE_` build arguments because
Vite embeds them into the browser JavaScript.
