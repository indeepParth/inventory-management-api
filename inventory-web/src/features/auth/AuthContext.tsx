import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import {
  clearActiveCompanyId,
  clearAuthTokens,
  getActiveCompanyId,
  getStoredAuthTokens,
  setActiveCompanyId,
  setAuthTokens,
} from '../../shared/api/tokenStorage'
import { registerCompanyAccessLostHandler } from '../../shared/api/apiClient'
import {
  getCurrentUser,
  login as loginRequest,
  type CurrentUser,
  type LoginRequest,
  type LoginResponse,
  type UserCompany,
} from './authApi'

type AuthState = {
  accessToken: string | null
  refreshToken: string | null
  expiresAt: string | null
  currentUser: CurrentUser | null
  activeCompany: UserCompany | null
  isAuthenticated: boolean
  isAuthResolved: boolean
  isCurrentUserLoading: boolean
}

type AuthContextValue = AuthState & {
  companies: UserCompany[]
  login: (request: LoginRequest) => Promise<void>
  logout: () => void
  refreshCurrentUser: () => Promise<CurrentUser>
  selectCompany: (companyId: number) => Promise<void>
  handleCompanyAccessLost: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

function getInitialAuthState(): AuthState {
  const storedTokens = getStoredAuthTokens()

  return {
    accessToken: storedTokens?.accessToken ?? null,
    refreshToken: storedTokens?.refreshToken ?? null,
    expiresAt: storedTokens?.expiresAt ?? null,
    currentUser: null,
    activeCompany: null,
    isAuthenticated: Boolean(storedTokens?.accessToken),
    isAuthResolved: !storedTokens?.accessToken,
    isCurrentUserLoading: Boolean(storedTokens?.accessToken),
  }
}

function resolveActiveCompany(currentUser: CurrentUser): UserCompany | null {
  const storedCompanyId = getActiveCompanyId()
  const storedCompany = storedCompanyId === null
    ? undefined
    : currentUser.companies.find((company) => company.id === storedCompanyId)

  if (storedCompany) {
    return storedCompany
  }

  if (currentUser.companies.length === 1) {
    const [company] = currentUser.companies
    setActiveCompanyId(company.id)

    return company
  }

  clearActiveCompanyId()
  return null
}

function mapLoginResponse(response: LoginResponse, currentUser: CurrentUser): AuthState {
  return {
    accessToken: response.accessToken,
    refreshToken: response.refreshToken,
    expiresAt: response.expiresAt,
    currentUser,
    activeCompany: resolveActiveCompany(currentUser),
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
      activeCompany: null,
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

  const refreshCurrentUser = useCallback(async (): Promise<CurrentUser> => {
    const currentUser = await getCurrentUser()

    setAuthState((currentState) => ({
      ...currentState,
      currentUser,
      activeCompany: resolveActiveCompany(currentUser),
      isAuthenticated: true,
      isAuthResolved: true,
      isCurrentUserLoading: false,
    }))

    return currentUser
  }, [])

  const selectCompany = useCallback(async (companyId: number): Promise<void> => {
    const company = authState.currentUser?.companies.find(
      (candidate) => candidate.id === companyId,
    )

    if (!company) {
      clearActiveCompanyId()
      await refreshCurrentUser()
      return
    }

    setActiveCompanyId(company.id)
    setAuthState((currentState) => ({
      ...currentState,
      activeCompany: company,
    }))
  }, [authState.currentUser?.companies, refreshCurrentUser])

  const handleCompanyAccessLost = useCallback(async (): Promise<void> => {
    try {
      await refreshCurrentUser()
    } catch {
      logout()
    }
  }, [logout, refreshCurrentUser])

  useEffect(() => {
    if (!authState.accessToken || authState.currentUser || authState.isAuthResolved) {
      return
    }

    async function loadCurrentUser(): Promise<void> {
      try {
        await refreshCurrentUser()
      } catch {
        logout()
      }
    }

    void loadCurrentUser()
  }, [
    authState.accessToken,
    authState.currentUser,
    authState.isAuthResolved,
    logout,
    refreshCurrentUser,
  ])

  useEffect(() => {
    registerCompanyAccessLostHandler(handleCompanyAccessLost)

    return () => registerCompanyAccessLostHandler(null)
  }, [handleCompanyAccessLost])

  const value = useMemo<AuthContextValue>(
    () => ({
      ...authState,
      companies: authState.currentUser?.companies ?? [],
      login,
      logout,
      refreshCurrentUser,
      selectCompany,
      handleCompanyAccessLost,
    }),
    [
      authState,
      handleCompanyAccessLost,
      login,
      logout,
      refreshCurrentUser,
      selectCompany,
    ],
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
