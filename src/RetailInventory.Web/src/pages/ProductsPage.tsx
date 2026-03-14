import { useState, useEffect, useRef, Fragment } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { getProducts, createProduct, updateProduct, restockProduct, deleteProduct } from '../api/products'
import { placeOrder } from '../api/orders'
import Pagination from '../components/Pagination'
import type { PagedResult, Product } from '../types'

interface ProductForm {
  name: string
  sku: string
  imageUrl: string
  price: string
  stockQuantity: string
}

const empty: ProductForm = { name: '', sku: '', imageUrl: '', price: '', stockQuantity: '' }

function productToForm(p: Product): ProductForm {
  return {
    name: p.name,
    sku: p.sku,
    imageUrl: p.imageUrl ?? '',
    price: String(p.price),
    stockQuantity: String(p.stockQuantity),
  }
}

export default function ProductsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [data, setData] = useState<PagedResult<Product> | null>(null)
  const page = parseInt(searchParams.get('page') ?? '1', 10)
  const setPage = (p: number) => setSearchParams(prev => { prev.set('page', String(p)); return prev }, { replace: true })
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState<ProductForm>(empty)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const isAdmin = localStorage.getItem('role') === 'Admin'
  const customerId = localStorage.getItem('customerId') ?? ''

  const [search, setSearch] = useState('')
  const searchTimer = useRef<ReturnType<typeof setTimeout> | null>(null)

  const [editingId, setEditingId] = useState<string | null>(null)
  const [editForm, setEditForm] = useState<ProductForm>(empty)
  const [restockingId, setRestockingId] = useState<string | null>(null)
  const [restockQty, setRestockQty] = useState(1)
  const [orderingId, setOrderingId] = useState<string | null>(null)
  const [orderQty, setOrderQty] = useState(1)

  const set = (field: keyof ProductForm) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm(f => ({ ...f, [field]: e.target.value }))

  const setEdit = (field: keyof ProductForm) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setEditForm(f => ({ ...f, [field]: e.target.value }))

  async function load(p = page, s = search) {
    try {
      const result = await getProducts(p, 10, 'name', 'asc', s)
      setData(result)
    } catch (err) {
      setError((err as Error).message)
    }
  }

  useEffect(() => { load() }, [page])

  function handleSearch(value: string) {
    setSearch(value)
    setPage(1)
    if (searchTimer.current) clearTimeout(searchTimer.current)
    searchTimer.current = setTimeout(() => load(1, value), 300)
  }

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setSuccess('')
    try {
      await createProduct({
        name: form.name,
        sku: form.sku,
        imageUrl: form.imageUrl || undefined,
        price: parseFloat(form.price),
        stockQuantity: parseInt(form.stockQuantity),
      })
      setSuccess('Product created.')
      setForm(empty)
      setShowForm(false)
      load()
    } catch (err) {
      setError((err as Error).message)
    }
  }

  function startEdit(p: Product) {
    setEditingId(p.id)
    setEditForm(productToForm(p))
    setRestockingId(null)
  }

  function cancelEdit() {
    setEditingId(null)
    setEditForm(empty)
  }

  async function handleEdit(e: React.FormEvent) {
    e.preventDefault()
    if (!editingId) return
    setError('')
    setSuccess('')
    try {
      await updateProduct(editingId, {
        name: editForm.name,
        sku: editForm.sku,
        imageUrl: editForm.imageUrl || undefined,
        price: parseFloat(editForm.price),
        stockQuantity: parseInt(editForm.stockQuantity),
      })
      setSuccess('Product updated.')
      cancelEdit()
      load()
    } catch (err) {
      setError((err as Error).message)
    }
  }

  function startRestock(id: string) {
    setRestockingId(id)
    setRestockQty(1)
    setEditingId(null)
  }

  function cancelRestock() {
    setRestockingId(null)
    setRestockQty(1)
  }

  async function handleRestock(e: React.FormEvent, id: string) {
    e.preventDefault()
    setError('')
    setSuccess('')
    try {
      await restockProduct(id, restockQty)
      setSuccess('Product restocked.')
      cancelRestock()
      load()
    } catch (err) {
      setError((err as Error).message)
    }
  }

  async function handleOrder(e: React.FormEvent, productId: string) {
    e.preventDefault()
    setError('')
    setSuccess('')
    try {
      await placeOrder(customerId, [{ productId, quantity: orderQty }])
      setSuccess('Order placed!')
      setOrderingId(null)
      setOrderQty(1)
    } catch (err) {
      setError((err as Error).message)
    }
  }

  async function handleDelete(id: string) {
    if (!window.confirm('Delete this product?')) return
    setError('')
    setSuccess('')
    try {
      await deleteProduct(id)
      setSuccess('Product deleted.')
      load()
    } catch (err) {
      setError((err as Error).message)
    }
  }

  return (
    <>
      <div className="page-header">
        <h1>Products</h1>
        <div className="search-wrap">
          <span className="search-icon">⌕</span>
          <input
            type="search"
            placeholder="Search products…"
            value={search}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleSearch(e.target.value)}
          />
        </div>
        {isAdmin && (
          <button className="btn btn-primary" onClick={() => { setShowForm(s => !s); cancelEdit(); cancelRestock() }}>
            {showForm ? 'Cancel' : '+ New Product'}
          </button>
        )}
      </div>

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      {showForm && (
        <div className="card">
          <h2>New Product</h2>
          <form onSubmit={handleCreate}>
            <div className="form-row">
              <div className="form-group">
                <label>Name</label>
                <input value={form.name} onChange={set('name')} required />
              </div>
              <div className="form-group">
                <label>SKU</label>
                <input value={form.sku} onChange={set('sku')} required />
              </div>
              <div className="form-group">
                <label>Price</label>
                <input type="number" step="0.01" min="0" value={form.price} onChange={set('price')} required />
              </div>
              <div className="form-group">
                <label>Stock</label>
                <input type="number" min="0" value={form.stockQuantity} onChange={set('stockQuantity')} required />
              </div>
              <div className="form-group">
                <label>Image URL</label>
                <input type="url" value={form.imageUrl} onChange={set('imageUrl')} />
              </div>
            </div>
            <button className="btn btn-primary" type="submit">Create</button>
          </form>
        </div>
      )}

      {editingId && (
        <div className="card">
          <h2>Edit Product</h2>
          <form onSubmit={handleEdit}>
            <div className="form-row">
              <div className="form-group">
                <label>Name</label>
                <input value={editForm.name} onChange={setEdit('name')} required />
              </div>
              <div className="form-group">
                <label>SKU</label>
                <input value={editForm.sku} onChange={setEdit('sku')} required />
              </div>
              <div className="form-group">
                <label>Price</label>
                <input type="number" step="0.01" min="0" value={editForm.price} onChange={setEdit('price')} required />
              </div>
              <div className="form-group">
                <label>Stock</label>
                <input type="number" min="0" value={editForm.stockQuantity} onChange={setEdit('stockQuantity')} required />
              </div>
              <div className="form-group">
                <label>Image URL</label>
                <input type="url" value={editForm.imageUrl} onChange={setEdit('imageUrl')} />
              </div>
            </div>
            <div style={{ display: 'flex', gap: '.5rem' }}>
              <button className="btn btn-primary" type="submit">Save</button>
              <button className="btn btn-outline" type="button" onClick={cancelEdit}>Cancel</button>
            </div>
          </form>
        </div>
      )}

      <div className="card">
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>SKU</th>
              <th>Price</th>
              <th>Stock</th>
              {(isAdmin || customerId) && <th></th>}
            </tr>
          </thead>
          <tbody>
            {data?.items.map(p => (
              <Fragment key={p.id}>
                <tr>
                  <td><Link to={`/products/${p.id}`}>{p.name}</Link></td>
                  <td><code>{p.sku}</code></td>
                  <td>${p.price.toFixed(2)}</td>
                  <td>{p.stockQuantity}</td>
                  {isAdmin && (
                    <td onClick={e => e.stopPropagation()}>
                      <div style={{ display: 'flex', gap: '.25rem' }}>
                        <button
                          className="btn btn-sm btn-outline"
                          onClick={() => editingId === p.id ? cancelEdit() : startEdit(p)}
                        >
                          {editingId === p.id ? 'Cancel' : 'Edit'}
                        </button>
                        <button
                          className="btn btn-sm btn-outline"
                          onClick={() => restockingId === p.id ? cancelRestock() : startRestock(p.id)}
                        >
                          {restockingId === p.id ? 'Cancel' : 'Restock'}
                        </button>
                        <button
                          className="btn btn-sm btn-danger"
                          onClick={() => handleDelete(p.id)}
                        >
                          Delete
                        </button>
                      </div>
                    </td>
                  )}
                  {!isAdmin && customerId && (
                    <td onClick={e => e.stopPropagation()}>
                      <button
                        className="btn btn-sm btn-primary"
                        onClick={() => { setOrderingId((id: string | null) => id === p.id ? null : p.id); setOrderQty(1) }}
                      >
                        {orderingId === p.id ? 'Cancel' : 'Order'}
                      </button>
                    </td>
                  )}
                </tr>
                {restockingId === p.id && (
                  <tr key={`${p.id}-restock`}>
                    <td colSpan={5} style={{ background: '#f8fafc', padding: '.75rem 1rem' }}>
                      <form onSubmit={e => handleRestock(e, p.id)} style={{ display: 'flex', gap: '.5rem', alignItems: 'flex-end' }}>
                        <div className="form-group" style={{ marginBottom: 0 }}>
                          <label>Quantity to add</label>
                          <input
                            type="number"
                            min="1"
                            value={restockQty}
                            onChange={e => setRestockQty(parseInt(e.target.value))}
                            required
                            style={{ width: 100 }}
                          />
                        </div>
                        <button className="btn btn-primary btn-sm" type="submit">Add Stock</button>
                        <button className="btn btn-outline btn-sm" type="button" onClick={cancelRestock}>Cancel</button>
                      </form>
                    </td>
                  </tr>
                )}
                {orderingId === p.id && (
                  <tr key={`${p.id}-order`}>
                    <td colSpan={5} style={{ background: '#f0f9ff', padding: '.75rem 1rem' }}>
                      <div style={{ marginBottom: '.5rem', display: 'flex', gap: '1.5rem', fontSize: 13 }}>
                        <span><strong>{p.name}</strong></span>
                        <span style={{ color: 'var(--muted)' }}>SKU: <code>{p.sku}</code></span>
                        <span>Unit price: <strong>${p.price.toFixed(2)}</strong></span>
                        <span>In stock: <strong>{p.stockQuantity}</strong></span>
                      </div>
                      <form onSubmit={e => handleOrder(e, p.id)} style={{ display: 'flex', gap: '.5rem', alignItems: 'flex-end' }}>
                        <div className="form-group" style={{ marginBottom: 0 }}>
                          <label>Quantity</label>
                          <input
                            type="number"
                            min="1"
                            max={p.stockQuantity}
                            value={orderQty}
                            onChange={e => setOrderQty(parseInt(e.target.value))}
                            required
                            style={{ width: 100 }}
                          />
                        </div>
                        <div style={{ paddingBottom: '2px', fontSize: 13, color: 'var(--muted)' }}>
                          Total: <strong style={{ color: 'var(--text)' }}>${(p.price * orderQty).toFixed(2)}</strong>
                        </div>
                        <button className="btn btn-primary btn-sm" type="submit">Place Order</button>
                        <button className="btn btn-outline btn-sm" type="button" onClick={() => { setOrderingId(null); setOrderQty(1) }}>Cancel</button>
                      </form>
                    </td>
                  </tr>
                )}
              </Fragment>
            ))}
            {data?.items.length === 0 && (
              <tr><td colSpan={5} style={{ textAlign: 'center', color: 'var(--muted)' }}>No products yet.</td></tr>
            )}
          </tbody>
        </table>
        {data && <Pagination page={page} pageSize={10} total={data.totalCount} onPage={setPage} />}
      </div>
    </>
  )
}
