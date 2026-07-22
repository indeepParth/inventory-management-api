import { useCallback, useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { getSupplier, getSupplierStatement, type StatementFilters, type Supplier } from '../features/parties/partiesApi'
import { PartyLedgerView } from '../features/parties/PartyLedgerView'
import { getErrorMessage } from '../shared/api/apiErrorMessages'
import { ErrorBanner, LoadingState } from '../shared/components/Feedback'

export function SupplierLedgerPage() {
  const { id } = useParams()
  const supplierId = Number(id)
  const [supplier, setSupplier] = useState<Supplier | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  useEffect(() => {
    async function loadSupplier(): Promise<void> {
      if (!supplierId) {
        setErrorMessage('Supplier not found.')
        setIsLoading(false)
        return
      }

      setIsLoading(true)
      setErrorMessage(null)

      try {
        setSupplier(await getSupplier(supplierId))
      } catch (error) {
        setErrorMessage(getErrorMessage(error))
      } finally {
        setIsLoading(false)
      }
    }

    void loadSupplier()
  }, [supplierId])

  const loadStatement = useCallback(
    (filters: StatementFilters) => getSupplierStatement(supplierId, filters),
    [supplierId],
  )

  if (isLoading) {
    return <LoadingState>Loading supplier...</LoadingState>
  }

  if (errorMessage) {
    return <ErrorBanner>{errorMessage}</ErrorBanner>
  }

  return (
    <PartyLedgerView
      backLabel="Back to supplier"
      backTo={`/app/suppliers/${supplierId}`}
      loadStatement={loadStatement}
      partyDetails={[
        { label: 'Status', value: supplier ? (supplier.isActive ? 'Active' : 'Inactive') : '' },
        { label: 'Contact person', value: supplier?.contactPerson ?? '' },
        { label: 'Phone', value: supplier?.phone ?? '' },
        { label: 'Email', value: supplier?.email ?? '' },
        { label: 'GST number', value: supplier?.gstNumber ?? '' },
        { label: 'Address', value: supplier?.address ?? '' },
      ]}
      partyName={supplier?.name ?? 'Supplier ledger'}
      title="Supplier ledger"
    />
  )
}
