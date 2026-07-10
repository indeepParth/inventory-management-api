import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { clearAuthTokens, getStoredAuthTokens, setAuthTokens } from '../../shared/api/tokenStorage'
import {
  getCurrentUser,
  login as loginRequest,
  type CurrentUser,
  type LoginRequest,
  type LoginResponse,
} from './authApi'

type AuthState = {
  accessToken: string | null
  refreshToken: string | null
  expiresAt: string | null
  currentUser: CurrentUser | null
  isAuthenticated: boolean
  isAuthResolved: boolean
  isCurrentUserLoading: boolean
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
    currentUser: null,
    isAuthenticated: Boolean(storedTokens?.accessToken),
    isAuthResolved: !storedTokens?.accessToken,
    isCurrentUserLoading: Boolean(storedTokens?.accessToken),
  }
}

function mapLoginResponse(response: LoginResponse, currentUser: CurrentUser): AuthState {
  return {
    accessToken: response.accessToken,
    refreshToken: response.refreshToken,
    expiresAt: response.expiresAt,
    currentUser,
    isAuthenticated: true,
    isAuthResolved: true,
    isCurrentUserLoading: false,
  }
}

type AuthProviderProps = {
  children: ReactNode
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [authState, setAuthState] = useState<AuthState>(getInitialAuthState)

  const logout = useCallback((): void => {
    clearAuthTokens()
    setAuthState({
      accessToken: null,
      refreshToken: null,
      expiresAt: null,
      currentUser: null,
      isAuthenticated: false,
      isAuthResolved: true,
      isCurrentUserLoading: false,
    })
  }, [])

  const login = useCallback(async (request: LoginRequest): Promise<void> => {
    const response = await loginRequest(request)
    setAuthTokens(response)
    const currentUser = await getCurrentUser()
    setAuthState(mapLoginResponse(response, currentUser))
  }, [])

  useEffect(() => {
    if (!authState.accessToken || authState.currentUser || authState.isAuthResolved) {
      return
    }

    async function loadCurrentUser(): Promise<void> {
      try {
        const currentUser = await getCurrentUser()

        setAuthState((currentState) => ({
          ...currentState,
          currentUser,
          isAuthenticated: true,
          isAuthResolved: true,
          isCurrentUserLoading: false,
        }))
      } catch {
        logout()
      }
    }

    void loadCurrentUser()
  }, [authState.accessToken, authState.currentUser, authState.isAuthResolved, logout])

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
