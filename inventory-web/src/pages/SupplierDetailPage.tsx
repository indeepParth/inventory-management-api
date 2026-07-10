import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getSupplier, type Supplier } from '../features/parties/partiesApi'
import { getErrorMessage } from '../shared/api/apiErrorMessages'

export function SupplierDetailPage() {
  const { id } = useParams()
  const [supplier, setSupplier] = useState<Supplier | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  useEffect(() => {
    async function loadSupplier(): Promise<void> {
      setIsLoading(true)
      setErrorMessage(null)

      try {
        setSupplier(await getSupplier(Number(id)))
      } catch (error) {
        setErrorMessage(getErrorMessage(error))
      } finally {
        setIsLoading(false)
      }
    }

    void loadSupplier()
  }, [id])

  return (
    <section className="content-panel" aria-labelledby="supplier-detail-title">
      <p className="page-kicker">Supplier</p>
      <h1 id="supplier-detail-title" className="page-title">{supplier?.name ?? 'Supplier detail'}</h1>
      {isLoading ? <p className="state-message">Loading supplier...</p> : null}
      {errorMessage ? <p className="form-error" role="alert">{errorMessage}</p> : null}
      {supplier ? (
        <div className="detail-grid">
          <span>Status</span><strong>{supplier.isActive ? 'Active' : 'Inactive'}</strong>
          <span>Contact person</span><strong>{supplier.contactPerson || '-'}</strong>
          <span>Phone</span><strong>{supplier.phone || '-'}</strong>
          <span>Email</span><strong>{supplier.email || '-'}</strong>
          <span>GST number</span><strong>{supplier.gstNumber || '-'}</strong>
          <span>Address</span><strong>{supplier.address || '-'}</strong>
        </div>
      ) : null}
      <p className="page-action"><Link className="text-link" to="/app/suppliers">Back to suppliers</Link></p>
    </section>
  )
}
