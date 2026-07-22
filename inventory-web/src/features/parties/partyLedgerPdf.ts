import { formatCurrency, formatDate } from '../../shared/utils/formatters'
import { getAppName } from '../../shared/config/appConfig'
import type { StatementEntry, StatementResponse } from './partiesApi'

export type PartyLedgerPdfDetail = {
  label: string
  value: string
}

export type PartyLedgerPdfValues = {
  title: string
  partyName: string
  partyDetails: PartyLedgerPdfDetail[]
  statement: StatementResponse
  totalDebit: number
  totalCredit: number
  fileName: string
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

function getReferenceText(entry: StatementEntry): string {
  return entry.externalReference
    ? `${entry.referenceNumber}\n${entry.externalReference}`
    : entry.referenceNumber
}

function formatPdfCurrency(value: number | null): string {
  return value === null ? '-' : formatCurrency(value)
}

function buildSafeFileName(value: string): string {
  return value
    .trim()
    .replace(/[^a-z0-9]+/gi, '-')
    .replace(/^-+|-+$/g, '')
    .toLowerCase()
}

export function buildPartyLedgerFileName(
  title: string,
  partyName: string,
  dateFrom: string,
  dateTo: string,
): string {
  const name = buildSafeFileName(`${title}-${partyName}-${dateFrom}-to-${dateTo}`)
  return `${name || 'party-ledger'}.pdf`
}

export async function downloadPartyLedgerPdf({
  title,
  partyName,
  partyDetails,
  statement,
  totalDebit,
  totalCredit,
  fileName,
}: PartyLedgerPdfValues): Promise<void> {
  const [{ default: pdfMake }, { default: pdfFonts }] = await Promise.all([
    import('pdfmake/build/pdfmake'),
    import('pdfmake/build/vfs_fonts'),
  ])

  pdfMake.addVirtualFileSystem(pdfFonts)

  const details = partyDetails
    .filter((detail) => detail.value)
    .map((detail) => `${detail.label}: ${detail.value}`)
  const appName = getAppName()

  const body = [
    [
      'Date',
      'Type',
      'Document / Reference No.',
      'Description',
      'Debit',
      'Credit',
      'Running Balance',
    ],
    ...statement.entries.map((entry) => [
      formatDate(entry.transactionDate),
      getStatementTypeLabel(entry),
      getReferenceText(entry),
      entry.note || getDefaultDescription(entry),
      formatPdfCurrency(getDebitAmount(entry)),
      formatPdfCurrency(getCreditAmount(entry)),
      formatCurrency(entry.runningBalance),
    ]),
  ]

  if (statement.entries.length === 0) {
    body.push(['-', '-', '-', 'No ledger entries found for this date range.', '-', '-', '-'])
  }

  pdfMake.createPdf({
    pageSize: 'A4',
    pageMargins: [28, 32, 28, 32],
    info: {
      title: `${title} - ${partyName}`,
      subject: title,
    },
    content: [
      { text: appName, style: 'appHeading' },
      { text: title, style: 'title' },
      {
        columns: [
          [
            { text: partyName, style: 'partyName' },
            ...(details.length > 0 ? [{ text: details.join('\n'), style: 'muted' }] : []),
          ],
          [
            { text: `Date range: ${formatDate(statement.dateFrom)} to ${formatDate(statement.dateTo)}`, alignment: 'right' },
            { text: `Generated: ${formatDate(new Date().toISOString())}`, alignment: 'right', style: 'muted' },
          ],
        ],
        columnGap: 16,
        margin: [0, 0, 0, 14],
      },
      {
        table: {
          widths: ['*', '*', '*', '*'],
          body: [
            ['Opening Balance', 'Total Debit / Charges', 'Total Credit / Payments', 'Closing Balance'],
            [
              formatCurrency(statement.openingBalance),
              formatCurrency(totalDebit),
              formatCurrency(totalCredit),
              formatCurrency(statement.closingBalance),
            ],
          ],
        },
        layout: 'lightHorizontalLines',
        margin: [0, 0, 0, 16],
      },
      {
        table: {
          headerRows: 1,
          widths: [48, 52, 82, '*', 54, 54, 62],
          body,
        },
        layout: 'lightHorizontalLines',
      },
    ],
    defaultStyle: {
      fontSize: 8,
    },
    styles: {
      appHeading: {
        bold: true,
        color: '#2563eb',
        fontSize: 12,
        margin: [0, 0, 0, 4],
      },
      title: {
        bold: true,
        fontSize: 18,
        margin: [0, 0, 0, 12],
      },
      partyName: {
        bold: true,
        fontSize: 11,
        margin: [0, 0, 0, 4],
      },
      muted: {
        color: '#64748b',
        fontSize: 8,
      },
    },
  }).download(fileName)
}
