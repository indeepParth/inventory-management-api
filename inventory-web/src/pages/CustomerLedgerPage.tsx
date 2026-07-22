import { useCallback, useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { getCustomer, getCustomerStatement, type Customer, type StatementFilters } from '../features/parties/partiesApi'
import { PartyLedgerView } from '../features/parties/PartyLedgerView'
import { getErrorMessage } from '../shared/api/apiErrorMessages'
import { ErrorBanner, LoadingState } from '../shared/components/Feedback'

export function CustomerLedgerPage() {
  const { id } = useParams()
  const customerId = Number(id)
  const [customer, setCustomer] = useState<Customer | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  useEffect(() => {
    async function loadCustomer(): Promise<void> {
      if (!customerId) {
        setErrorMessage('Customer not found.')
        setIsLoading(false)
        return
      }

      setIsLoading(true)
      setErrorMessage(null)

      try {
        setCustomer(await getCustomer(customerId))
      } catch (error) {
        setErrorMessage(getErrorMessage(error))
      } finally {
        setIsLoading(false)
      }
    }

    void loadCustomer()
  }, [customerId])

  const loadStatement = useCallback(
    (filters: StatementFilters) => getCustomerStatement(customerId, filters),
    [customerId],
  )

  if (isLoading) {
    return <LoadingState>Loading customer...</LoadingState>
  }

  if (errorMessage) {
    return <ErrorBanner>{errorMessage}</ErrorBanner>
  }

  return (
    <PartyLedgerView
      backLabel="Back to customer"
      backTo={`/app/customers/${customerId}`}
      loadStatement={loadStatement}
      partyDetails={[
        { label: 'Status', value: customer ? (customer.isActive ? 'Active' : 'Inactive') : '' },
        { label: 'Contact person', value: customer?.contactPerson ?? '' },
        { label: 'Phone', value: customer?.phone ?? '' },
        { label: 'Email', value: customer?.email ?? '' },
        { label: 'GST number', value: customer?.gstNumber ?? '' },
        { label: 'Billing address', value: customer?.billingAddress ?? '' },
        { label: 'Delivery address', value: customer?.deliveryAddress ?? '' },
      ]}
      partyName={customer?.name ?? 'Customer ledger'}
      title="Customer ledger"
    />
  )
}
