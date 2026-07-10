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

Backend API integration is intentionally not connected yet.
