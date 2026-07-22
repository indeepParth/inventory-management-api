const defaultAppName = 'StockFlow'

export function getAppName(): string {
  return import.meta.env.VITE_APP_NAME?.trim() || defaultAppName
}
