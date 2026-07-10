import { useCallback, useEffect, useState, type FormEvent } from 'react'
import {
  getCurrentStock,
  getGrossProfit,
  getPurchaseRegister,
  getSalesRegister,
  type CurrentStockItem,
  type GrossProfitReport,
  type PagedResponse,
  type PurchaseRegisterItem,
  type RegisterResponse,
  type SalesRegisterItem,
} from '../features/reports/reportsApi'
import { getErrorMessage } from '../shared/api/apiErrorMessages'
import { EmptyState, ErrorBanner, LoadingState } from '../shared/components/Feedback'
import { formatCurrency, formatDate, formatQuantity } from '../shared/utils/formatters'

const pageSize = 10

type ReportTab = 'stock' | 'purchases' | 'sales' | 'grossProfit'

export function ReportsPage() {
  const [tab, setTab] = useState<ReportTab>('stock')
  const [stock, setStock] = useState<PagedResponse<CurrentStockItem> | null>(null)
  const [purchases, setPurchases] = useState<RegisterResponse<PurchaseRegisterItem> | null>(null)
  const [sales, setSales] = useState<RegisterResponse<SalesRegisterItem> | null>(null)
  const [grossProfit, setGrossProfit] = useState<GrossProfitReport | null>(null)
  const [pageNumber, setPageNumber] = useState(1)
  const [fromDateInput, setFromDateInput] = useState('')
  const [toDateInput, setToDateInput] = useState('')
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const loadReport = useCallback(async (): Promise<void> => {
    setIsLoading(true)
    setErrorMessage(null)

    try {
      if (tab === 'stock') {
        setStock(await getCurrentStock(pageNumber, pageSize))
      } else if (tab === 'purchases') {
        setPurchases(await getPurchaseRegister({ pageNumber, pageSize, fromDate, toDate }))
      } else if (tab === 'sales') {
        setSales(await getSalesRegister({ pageNumber, pageSize, fromDate, toDate }))
      } else {
        setGrossProfit(await getGrossProfit(fromDate, toDate))
      }
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [fromDate, pageNumber, tab, toDate])

  useEffect(() => {
    void loadReport()
  }, [loadReport])

  function handleDateFilter(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault()
    setPageNumber(1)
    setFromDate(fromDateInput)
    setToDate(toDateInput)
  }

  function switchTab(nextTab: ReportTab): void {
    setTab(nextTab)
    setPageNumber(1)
    setErrorMessage(null)
  }

  const activePagedResponse =
    tab === 'stock' ? stock : tab === 'purchases' ? purchases : tab === 'sales' ? sales : null

  return (
    <section className="content-panel wide-panel" aria-labelledby="reports-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">Reports</p>
          <h1 id="reports-title" className="page-title">Read-only reports</h1>
        </div>
      </div>

      <div className="tab-list" role="tablist" aria-label="Report sections">
        <button className={tab === 'stock' ? 'primary-button' : 'secondary-button'} onClick={() => switchTab('stock')} type="button">Current stock</button>
        <button className={tab === 'purchases' ? 'primary-button' : 'secondary-button'} onClick={() => switchTab('purchases')} type="button">Purchase register</button>
        <button className={tab === 'sales' ? 'primary-button' : 'secondary-button'} onClick={() => switchTab('sales')} type="button">Sales register</button>
        <button className={tab === 'grossProfit' ? 'primary-button' : 'secondary-button'} onClick={() => switchTab('grossProfit')} type="button">Gross profit</button>
      </div>

      {tab !== 'stock' ? (
        <form className="toolbar" onSubmit={handleDateFilter}>
          <input aria-label="From date" onChange={(event) => setFromDateInput(event.target.value)} type="date" value={fromDateInput} />
          <input aria-label="To date" onChange={(event) => setToDateInput(event.target.value)} type="date" value={toDateInput} />
          <button className="secondary-button" type="submit">Apply dates</button>
        </form>
      ) : null}

      {isLoading ? <LoadingState>Loading report...</LoadingState> : null}
      {errorMessage ? <ErrorBanner>{errorMessage}</ErrorBanner> : null}

      {!isLoading && !errorMessage && tab === 'stock' && stock ? (
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>Product</th>
                <th>Category</th>
                <th>Quantity</th>
                <th>Average cost</th>
                <th>Stock value</th>
              </tr>
            </thead>
            <tbody>
              {stock.items.map((item) => (
                <tr key={item.productId}>
                  <td>{item.productName}</td>
                  <td>{item.category}</td>
                  <td>{formatQuantity(item.quantity)}</td>
                  <td>{formatCurrency(item.averageCost)}</td>
                  <td>{formatCurrency(item.stockValue)}</td>
                </tr>
              ))}
            {stock.items.length === 0 ? (
              <tr><td colSpan={5}><EmptyState>No current stock rows found.</EmptyState></td></tr>
            ) : null}
            </tbody>
          </table>
        </div>
      ) : null}

      {!isLoading && !errorMessage && tab === 'purchases' && purchases ? (
        <>
          <div className="summary-strip">
            <span>Documents: {purchases.summary.documentCount}</span>
            <strong>Total: {formatCurrency(purchases.summary.grandTotal)}</strong>
            <span>Outstanding: {formatCurrency(purchases.summary.outstandingAmount)}</span>
          </div>
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Purchase</th>
                  <th>Supplier</th>
                  <th>Date</th>
                  <th>Quantity</th>
                  <th>Total</th>
                  <th>Outstanding</th>
                </tr>
              </thead>
              <tbody>
                {purchases.items.map((item) => (
                  <tr key={item.purchaseId}>
                    <td>{item.purchaseNumber}</td>
                    <td>{item.supplierName}</td>
                    <td>{formatDate(item.date)}</td>
                    <td>{formatQuantity(item.totalQuantity)}</td>
                    <td>{formatCurrency(item.grandTotal)}</td>
                    <td>{formatCurrency(item.outstandingAmount)}</td>
                  </tr>
                ))}
                {purchases.items.length === 0 ? (
                  <tr><td colSpan={6}><EmptyState>No purchase register rows found.</EmptyState></td></tr>
                ) : null}
              </tbody>
            </table>
          </div>
        </>
      ) : null}

      {!isLoading && !errorMessage && tab === 'sales' && sales ? (
        <>
          <div className="summary-strip">
            <span>Invoices: {sales.summary.documentCount}</span>
            <strong>Total: {formatCurrency(sales.summary.grandTotal)}</strong>
            <span>Receivable: {formatCurrency(sales.summary.outstandingAmount)}</span>
          </div>
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Invoice</th>
                  <th>Customer</th>
                  <th>Date</th>
                  <th>Quantity</th>
                  <th>Total</th>
                  <th>Outstanding</th>
                </tr>
              </thead>
              <tbody>
                {sales.items.map((item) => (
                  <tr key={item.salesInvoiceId}>
                    <td>{item.invoiceNumber}</td>
                    <td>{item.customerName}</td>
                    <td>{formatDate(item.date)}</td>
                    <td>{formatQuantity(item.totalQuantity)}</td>
                    <td>{formatCurrency(item.grandTotal)}</td>
                    <td>{formatCurrency(item.outstandingAmount)}</td>
                  </tr>
                ))}
                {sales.items.length === 0 ? (
                  <tr><td colSpan={6}><EmptyState>No sales register rows found.</EmptyState></td></tr>
                ) : null}
              </tbody>
            </table>
          </div>
        </>
      ) : null}

      {!isLoading && !errorMessage && tab === 'grossProfit' && grossProfit ? (
        <>
          <div className="summary-grid">
            <article className="summary-card">
              <span>Net revenue</span>
              <strong>{formatCurrency(grossProfit.summary.netRevenue)}</strong>
            </article>
            <article className="summary-card">
              <span>Net COGS</span>
              <strong>{formatCurrency(grossProfit.summary.netCostOfGoodsSold)}</strong>
            </article>
            <article className="summary-card">
              <span>Gross profit</span>
              <strong>{formatCurrency(grossProfit.summary.grossProfit)}</strong>
            </article>
            <article className="summary-card">
              <span>Gross margin</span>
              <strong>{grossProfit.summary.grossMarginPercentage.toFixed(2)}%</strong>
            </article>
          </div>
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Product</th>
                  <th>Net quantity</th>
                  <th>Net revenue</th>
                  <th>Gross profit</th>
                  <th>Margin</th>
                </tr>
              </thead>
              <tbody>
                {grossProfit.byProduct.slice(0, 10).map((item) => (
                  <tr key={item.productId}>
                    <td>{item.productName}</td>
                    <td>{formatQuantity(item.netQuantity)}</td>
                    <td>{formatCurrency(item.netRevenue)}</td>
                    <td>{formatCurrency(item.grossProfit)}</td>
                    <td>{item.grossMarginPercentage.toFixed(2)}%</td>
                  </tr>
                ))}
                {grossProfit.byProduct.length === 0 ? (
                  <tr><td colSpan={5}><EmptyState>No gross profit rows found.</EmptyState></td></tr>
                ) : null}
              </tbody>
            </table>
          </div>
        </>
      ) : null}

      {activePagedResponse ? (
        <div className="pagination">
          <button className="secondary-button" disabled={!activePagedResponse.hasPreviousPage} onClick={() => setPageNumber((current) => Math.max(1, current - 1))} type="button">Previous</button>
          <span>Page {activePagedResponse.pageNumber} of {activePagedResponse.totalPages}</span>
          <button className="secondary-button" disabled={!activePagedResponse.hasNextPage} onClick={() => setPageNumber((current) => current + 1)} type="button">Next</button>
        </div>
      ) : null}
    </section>
  )
}
