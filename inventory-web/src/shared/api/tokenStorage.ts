const accessTokenKey = 'inventory-web.access-token'
const refreshTokenKey = 'inventory-web.refresh-token'
const accessTokenExpiresAtKey = 'inventory-web.access-token-expires-at'

export type StoredAuthTokens = {
  accessToken: string
  refreshToken: string
  expiresAt: string
}

export function getAccessToken(): string | null {
  return window.localStorage.getItem(accessTokenKey)
}

export function getRefreshToken(): string | null {
  return window.localStorage.getItem(refreshTokenKey)
}

export function getStoredAuthTokens(): StoredAuthTokens | null {
  const accessToken = window.localStorage.getItem(accessTokenKey)
  const refreshToken = window.localStorage.getItem(refreshTokenKey)
  const expiresAt = window.localStorage.getItem(accessTokenExpiresAtKey)

  if (!accessToken || !refreshToken || !expiresAt) {
    return null
  }

  return {
    accessToken,
    refreshToken,
    expiresAt,
  }
}

export function setAuthTokens(tokens: StoredAuthTokens): void {
  window.localStorage.setItem(accessTokenKey, tokens.accessToken)
  window.localStorage.setItem(refreshTokenKey, tokens.refreshToken)
  window.localStorage.setItem(accessTokenExpiresAtKey, tokens.expiresAt)
}

export function clearAuthTokens(): void {
  window.localStorage.removeItem(accessTokenKey)
  window.localStorage.removeItem(refreshTokenKey)
  window.localStorage.removeItem(accessTokenExpiresAtKey)
}
