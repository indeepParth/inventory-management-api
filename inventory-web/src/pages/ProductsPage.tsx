import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../features/auth/AuthContext'
import { hasRouteAccess } from '../features/auth/roleAccess'
import { ProductForm } from '../features/products/ProductForm'
import {
  createProduct,
  deleteProduct,
  getCategories,
  getProducts,
  updateProduct,
  type Category,
  type PagedResponse,
  type Product,
  type ProductFormValues,
} from '../features/products/productsApi'
import {
  getErrorMessage,
  getFieldErrors,
  type FieldErrors,
} from '../shared/api/apiErrorMessages'
import { EmptyState, ErrorBanner, LoadingState } from '../shared/components/Feedback'
import { formatCurrency, formatQuantity } from '../shared/utils/formatters'

const pageSize = 10

export function ProductsPage() {
  const { currentUser } = useAuth()
  const canManageProducts = hasRouteAccess(currentUser?.roles ?? [], 'manageProducts')
  const [productsResponse, setProductsResponse] = useState<PagedResponse<Product> | null>(null)
  const [categories, setCategories] = useState<Category[]>([])
  const [editingProduct, setEditingProduct] = useState<Product | undefined>()
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [pageNumber, setPageNumber] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const loadProducts = useCallback(async (): Promise<void> => {
    setIsLoading(true)
    setErrorMessage(null)

    try {
      const [productPage, categoryItems] = await Promise.all([
        getProducts(pageNumber, pageSize, search),
        getCategories(),
      ])
      setProductsResponse(productPage)
      setCategories(categoryItems)
    } catch (error) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [pageNumber, search])

  useEffect(() => {
    void loadProducts()
  }, [loadProducts])

  function closeForm(): void {
    setIsFormOpen(false)
    setEditingProduct(undefined)
    setFieldErrors({})
    setActionError(null)
  }

  function handleSearch(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault()
    setPageNumber(1)
    setSearch(searchInput.trim())
  }

  async function handleSubmit(values: ProductFormValues): Promise<void> {
    setIsSaving(true)
    setFieldErrors({})
    setActionError(null)

    try {
      if (editingProduct) {
        await updateProduct(editingProduct.id, values)
      } else {
        await createProduct(values)
      }

      closeForm()
      await loadProducts()
    } catch (error) {
      setFieldErrors(getFieldErrors(error))
      setActionError(getErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleDelete(product: Product): Promise<void> {
    const confirmed = window.confirm(`Delete product "${product.name}"?`)

    if (!confirmed) {
      return
    }

    setActionError(null)

    try {
      await deleteProduct(product.id)
      await loadProducts()
    } catch (error) {
      setActionError(getErrorMessage(error))
    }
  }

  const products = productsResponse?.items ?? []

  return (
    <section className="content-panel wide-panel" aria-labelledby="products-title">
      <div className="page-header">
        <div>
          <p className="page-kicker">Inventory</p>
          <h1 id="products-title" className="page-title">
            Products
          </h1>
        </div>
        {canManageProducts ? (
          <button
            className="primary-button"
            disabled={categories.length === 0}
            onClick={() => {
              setIsFormOpen(true)
              setEditingProduct(undefined)
              setFieldErrors({})
              setActionError(null)
            }}
            type="button"
          >
            New product
          </button>
        ) : null}
      </div>

      <form className="toolbar" onSubmit={handleSearch}>
        <input
          aria-label="Search products"
          onChange={(event) => setSearchInput(event.target.value)}
          placeholder="Search products"
          type="search"
          value={searchInput}
        />
        <button className="secondary-button" type="submit">
          Search
        </button>
        <Link className="text-link" to="/app/categories">
          Manage categories
        </Link>
      </form>

      {categories.length === 0 && canManageProducts ? (
        <p className="state-message">
          Create a category before adding products.
        </p>
      ) : null}

      {actionError ? (
        <ErrorBanner>{actionError}</ErrorBanner>
      ) : null}

      {isFormOpen ? (
        <ProductForm
          categories={categories}
          errors={fieldErrors}
          initialValue={editingProduct}
          isSubmitting={isSaving}
          onCancel={closeForm}
          onSubmit={handleSubmit}
        />
      ) : null}

      {isLoading ? <LoadingState>Loading products...</LoadingState> : null}
      {errorMessage ? (
        <ErrorBanner>{errorMessage}</ErrorBanner>
      ) : null}
      {!isLoading && !errorMessage && products.length === 0 ? (
        <EmptyState>No products found.</EmptyState>
      ) : null}

      {!isLoading && !errorMessage && products.length > 0 ? (
        <>
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>SKU</th>
                  <th>Category</th>
                  <th>Stock</th>
                  <th>Price</th>
                  {canManageProducts ? <th>Actions</th> : null}
                </tr>
              </thead>
              <tbody>
                {products.map((product) => (
                  <tr key={product.id}>
                    <td>{product.name}</td>
                    <td>{product.sku}</td>
                    <td>{product.categoryName}</td>
                    <td>
                      {formatQuantity(product.quantity)} {product.baseUnit}
                    </td>
                    <td>{formatCurrency(product.defaultSellingPrice)}</td>
                    {canManageProducts ? (
                      <td>
                        <div className="table-actions">
                          <button
                            className="text-button"
                            onClick={() => {
                              setEditingProduct(product)
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
                            onClick={() => void handleDelete(product)}
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

          <div className="pagination">
            <button
              className="secondary-button"
              disabled={!productsResponse?.hasPreviousPage}
              onClick={() => setPageNumber((current) => Math.max(1, current - 1))}
              type="button"
            >
              Previous
            </button>
            <span>
              Page {productsResponse?.pageNumber ?? 1} of {productsResponse?.totalPages ?? 1}
            </span>
            <button
              className="secondary-button"
              disabled={!productsResponse?.hasNextPage}
              onClick={() => setPageNumber((current) => current + 1)}
              type="button"
            >
              Next
            </button>
          </div>
        </>
      ) : null}
    </section>
  )
}
