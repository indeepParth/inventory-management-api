const missingApiBaseUrlMessage =
  'Missing VITE_API_BASE_URL. Create inventory-web/.env.local and set VITE_API_BASE_URL=https://localhost:5001.'

export function getApiBaseUrl(): string {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim()

  if (!apiBaseUrl) {
    throw new Error(missingApiBaseUrlMessage)
  }

  return apiBaseUrl.replace(/\/+$/, '')
}
