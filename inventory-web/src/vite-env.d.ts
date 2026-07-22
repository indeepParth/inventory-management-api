/// <reference types="vite/client" />

declare module 'pdfmake/build/pdfmake' {
  type PdfContent = Record<string, unknown> | string

  type PdfDocumentDefinition = {
    content: PdfContent[]
    defaultStyle?: Record<string, unknown>
    info?: Record<string, unknown>
    pageMargins?: number[]
    pageSize?: string
    styles?: Record<string, Record<string, unknown>>
  }

  type PdfMake = {
    addVirtualFileSystem: (fonts: Record<string, unknown>) => void
    createPdf: (definition: PdfDocumentDefinition) => {
      download: (fileName?: string) => void
    }
  }

  const pdfMake: PdfMake
  export default pdfMake
}

declare module 'pdfmake/build/vfs_fonts' {
  const pdfFonts: Record<string, unknown>
  export default pdfFonts
}

interface ImportMetaEnv {
  readonly VITE_APP_NAME?: string
  readonly VITE_API_BASE_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
