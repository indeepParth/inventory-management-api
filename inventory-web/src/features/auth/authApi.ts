import { apiRequest } from '../../shared/api/apiClient'

export type LoginRequest = {
  userName: string
  password: string
}

export type LoginResponse = {
  accessToken: string
  refreshToken: string
  expiresAt: string
}

export type CompanyRole = 'Owner' | 'Manager' | 'Sales' | 'Inventory'

export type UserCompany = {
  id: number
  name: string
  role: CompanyRole
}

export type CurrentUser = {
  username: string
  email?: string
  isDisabled: boolean
  companies: UserCompany[]
}

export function login(request: LoginRequest): Promise<LoginResponse> {
  return apiRequest<LoginResponse, LoginRequest>('/api/Auth/Login', {
    method: 'POST',
    body: request,
    skipAuthRefresh: true,
  })
}

export function getCurrentUser(): Promise<CurrentUser> {
  return apiRequest<CurrentUser>('/api/Users/me')
}
