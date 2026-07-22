import { apiRequest } from '../../shared/api/apiClient'

export type CompanyProfile = {
  companyName: string
  address?: string
  gstNumber?: string
  email?: string
  phone?: string
  website?: string
  createdAtUtc?: string
  updatedAtUtc?: string
}

export type CompanyProfileFormValues = {
  companyName: string
  address: string
  gstNumber: string
  email: string
  phone: string
  website: string
}

export function getCompanyProfile(): Promise<CompanyProfile> {
  return apiRequest<CompanyProfile>('/api/company-profile')
}

export function updateCompanyProfile(
  values: CompanyProfileFormValues,
): Promise<CompanyProfile> {
  return apiRequest<CompanyProfile, CompanyProfileFormValues>('/api/company-profile', {
    method: 'PUT',
    body: values,
  })
}
