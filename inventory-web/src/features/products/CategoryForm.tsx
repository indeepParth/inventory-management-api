import { useEffect, useState, type FormEvent } from 'react'
import { getFieldError, type FieldErrors } from '../../shared/api/apiErrorMessages'
import type { Category, CategoryFormValues } from './productsApi'

type CategoryFormProps = {
  initialValue?: Category
  errors: FieldErrors
  isSubmitting: boolean
  onCancel: () => void
  onSubmit: (values: CategoryFormValues) => Promise<void>
}

export function CategoryForm({
  initialValue,
  errors,
  isSubmitting,
  onCancel,
  onSubmit,
}: CategoryFormProps) {
  const [name, setName] = useState(initialValue?.name ?? '')
  const [description, setDescription] = useState(initialValue?.description ?? '')
  const [isActive, setIsActive] = useState(initialValue?.isActive ?? true)

  useEffect(() => {
    setName(initialValue?.name ?? '')
    setDescription(initialValue?.description ?? '')
    setIsActive(initialValue?.isActive ?? true)
  }, [initialValue])

  async function handleSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    await onSubmit({
      name,
      description,
      isActive,
    })
  }

  return (
    <form className="entity-form" onSubmit={handleSubmit}>
      <label className="form-field">
        <span>Name</span>
        <input
          disabled={isSubmitting}
          maxLength={100}
          onChange={(event) => setName(event.target.value)}
          required
          type="text"
          value={name}
        />
        {getFieldError(errors, 'Name') ? (
          <span className="field-error">{getFieldError(errors, 'Name')}</span>
        ) : null}
      </label>

      <label className="form-field">
        <span>Description</span>
        <textarea
          disabled={isSubmitting}
          maxLength={500}
          onChange={(event) => setDescription(event.target.value)}
          rows={3}
          value={description}
        />
        {getFieldError(errors, 'Description') ? (
          <span className="field-error">{getFieldError(errors, 'Description')}</span>
        ) : null}
      </label>

      {initialValue ? (
        <label className="checkbox-field">
          <input
            checked={isActive}
            disabled={isSubmitting}
            onChange={(event) => setIsActive(event.target.checked)}
            type="checkbox"
          />
          <span>Active</span>
        </label>
      ) : null}

      <div className="form-actions">
        <button className="primary-button" disabled={isSubmitting} type="submit">
          {isSubmitting ? 'Saving...' : 'Save'}
        </button>
        <button className="secondary-button" disabled={isSubmitting} onClick={onCancel} type="button">
          Cancel
        </button>
      </div>
    </form>
  )
}
