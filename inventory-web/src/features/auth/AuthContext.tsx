import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { clearAuthTokens, getStoredAuthTokens, setAuthTokens } from '../../shared/api/tokenStorage'
import { login as loginRequest, type LoginRequest, type LoginResponse } from './authApi'

type AuthState = {
  accessToken: string | null
  refreshToken: string | null
  expiresAt: string | null
  isAuthenticated: boolean
}

type AuthContextValue = AuthState & {
  login: (request: LoginRequest) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

function getInitialAuthState(): AuthState {
  const storedTokens = getStoredAuthTokens()

  return {
    accessToken: storedTokens?.accessToken ?? null,
    refreshToken: storedTokens?.refreshToken ?? null,
    expiresAt: storedTokens?.expiresAt ?? null,
    isAuthenticated: Boolean(storedTokens?.accessToken),
  }
}

function mapLoginResponse(response: LoginResponse): AuthState {
  return {
    accessToken: response.accessToken,
    refreshToken: response.refreshToken,
    expiresAt: response.expiresAt,
    isAuthenticated: true,
  }
}

type AuthProviderProps = {
  children: ReactNode
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [authState, setAuthState] = useState<AuthState>(getInitialAuthState)

  const login = useCallback(async (request: LoginRequest): Promise<void> => {
    const response = await loginRequest(request)
    setAuthTokens(response)
    setAuthState(mapLoginResponse(response))
  }, [])

  const logout = useCallback((): void => {
    clearAuthTokens()
    setAuthState({
      accessToken: null,
      refreshToken: null,
      expiresAt: null,
      isAuthenticated: false,
    })
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      ...authState,
      login,
      logout,
    }),
    [authState, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)

  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider.')
  }

  return context
}
