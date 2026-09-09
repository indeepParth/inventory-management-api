import { apiRequest } from '../../shared/api/apiClient'

export type SubscriptionStatus =
  | 'Active'
  | 'Trialing'
  | 'PastDue'
  | 'Cancelled'
  | 'Expired'

export type SubscriptionPlan = {
  code: 'free'
  name: string
  maxCompanies: number
  maxUsersPerCompany: number
  maxInvoicesPerMonth: number
  maxProducts: number
  maxCustomers: number
}

export type CurrentSubscription = {
  status: SubscriptionStatus
  plan: SubscriptionPlan
  usage: {
    ownedCompanies: number
    companyUsers?: number
    invoicesThisMonth?: number
    products?: number
    customers?: number
  }
}

export function getCurrentSubscription(): Promise<CurrentSubscription> {
  return apiRequest<CurrentSubscription>('/api/subscription/current')
}
