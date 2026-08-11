import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../features/auth/AuthContext'
import { hasRouteAccess } from '../features/auth/roleAccess'
import { getCustomers, type Customer } from '../features/parties/partiesApi'
import {
  getCurrentStock,
  getPurchaseRegister,
  getSalesRegister,
  type CurrentStockItem,
  type RegisterSummary,
} from '../features/reports/reportsApi'
import {
  getSalesInvoices,
  type SalesInvoice,
} from '../features/salesInvoices/salesInvoicesApi'
import { getErrorMessage } from '../shared/api/apiErrorMessages'
import { EmptyState, ErrorBanner, LoadingState } from '../shared/components/Feedback'
import { formatCurrency, formatDate } from '../shared/utils/formatters'

const dashboardRowLimit = 5
const pageSize = 100

async function getAllCustomers(): Promise<Customer[]> {
  const customers: Customer[] = []
  let pageNumber = 1
  let hasNextPage = true

  while (hasNextPage) {
    const response = await getCustomers(pageNumber, pageSize, '', '')
    customers.push(...response.items)
    hasNextPage = response.hasNextPage
    pageNumber += 1
  }

  return customers
}

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
  const { currentUser } = useAuth()
  const [stockItems, setStockItems] = useState<CurrentStockItem[]>([])
  const [purchaseSummary, setPurchaseSummary] = useState<RegisterSummary>(emptySummary)
  const [salesSummary, setSalesSummary] = useState<RegisterSummary>(emptySummary)
  const [unpaidCustomers, setUnpaidCustomers] = useState<Customer[]>([])
  const [draftInvoices, setDraftInvoices] = useState<SalesInvoice[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const loadDashboard = useCallback(async (): Promise<void> => {
    setIsLoading(true)
    setErrorMessage(null)

    try {
      const roles = currentUser?.roles ?? []
      const canReadCustomers = hasRouteAccess(roles, 'readCustomers')
      const canManageSalesInvoices = hasRouteAccess(roles, 'manageSalesInvoices')
      const canViewReports = hasRouteAccess(roles, 'viewReports')

      const [reportData, customers, invoices] =
        await Promise.all([
          canViewReports
            ? Promise.all([
                getCurrentStock(1, 100),
                getPurchaseRegister({ pageNumber: 1, pageSize: 5, fromDate: '', toDate: '' }),
                getSalesRegister({ pageNumber: 1, pageSize: 5, fromDate: '', toDate: '' }),
              ])
            : Promise.resolve(null),
          canReadCustomers ? getAllCustomers() : Promise.resolve(null),
          canManageSalesInvoices
            ? getSalesInvoices({
                pageNumber: 1,
                pageSize: dashboardRowLimit,
                customerId: '',
                status: '0',
                invoiceNumber: '',
              })
            : Promise.resolve(null),
        ])

      if (reportData) {
        const [stock, purchases, sales] = reportData
        setStockItems(stock.items)
        setPurchaseSummary(purchases.summary)
        setSalesSummary(sales.summary)
      } else {
        setStockItems([])
        setPurchaseSummary(emptySummary())
        setSalesSummary(emptySummary())
      }

      setUnpaidCustomers(
        customers
          ?.filter((customer) => customer.balanceDue > 0)
          .sort((first, second) => second.balanceDue - first.balanceDue)
          .slice(0, dashboardRowLimit) ?? [],
      )
      setDraftInvoices(invoices?.items ?? [])
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [currentUser?.roles])

  useEffect(() => {
    void loadDashboard()
  }, [loadDashboard])

  const roles = currentUser?.roles ?? []
  const canReadCustomers = hasRouteAccess(roles, 'readCustomers')
  const canManageSalesInvoices = hasRouteAccess(roles, 'manageSalesInvoices')
  const canViewReports = hasRouteAccess(roles, 'viewReports')
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

      {!isLoading && !errorMessage && canViewReports ? (
        <div className="summary-grid dashboard-summary-grid">
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

      {!isLoading && !errorMessage ? (
        <div className="dashboard-tables">
          {canReadCustomers ? (
            <section className="dashboard-table-section" aria-labelledby="unpaid-parties-title">
              <div className="dashboard-table-header">
                <h2 id="unpaid-parties-title">Unpaid parties</h2>
              </div>
              {unpaidCustomers.length === 0 ? <EmptyState>No unpaid parties found.</EmptyState> : null}
              {unpaidCustomers.length > 0 ? (
                <div className="table-wrap">
                  <table className="data-table dashboard-data-table">
                    <thead>
                      <tr>
                        <th>Party</th>
                        <th>Total unpaid</th>
                      </tr>
                    </thead>
                    <tbody>
                      {unpaidCustomers.map((customer) => (
                        <tr key={customer.id}>
                          <td>
                            <Link className="text-link" to={`/app/customers/${customer.id}`}>
                              {customer.name}
                            </Link>
                          </td>
                          <td className="numeric-cell">{formatCurrency(customer.balanceDue)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : null}
            </section>
          ) : null}

          <div className="dashboard-table-grid">
            {canManageSalesInvoices ? (
              <section className="dashboard-table-section" aria-labelledby="unposted-invoices-title">
                <div className="dashboard-table-header">
                  <h2 id="unposted-invoices-title">Unposted invoices</h2>
                </div>
                {draftInvoices.length === 0 ? <EmptyState>No unposted invoices found.</EmptyState> : null}
                {draftInvoices.length > 0 ? (
                  <div className="table-wrap">
                    <table className="data-table dashboard-data-table">
                      <thead>
                        <tr>
                          <th>Party</th>
                          <th>Invoice</th>
                          <th>Date</th>
                        </tr>
                      </thead>
                      <tbody>
                        {draftInvoices.map((invoice) => (
                          <tr key={invoice.id}>
                            <td>
                              <Link className="text-link" to={`/app/customers/${invoice.customerId}`}>
                                {invoice.customerName}
                              </Link>
                            </td>
                            <td>{invoice.invoiceNumber}</td>
                            <td>{formatDate(invoice.invoiceDate)}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ) : null}
              </section>
            ) : null}
          </div>
        </div>
      ) : null}
    </section>
  )
}
