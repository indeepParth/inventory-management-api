import { apiRequest } from '../../shared/api/apiClient'
import type { CompanyRole, UserCompany } from '../auth/authApi'

export type CompanyInvitationStatus = 'Pending' | 'Accepted' | 'Revoked' | 'Expired'

export type CompanyInvitation = {
  id: number
  companyId: number
  companyName: string
  email: string
  role: CompanyRole
  status: CompanyInvitationStatus
  invitedByUserName: string
  createdAtUtc: string
  expiresAtUtc: string
  acceptedAtUtc?: string
}

export type CreateCompanyInvitationRequest = {
  email: string
  role: CompanyRole
}

export type CreateCompanyInvitationResponse = CompanyInvitation & {
  acceptUrl: string
}

export type AcceptInvitationRequest =
  | { mode: 'existingUser' }
  | {
      mode: 'newUser'
      userName: string
      password: string
      confirmPassword: string
    }

export function getCompanyInvitations(): Promise<CompanyInvitation[]> {
  return apiRequest<CompanyInvitation[]>('/api/company-invitations')
}

export function createCompanyInvitation(
  request: CreateCompanyInvitationRequest,
): Promise<CreateCompanyInvitationResponse> {
  return apiRequest<CreateCompanyInvitationResponse, CreateCompanyInvitationRequest>(
    '/api/company-invitations',
    {
      method: 'POST',
      body: request,
    },
  )
}

export function revokeCompanyInvitation(id: number): Promise<void> {
  return apiRequest<void>(`/api/company-invitations/${id}`, {
    method: 'DELETE',
  })
}

export function getCompanyInvitation(token: string): Promise<CompanyInvitation> {
  return apiRequest<CompanyInvitation>(
    `/api/company-invitations/${encodeURIComponent(token)}`,
    {
      skipCompanyAccessRecovery: true,
    },
  )
}

export function acceptCompanyInvitation(
  token: string,
  request: AcceptInvitationRequest,
): Promise<UserCompany> {
  return apiRequest<UserCompany, AcceptInvitationRequest>(
    `/api/company-invitations/${encodeURIComponent(token)}/accept`,
    {
      method: 'POST',
      body: request,
      skipCompanyAccessRecovery: true,
    },
  )
}
