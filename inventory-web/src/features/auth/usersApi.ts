import { apiRequest } from '../../shared/api/apiClient'
import type { AppRole } from './roleAccess'

export type UserAccount = {
  id: string
  userName: string
  email: string
  roles: string[]
  isDisabled: boolean
}

export type CreateUserRequest = {
  userName: string
  email: string
  password: string
  roles: AppRole[]
}

export type ChangePasswordRequest = {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}

export function getUsers(): Promise<UserAccount[]> {
  return apiRequest<UserAccount[]>('/api/Users')
}

export function createUser(request: CreateUserRequest): Promise<UserAccount> {
  return apiRequest<UserAccount, CreateUserRequest>('/api/Users', {
    method: 'POST',
    body: request,
  })
}

export function assignUserRole(userId: string, role: AppRole): Promise<UserAccount> {
  return apiRequest<UserAccount, { role: AppRole }>(`/api/Users/${userId}/roles`, {
    method: 'POST',
    body: { role },
  })
}

export function removeUserRole(userId: string, role: AppRole): Promise<UserAccount> {
  return apiRequest<UserAccount>(`/api/Users/${userId}/roles/${role}`, {
    method: 'DELETE',
  })
}

export function disableUser(userId: string): Promise<UserAccount> {
  return apiRequest<UserAccount>(`/api/Users/${userId}/disable`, {
    method: 'POST',
  })
}

export function enableUser(userId: string): Promise<UserAccount> {
  return apiRequest<UserAccount>(`/api/Users/${userId}/enable`, {
    method: 'POST',
  })
}

export function changePassword(request: ChangePasswordRequest): Promise<void> {
  return apiRequest<void, ChangePasswordRequest>('/api/Users/me/change-password', {
    method: 'POST',
    body: request,
  })
}
