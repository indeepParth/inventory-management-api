import { getApiBaseUrl } from '../config/apiConfig'
import {
  clearAuthTokens,
  getAccessToken,
  getRefreshToken,
  setAuthTokens,
  type StoredAuthTokens,
} from './tokenStorage'

type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'

type ApiRequestOptions<TBody> = {
  method?: HttpMethod
  body?: TBody
  headers?: HeadersInit
  signal?: AbortSignal
  skipAuthRefresh?: boolean
}

type RefreshTokenRequest = {
  refreshToken: string
}

export type ProblemDetails = {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  readonly status: number
  readonly problemDetails?: ProblemDetails

  constructor(status: number, message: string, problemDetails?: ProblemDetails) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.problemDetails = problemDetails
  }
}

async function parseResponse(response: Response): Promise<unknown> {
  if (response.status === 204) {
    return undefined
  }

  const contentType = response.headers.get('content-type')

  if (contentType?.includes('application/json')) {
    return response.json()
  }

  return response.text()
}

function buildHeaders(hasBody: boolean, headers?: HeadersInit): Headers {
  const requestHeaders = new Headers(headers)
  const accessToken = getAccessToken()

  if (hasBody && !requestHeaders.has('Content-Type')) {
    requestHeaders.set('Content-Type', 'application/json')
  }

  if (!requestHeaders.has('Accept')) {
    requestHeaders.set('Accept', 'application/json')
  }

  if (accessToken) {
    requestHeaders.set('Authorization', `Bearer ${accessToken}`)
  }

  return requestHeaders
}

function buildUrl(path: string): string {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  return `${getApiBaseUrl()}${normalizedPath}`
}

function redirectToLogin(): void {
  if (window.location.pathname !== '/login') {
    window.location.assign('/login')
  }
}

function getErrorMessage(status: number, responseBody: unknown): string {
  if (isProblemDetails(responseBody)) {
    return responseBody.title ?? responseBody.detail ?? `Request failed with status ${status}.`
  }

  if (typeof responseBody === 'string' && responseBody.trim()) {
    return responseBody
  }

  return `Request failed with status ${status}.`
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  if (!value || typeof value !== 'object') {
    return false
  }

  return 'title' in value || 'detail' in value || 'errors' in value || 'status' in value
}

async function sendRequest<TBody>(
  path: string,
  options: ApiRequestOptions<TBody>,
): Promise<Response> {
  const hasBody = options.body !== undefined

  return fetch(buildUrl(path), {
    method: options.method ?? 'GET',
    headers: buildHeaders(hasBody, options.headers),
    body: hasBody ? JSON.stringify(options.body) : undefined,
    signal: options.signal,
  })
}

async function refreshAccessToken(): Promise<boolean> {
  const refreshToken = getRefreshToken()

  if (!refreshToken) {
    return false
  }

  const response = await fetch(buildUrl('/api/Auth/RefreshToken'), {
    method: 'POST',
    headers: buildHeaders(true),
    body: JSON.stringify({ refreshToken } satisfies RefreshTokenRequest),
  })

  if (!response.ok) {
    return false
  }

  const responseBody = await parseResponse(response)

  if (!isStoredAuthTokens(responseBody)) {
    return false
  }

  setAuthTokens(responseBody)
  return true
}

function isStoredAuthTokens(value: unknown): value is StoredAuthTokens {
  if (!value || typeof value !== 'object') {
    return false
  }

  const tokens = value as Record<string, unknown>

  return (
    typeof tokens.accessToken === 'string' &&
    typeof tokens.refreshToken === 'string' &&
    typeof tokens.expiresAt === 'string'
  )
}

async function handleAuthFailure(): Promise<void> {
  clearAuthTokens()
  redirectToLogin()
}

export async function apiRequest<TResponse, TBody = unknown>(
  path: string,
  options: ApiRequestOptions<TBody> = {},
): Promise<TResponse> {
  let response = await sendRequest(path, options)

  if (response.status === 401 && !options.skipAuthRefresh) {
    const refreshed = await refreshAccessToken()

    if (refreshed) {
      response = await sendRequest(path, {
        ...options,
        skipAuthRefresh: true,
      })
    } else {
      await handleAuthFailure()
    }
  }

  const responseBody = await parseResponse(response)

  if (!response.ok) {
    if (response.status === 401 && !options.skipAuthRefresh) {
      await handleAuthFailure()
    }

    throw new ApiError(
      response.status,
      getErrorMessage(response.status, responseBody),
      isProblemDetails(responseBody) ? responseBody : undefined,
    )
  }

  return responseBody as TResponse
}
