type FeedbackProps = {
  children: string
}

export function ErrorBanner({ children }: FeedbackProps) {
  return (
    <p className="feedback-banner error" role="alert">
      {children}
    </p>
  )
}

export function EmptyState({ children }: FeedbackProps) {
  return <p className="feedback-banner muted">{children}</p>
}

export function LoadingState({ children }: FeedbackProps) {
  return (
    <p className="feedback-banner loading" aria-live="polite">
      {children}
    </p>
  )
}
