import { apiRequest } from '../../shared/api/apiClient'
import type { CompanyRole } from '../auth/authApi'

export type CurrentCompanyAccess = {
  companyId: number
  companyName: string
  role: CompanyRole
  permissions: string[]
}

export function getCurrentCompanyAccess(): Promise<CurrentCompanyAccess> {
  return apiRequest<CurrentCompanyAccess>('/api/companies/current/access')
}
