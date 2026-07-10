import { ApiError } from './apiClient'

export type FieldErrors = Record<string, string[]>

export function getErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const validationMessages = Object.values(error.problemDetails?.errors ?? {}).flat()

    if (validationMessages.length > 0) {
      return validationMessages.join(' ')
    }

    return error.message
  }

  if (error instanceof Error) {
    return error.message
  }

  return 'Something went wrong. Please try again.'
}

export function getFieldErrors(error: unknown): FieldErrors {
  if (error instanceof ApiError) {
    return error.problemDetails?.errors ?? {}
  }

  return {}
}

export function getFieldError(errors: FieldErrors, fieldName: string): string | undefined {
  const match = Object.entries(errors).find(
    ([key]) => key.toLowerCase() === fieldName.toLowerCase(),
  )

  return match?.[1]?.join(' ')
}
