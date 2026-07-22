import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { getErrorMessage } from '../../shared/api/apiErrorMessages'
import { EmptyState, ErrorBanner, LoadingState } from '../../shared/components/Feedback'
import { formatCurrency, formatDate, toDateInputValue } from '../../shared/utils/formatters'
import {
  buildPartyLedgerFileName,
  downloadPartyLedgerPdf,
  type PartyLedgerPdfDetail,
} from './partyLedgerPdf'
import type { StatementEntry, StatementResponse } from './partiesApi'

const pageSize = 20

type PartyLedgerViewProps = {
  backLabel: string
  backTo: string
  loadStatement: (filters: {
    dateFrom: string
    dateTo: string
    pageNumber: number
    pageSize: number
  }) => Promise<StatementResponse>
  partyDetails?: PartyLedgerPdfDetail[]
  partyName: string
  title: string
}

function getCurrentYearStart(): string {
  return `${new Date().getFullYear()}-01-01`
}

function getDefaultDescription(entry: StatementEntry): string {
  if (entry.type === 0) {
    return 'Sales invoice'
  }

  if (entry.type === 1) {
    return 'Purchase'
  }

  if (entry.type === 2) {
    return 'Payment'
  }

  return 'Payment reversal'
}

function getStatementTypeLabel(entry: StatementEntry): string {
  if (entry.type === 0) {
    return 'Invoice'
  }

  if (entry.type === 1) {
    return 'Purchase'
  }

  if (entry.type === 2) {
    return 'Payment'
  }

  return 'Reversal'
}

function getDebitAmount(entry: StatementEntry): number | null {
  return entry.balanceChange > 0 ? entry.balanceChange : null
}

function getCreditAmount(entry: StatementEntry): number | null {
  return entry.balanceChange < 0 ? Math.abs(entry.balanceChange) : null
}

