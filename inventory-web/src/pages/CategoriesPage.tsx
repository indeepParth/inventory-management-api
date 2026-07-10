import { useCallback, useEffect, useState } from 'react'
import { useAuth } from '../features/auth/AuthContext'
import { hasRouteAccess } from '../features/auth/roleAccess'
import { CategoryForm } from '../features/products/CategoryForm'
import {
  createCategory,
  deleteCategory,
  getCategories,
  updateCategory,
  type Category,
  type CategoryFormValues,
} from '../features/products/productsApi'
import {
  getErrorMessage,
  getFieldErrors,
  type FieldErrors,
} from '../shared/api/apiErrorMessages'

export function CategoriesPage() {
  const { currentUser } = useAuth()
  const canManageProducts = hasRouteAccess(currentUser?.roles ?? [], 'manageProducts')
  const [categories, setCategories] = useState<Category[]>([])
  const [editingCategory, setEditingCategory] = useState<Category | undefined>()
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const loadCategories = useCallback(async (): Promise<void> => {
    setIsLoading(true)
    setErrorMessage(null)

    try {
      setCategories(await getCategories())
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadCategories()
  }, [loadCategories])

  function closeForm(): void {
    setIsFormOpen(false)
    setEditingCategory(undefined)
    setFieldErrors({})
    setActionError(null)
  }

  async function handleSubmit(values: CategoryFormValues): Promise<void> {
    setIsSaving(true)
    setFieldErrors({})
    setActionError(null)

    try {
      if (editingCategory) {
        await updateCategory(editingCategory.id, values)
      } else {
        await createCategory({
          name: values.name,
          description: values.description,
        })
      }

      closeForm()
      await loadCategories()
    } catch (error) {
      setFieldErrors(getFieldErrors(error))
      setActionError(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleDelete(category: Category): Promise<void> {
    const confirmed = window.confirm(`Delete category "${category.name}"?`)

    if (!confirmed) {
      return
    }

    setActionError(null)

    try {
      await deleteCategory(category.id)
      await loadCategories()
    } catch (error) {
      setActionError(getErrorMessage(error))
    }
  }

  return (
    <section className="content-panel wide-panel" aria-labelledby="categories-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">Master data</p>
          <h1 id="categories-title" className="page-title">
            Categories
          </h1>
        </div>
        {canManageProducts ? (
          <button
            className="primary-button"
            onClick={() => {
              setIsFormOpen(true)
              setEditingCategory(undefined)
              setFieldErrors({})
              setActionError(null)
            }}
            type="button"
          >
            New category
          </button>
        ) : null}
      </div>

      {actionError ? (
        <p className="form-error" role="alert">
          {actionError}
        </p>
      ) : null}

      {isFormOpen ? (
        <CategoryForm
          errors={fieldErrors}
          initialValue={editingCategory}
          isSubmitting={isSaving}
          onCancel={closeForm}
          onSubmit={handleSubmit}
        />
      ) : null}

      {isLoading ? <p className="state-message">Loading categories...</p> : null}
      {errorMessage ? (
        <p className="form-error" role="alert">
          {errorMessage}
        </p>
      ) : null}
      {!isLoading && !errorMessage && categories.length === 0 ? (
        <p className="state-message">No categories found.</p>
      ) : null}

      {!isLoading && !errorMessage && categories.length > 0 ? (
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Description</th>
                <th>Status</th>
                {canManageProducts ? <th>Actions</th> : null}
              </tr>
            </thead>
            <tbody>
              {categories.map((category) => (
                <tr key={category.id}>
                  <td>{category.name}</td>
                  <td>{category.description || '-'}</td>
                  <td>{category.isActive ? 'Active' : 'Inactive'}</td>
                  {canManageProducts ? (
                    <td>
                      <div className="table-actions">
                        <button
                          className="text-button"
                          onClick={() => {
                            setEditingCategory(category)
                            setIsFormOpen(true)
                            setFieldErrors({})
                            setActionError(null)
                          }}
                          type="button"
                        >
                          Edit
                        </button>
                        <button
                          className="danger-button"
                          onClick={() => void handleDelete(category)}
                          type="button"
                        >
                          Delete
                        </button>
                      </div>
                    </td>
                  ) : null}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </section>
  )
}
