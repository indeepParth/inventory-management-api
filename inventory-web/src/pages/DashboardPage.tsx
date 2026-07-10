import { useCallback, useEffect, useState } from 'react'
import {
  getCurrentStock,
  getPurchaseRegister,
  getSalesRegister,
  type CurrentStockItem,
  type RegisterSummary,
} from '../features/reports/reportsApi'
import { getErrorMessage } from '../shared/api/apiErrorMessages'
import { ErrorBanner, LoadingState } from '../shared/components/Feedback'
import { formatCurrency } from '../shared/utils/formatters'

function emptySummary(): RegisterSummary {
  return {
    documentCount: 0,
    totalQuantity: 0,
    subtotal: 0,
    discount: 0,
    taxAmount: 0,
    otherCharges: 0,
    grandTotal: 0,
    paidAmount: 0,
    outstandingAmount: 0,
  }
}

export function DashboardPage() {
  const [stockItems, setStockItems] = useState<CurrentStockItem[]>([])
  const [purchaseSummary, setPurchaseSummary] = useState<RegisterSummary>(emptySummary)
  const [salesSummary, setSalesSummary] = useState<RegisterSummary>(emptySummary)
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const loadDashboard = useCallback(async (): Promise<void> => {
    setIsLoading(true)
    setErrorMessage(null)

    try {
      const [stock, purchases, sales] = await Promise.all([
        getCurrentStock(1, 100),
        getPurchaseRegister({ pageNumber: 1, pageSize: 5, fromDate: '', toDate: '' }),
        getSalesRegister({ pageNumber: 1, pageSize: 5, fromDate: '', toDate: '' }),
      ])
      setStockItems(stock.items)
      setPurchaseSummary(purchases.summary)
      setSalesSummary(sales.summary)
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadDashboard()
  }, [loadDashboard])

  const stockValue = stockItems.reduce((total, item) => total + item.stockValue, 0)
  const positiveStockCount = stockItems.filter((item) => item.quantity > 0).length

  return (
    <section className="content-panel wide-panel" aria-labelledby="dashboard-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">Dashboard</p>
          <h1 id="dashboard-title" className="page-title">Overview</h1>
        </div>
      </div>

      {isLoading ? <LoadingState>Loading dashboard...</LoadingState> : null}
      {errorMessage ? <ErrorBanner>{errorMessage}</ErrorBanner> : null}

      {!isLoading && !errorMessage ? (
        <div className="summary-grid">
          <article className="summary-card">
            <span>Current stock value</span>
            <strong>{formatCurrency(stockValue)}</strong>
            <small>{positiveStockCount} products with stock</small>
          </article>
          <article className="summary-card">
            <span>Purchases</span>
            <strong>{formatCurrency(purchaseSummary.grandTotal)}</strong>
            <small>{purchaseSummary.documentCount} documents</small>
          </article>
          <article className="summary-card">
            <span>Sales</span>
            <strong>{formatCurrency(salesSummary.grandTotal)}</strong>
            <small>{salesSummary.documentCount} invoices</small>
          </article>
          <article className="summary-card">
            <span>Receivables</span>
            <strong>{formatCurrency(salesSummary.outstandingAmount)}</strong>
            <small>Outstanding sales balance</small>
          </article>
          <article className="summary-card">
            <span>Payables</span>
            <strong>{formatCurrency(purchaseSummary.outstandingAmount)}</strong>
            <small>Outstanding purchase balance</small>
          </article>
        </div>
      ) : null}
    </section>
  )
}