export function PartyLedgerView({
  backLabel,
  backTo,
  loadStatement,
  partyDetails = [],
  partyName,
  title,
}: PartyLedgerViewProps) {
  const defaultFromDate = getCurrentYearStart()
  const defaultToDate = toDateInputValue()
  const [statement, setStatement] = useState<StatementResponse | null>(null)
  const [fromDateInput, setFromDateInput] = useState(defaultFromDate)
  const [toDateInput, setToDateInput] = useState(defaultToDate)
  const [fromDate, setFromDate] = useState(defaultFromDate)
  const [toDate, setToDate] = useState(defaultToDate)
  const [pageNumber, setPageNumber] = useState(1)
  const [isLoading, setIsLoading] = useState(true)
  const [isDownloadingPdf, setIsDownloadingPdf] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const loadLedger = useCallback(async (): Promise<void> => {
    setIsLoading(true)
    setErrorMessage(null)
    setStatement(null)

    try {
      setStatement(await loadStatement({ dateFrom: fromDate, dateTo: toDate, pageNumber, pageSize }))
    } catch (error) {
      setStatement(null)
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [fromDate, loadStatement, pageNumber, toDate])

  useEffect(() => {
    void loadLedger()
  }, [loadLedger])

  function handleFilters(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault()

    if (!fromDateInput || !toDateInput) {
      setStatement(null)
      setErrorMessage('Select both from date and to date.')
      return
    }

    if (toDateInput < fromDateInput) {
      setStatement(null)
      setErrorMessage('To date must be on or after from date.')
      return
    }

    setErrorMessage(null)
    setPageNumber(1)
    setFromDate(fromDateInput)
    setToDate(toDateInput)
  }

  function clearFilters(): void {
    const nextFromDate = getCurrentYearStart()
    const nextToDate = toDateInputValue()
    setPageNumber(1)
    setFromDateInput(nextFromDate)
    setToDateInput(nextToDate)
    setFromDate(nextFromDate)
    setToDate(nextToDate)
    setErrorMessage(null)
  }

  async function handleDownloadPdf(): Promise<void> {
    if (!statement) {
      return
    }

    setIsDownloadingPdf(true)
    setErrorMessage(null)

    try {
      await downloadPartyLedgerPdf({
        title,
        partyName,
        partyDetails,
        statement,
        totalDebit,
        totalCredit,
        fileName: buildPartyLedgerFileName(title, partyName, fromDate, toDate),
      })
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsDownloadingPdf(false)
    }
  }

  const entries = statement?.entries ?? []
  const totalDebit = statement ? statement.totalCharges + statement.totalReversals : 0
  const totalCredit = statement?.totalPayments ?? 0

  return (
    <section className="content-panel wide-panel" aria-labelledby="party-ledger-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">{title}</p>
          <h1 id="party-ledger-title" className="page-title">{partyName}</h1>
        </div>
        <button className="secondary-button" disabled={!statement || isLoading || isDownloadingPdf} onClick={() => void handleDownloadPdf()} type="button">
          {isDownloadingPdf ? 'Preparing PDF...' : 'Download PDF'}
        </button>
      </div>

      <form className="toolbar" onSubmit={handleFilters}>
        <input aria-label="From date" onChange={(event) => setFromDateInput(event.target.value)} type="date" value={fromDateInput} />
        <input aria-label="To date" onChange={(event) => setToDateInput(event.target.value)} type="date" value={toDateInput} />
        <button className="secondary-button" type="submit">Apply dates</button>
        <button className="text-button" onClick={clearFilters} type="button">Clear</button>
      </form>

      {isLoading ? <LoadingState>Loading ledger...</LoadingState> : null}
      {errorMessage ? <ErrorBanner>{errorMessage}</ErrorBanner> : null}

      {statement ? (
        <>
          <div className="summary-grid">
            <article className="summary-card">
              <span>Opening balance</span>
              <strong>{formatCurrency(statement.openingBalance)}</strong>
            </article>
            <article className="summary-card">
              <span>Total debit / charges</span>
              <strong>{formatCurrency(totalDebit)}</strong>
            </article>
            <article className="summary-card">
              <span>Total credit / payments</span>
              <strong>{formatCurrency(totalCredit)}</strong>
            </article>
            <article className="summary-card">
              <span>Closing balance</span>
              <strong>{formatCurrency(statement.closingBalance)}</strong>
            </article>
          </div>

          <h2>Entries</h2>
          {entries.length === 0 ? <EmptyState>No ledger entries found for this date range.</EmptyState> : null}
          {entries.length > 0 ? (
            <>
              <div className="table-wrap">
                <table className="data-table ledger-table">
                  <thead>
                    <tr>
                      <th>Date</th>
                      <th>Type</th>
                      <th>Document / Reference No.</th>
                      <th>Description</th>
                      <th>Debit</th>
                      <th>Credit</th>
                      <th>Running balance</th>
                    </tr>
                  </thead>
                  <tbody>
                    {entries.map((entry) => {
                      const debit = getDebitAmount(entry)
                      const credit = getCreditAmount(entry)

                      return (
                        <tr key={`${entry.type}-${entry.transactionId}-${entry.timestampUtc}`}>
                          <td>{formatDate(entry.transactionDate)}</td>
                          <td>{getStatementTypeLabel(entry)}</td>
                          <td className="ledger-reference-cell">
                            <strong>{entry.referenceNumber}</strong>
                            {entry.externalReference ? (
                              <>
                                <br />
                                <span>{entry.externalReference}</span>
                              </>
                            ) : null}
                          </td>
                          <td className="ledger-description-cell">{entry.note || getDefaultDescription(entry)}</td>
                          <td className="numeric-cell">{debit === null ? '-' : formatCurrency(debit)}</td>
                          <td className="numeric-cell">{credit === null ? '-' : formatCurrency(credit)}</td>
                          <td className="numeric-cell">{formatCurrency(entry.runningBalance)}</td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>
              <div className="pagination">
                <button className="secondary-button" disabled={!statement.hasPreviousPage} onClick={() => setPageNumber((current) => Math.max(1, current - 1))} type="button">Previous</button>
                <span>Page {statement.pageNumber} of {statement.totalPages}</span>
                <button className="secondary-button" disabled={!statement.hasNextPage} onClick={() => setPageNumber((current) => current + 1)} type="button">Next</button>
              </div>
            </>
          ) : null}
        </>
      ) : null}

      <p className="page-action"><Link className="text-link" to={backTo}>{backLabel}</Link></p>
    </section>
  )
}
