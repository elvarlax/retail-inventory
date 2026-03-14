import { useState, useEffect, useRef, Fragment } from 'react'
import { getOrders, getOrderSummary, placeOrder, completeOrder, cancelOrder, deleteOrder } from '../api/orders'
import { searchProducts } from '../api/products'
import { seedData } from '../api/admin'
import { Link } from 'react-router-dom'
import Pagination from '../components/Pagination'
import type { CreateOrderItem, Order, OrderSummary, PagedResult, Product } from '../types'

const emptyItem: CreateOrderItem = { productId: '', quantity: 1 }

interface ItemWithMeta extends CreateOrderItem {
  productLabel: string
}

const emptyItemWithMeta: ItemWithMeta = { productId: '', quantity: 1, productLabel: '' }

export default function OrdersPage() {
  const [data, setData] = useState<PagedResult<Order> | null>(null)
  const [summary, setSummary] = useState<OrderSummary | null>(null)
  const [page, setPage] = useState(1)
  const [statusFilter, setStatusFilter] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [expandedId, setExpandedId] = useState<string | null>(null)
  const customerId = localStorage.getItem('customerId') ?? ''
  const isAdmin = localStorage.getItem('role') === 'Admin'
  const [items, setItems] = useState<ItemWithMeta[]>([{ ...emptyItemWithMeta }])
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  // Per-row combobox state
  const [searchTexts, setSearchTexts] = useState<string[]>([''])
  const [searchResults, setSearchResults] = useState<Product[][]>([[]])
  const [openDropdown, setOpenDropdown] = useState<number | null>(null)
  const searchTimers = useRef<ReturnType<typeof setTimeout>[]>([])

  const [search, setSearch] = useState('')
  const searchTimer = useRef<ReturnType<typeof setTimeout> | null>(null)

  // Seed state (admin only)
  const [showSeed, setShowSeed] = useState(false)
  const [seed, setSeed] = useState({ customers: 50, products: 50, orders: 200 })
  const [seeding, setSeeding] = useState(false)

  async function load(p = page, s = statusFilter, q = search) {
    try {
      const ordersResult = await getOrders(p, 10, s, q)
      setData(ordersResult)
      if (isAdmin) {
        const sum = await getOrderSummary()
        setSummary(sum)
      }
    } catch (err) {
      setError((err as Error).message)
    }
  }

  useEffect(() => { load() }, [page, statusFilter])

  function handleSearch(value: string) {
    setSearch(value)
    setPage(1)
    if (searchTimer.current) clearTimeout(searchTimer.current)
    searchTimer.current = setTimeout(() => load(1, statusFilter, value), 300)
  }

  function openForm() {
    setShowForm(true)
    setItems([{ ...emptyItemWithMeta }])
    setSearchTexts([''])
    setSearchResults([[]])
    setOpenDropdown(null)
  }

  function handleSearchInput(index: number, value: string) {
    setSearchTexts(prev => prev.map((t, i) => i === index ? value : t))
    setOpenDropdown(index)
    // Clear selection when user types
    setItems(prev => prev.map((item, i) => i === index ? { ...item, productId: '', productLabel: '' } : item))

    clearTimeout(searchTimers.current[index])
    if (!value.trim()) {
      setSearchResults(prev => prev.map((r, i) => i === index ? [] : r))
      return
    }
    searchTimers.current[index] = setTimeout(async () => {
      const result = await searchProducts(value)
      setSearchResults(prev => prev.map((r, i) => i === index ? result.items : r))
    }, 250)
  }

  function selectProduct(index: number, product: Product) {
    setItems(prev => prev.map((item, i) =>
      i === index ? { ...item, productId: product.id, productLabel: product.name } : item
    ))
    setSearchTexts(prev => prev.map((t, i) => i === index ? product.name : t))
    setSearchResults(prev => prev.map((r, i) => i === index ? [] : r))
    setOpenDropdown(null)
  }

  function addItem() {
    setItems(prev => [...prev, { ...emptyItemWithMeta }])
    setSearchTexts(prev => [...prev, ''])
    setSearchResults(prev => [...prev, []])
  }

  function removeItem(index: number) {
    setItems(prev => prev.filter((_, i) => i !== index))
    setSearchTexts(prev => prev.filter((_, i) => i !== index))
    setSearchResults(prev => prev.filter((_, i) => i !== index))
  }

  async function handlePlace(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setSuccess('')
    try {
      await placeOrder(customerId, items.map(({ productId, quantity }) => ({ productId, quantity })))
      setSuccess('Order placed.')
      setItems([{ ...emptyItemWithMeta }])
      setSearchTexts([''])
      setSearchResults([[]])
      setShowForm(false)
      load()
    } catch (err) {
      setError((err as Error).message)
    }
  }

  async function handleComplete(id: string) {
    try { await completeOrder(id); load() }
    catch (err) { setError((err as Error).message) }
  }

  async function handleCancel(id: string) {
    try { await cancelOrder(id); load() }
    catch (err) { setError((err as Error).message) }
  }

  async function handleDelete(id: string) {
    if (!window.confirm('Permanently delete this order?')) return
    try { await deleteOrder(id); load() }
    catch (err) { setError((err as Error).message) }
  }

  async function handleSeed(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setSuccess('')
    setSeeding(true)
    try {
      const result = await seedData(seed.customers, seed.products, seed.orders) as { customers: number; products: number; orders: number }
      setSuccess(`Seeded ${result.customers} customers, ${result.products} products, ${result.orders} orders.`)
      setShowSeed(false)
      load()
    } catch (err) {
      setError((err as Error).message)
    } finally {
      setSeeding(false)
    }
  }

  return (
    <>
      <div className="page-header">
        <h1>Orders</h1>
        {isAdmin && (
          <div className="search-wrap">
            <span className="search-icon">⌕</span>
            <input
              type="search"
              placeholder="Search by customer name or email…"
              value={search}
              onChange={e => handleSearch(e.target.value)}
            />
          </div>
        )}
        <div style={{ display: 'flex', gap: '.5rem' }}>
          {isAdmin && (
            <button className="btn btn-outline" onClick={() => setShowSeed(s => !s)}>
              {showSeed ? 'Cancel' : '⚡ Seed Data'}
            </button>
          )}
          {customerId && !isAdmin && (
            <button className="btn btn-primary" onClick={() => showForm ? setShowForm(false) : openForm()}>
              {showForm ? 'Cancel' : '+ New Order'}
            </button>
          )}
        </div>
      </div>

      {summary && (
        <div className="summary-grid">
          <div className="summary-tile">
            <div className="label">Total Orders</div>
            <div className="value">{summary.totalOrders}</div>
          </div>
          <div className="summary-tile">
            <div className="label">Revenue</div>
            <div className="value">${summary.totalRevenue.toFixed(2)}</div>
          </div>
          <div className="summary-tile">
            <div className="label">Pending</div>
            <div className="value">{summary.pendingOrders}</div>
          </div>
          <div className="summary-tile">
            <div className="label">Completed</div>
            <div className="value">{summary.completedOrders}</div>
          </div>
          <div className="summary-tile">
            <div className="label">Cancelled</div>
            <div className="value">{summary.cancelledOrders}</div>
          </div>
        </div>
      )}

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      {showSeed && (
        <div className="card">
          <h2>Generate Test Data (Bogus)</h2>
          <form onSubmit={handleSeed}>
            <div className="form-row">
              <div className="form-group">
                <label>Customers</label>
                <input type="number" min="1" max="10000" value={seed.customers}
                  onChange={e => setSeed(s => ({ ...s, customers: parseInt(e.target.value) }))} required />
              </div>
              <div className="form-group">
                <label>Products</label>
                <input type="number" min="1" max="10000" value={seed.products}
                  onChange={e => setSeed(s => ({ ...s, products: parseInt(e.target.value) }))} required />
              </div>
              <div className="form-group">
                <label>Orders</label>
                <input type="number" min="1" max="50000" value={seed.orders}
                  onChange={e => setSeed(s => ({ ...s, orders: parseInt(e.target.value) }))} required />
              </div>
            </div>
            <button className="btn btn-primary" type="submit" disabled={seeding}>
              {seeding ? 'Generating…' : 'Generate'}
            </button>
          </form>
        </div>
      )}

      {showForm && (
        <div className="card">
          <h2>Place Order</h2>
          <form onSubmit={handlePlace}>
            <div className="items-builder">
              {items.map((item, i) => (
                <div className="item-row" key={i}>
                  <div className="form-group" style={{ flex: 3, position: 'relative' }}>
                    <label>Product</label>
                    <input
                      type="text"
                      placeholder="Search products…"
                      value={searchTexts[i] ?? ''}
                      onChange={e => handleSearchInput(i, e.target.value)}
                      onFocus={() => searchTexts[i] && setOpenDropdown(i)}
                      autoComplete="off"
                      required={!item.productId}
                      style={{ width: '100%' }}
                    />
                    {/* hidden input to satisfy required validation when product selected */}
                    <input type="hidden" value={item.productId} required />
                    {openDropdown === i && searchResults[i]?.length > 0 && (
                      <div style={{
                        position: 'absolute', top: '100%', left: 0, right: 0, zIndex: 100,
                        background: '#fff', border: '1px solid var(--border)', borderRadius: 6,
                        boxShadow: '0 4px 16px rgba(0,0,0,.12)', maxHeight: 260, overflowY: 'auto'
                      }}>
                        {searchResults[i].map(p => (
                          <div
                            key={p.id}
                            onMouseDown={() => selectProduct(i, p)}
                            style={{
                              padding: '.5rem .75rem', cursor: 'pointer', borderBottom: '1px solid var(--border)',
                              display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '1rem'
                            }}
                            onMouseEnter={e => (e.currentTarget.style.background = '#f1f5f9')}
                            onMouseLeave={e => (e.currentTarget.style.background = '#fff')}
                          >
                            <span style={{ fontWeight: 500 }}>{p.name}</span>
                            <span style={{ fontSize: 12, color: 'var(--muted)', whiteSpace: 'nowrap' }}>
                              ${p.price.toFixed(2)} · stock {p.stockQuantity}
                            </span>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                  <div className="form-group" style={{ flex: 1 }}>
                    <label>Qty</label>
                    <input type="number" min="1" value={item.quantity}
                      onChange={e => setItems(prev => prev.map((it, idx) =>
                        idx === i ? { ...it, quantity: parseInt(e.target.value) } : it
                      ))} required />
                  </div>
                  {item.productId && (
                    <div style={{ paddingBottom: 2, fontSize: 12, color: 'var(--muted)', alignSelf: 'flex-end', whiteSpace: 'nowrap' }}>
                      {(() => {
                        const p = searchResults[i]?.find(x => x.id === item.productId)
                        return p ? `$${(p.price * item.quantity).toFixed(2)}` : ''
                      })()}
                    </div>
                  )}
                  {items.length > 1 && (
                    <button type="button" className="btn btn-outline btn-sm" style={{ alignSelf: 'flex-end', marginBottom: 2 }}
                      onClick={() => removeItem(i)}>✕</button>
                  )}
                </div>
              ))}
            </div>
            <div style={{ display: 'flex', gap: '.5rem', marginTop: '.75rem' }}>
              <button type="button" className="btn btn-outline" onClick={addItem}>+ Add item</button>
              <button type="submit" className="btn btn-primary">Place Order</button>
            </div>
          </form>
        </div>
      )}

      <div className="card">
        <div style={{ display: 'flex', gap: '.5rem', marginBottom: '1rem', alignItems: 'center' }}>
          <label style={{ fontSize: 12, fontWeight: 600, color: 'var(--muted)' }}>Filter:</label>
          {(['', 'Pending', 'Completed', 'Cancelled'] as const).map(s => (
            <button key={s} className={`btn btn-sm ${statusFilter === s ? 'btn-primary' : 'btn-outline'}`}
              onClick={() => { setStatusFilter(s); setPage(1) }}>
              {s || 'All'}
            </button>
          ))}
        </div>
        <table>
          <thead>
            <tr>
              <th></th>
              <th>Order ID</th>
              {isAdmin && <th>Customer</th>}
              <th>Status</th>
              <th>Total</th>
              <th>Created</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {data?.items.map(o => (
              <Fragment key={o.id}>
                <tr style={{ cursor: 'pointer' }} onClick={() => setExpandedId(expandedId === o.id ? null : o.id)}>
                  <td style={{ color: 'var(--muted)', fontSize: 11 }}>{expandedId === o.id ? '▲' : '▼'}</td>
                  <td><Link to={`/orders/${o.id}`} onClick={e => e.stopPropagation()}><code title={o.id}>{o.id.slice(0, 8)}…</code></Link></td>
                  {isAdmin && <td>{o.customerName}</td>}
                  <td><span className={`badge badge-${o.status.toLowerCase()}`}>{o.status}</span></td>
                  <td>${o.totalAmount.toFixed(2)}</td>
                  <td>{new Date(o.createdAt).toLocaleDateString()}</td>
                  <td onClick={e => e.stopPropagation()}>
                    <div style={{ display: 'flex', gap: '.25rem' }}>
                      {o.status === 'Pending' && isAdmin && (
                        <button className="btn btn-success btn-sm" onClick={() => handleComplete(o.id)}>Complete</button>
                      )}
                      {o.status === 'Pending' && (
                        <button className="btn btn-danger btn-sm" onClick={() => handleCancel(o.id)}>Cancel</button>
                      )}
                      {isAdmin && (
                        <button className="btn btn-danger btn-sm" onClick={() => handleDelete(o.id)}>Delete</button>
                      )}
                    </div>
                  </td>
                </tr>
                {expandedId === o.id && (
                  <tr>
                    <td colSpan={6} style={{ background: '#f8fafc', padding: '0 1rem 1rem 2.5rem' }}>
                      <table style={{ marginTop: '.75rem' }}>
                        <thead>
                          <tr>
                            <th>Product</th>
                            <th>Unit Price</th>
                            <th>Qty</th>
                            <th>Subtotal</th>
                          </tr>
                        </thead>
                        <tbody>
                          {o.items?.map((item, i) => (
                            <tr key={i}>
                              <td>{item.productName}</td>
                              <td>${item.unitPrice.toFixed(2)}</td>
                              <td>{item.quantity}</td>
                              <td>${(item.unitPrice * item.quantity).toFixed(2)}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </td>
                  </tr>
                )}
              </Fragment>
            ))}
            {data?.items.length === 0 && (
              <tr><td colSpan={isAdmin ? 7 : 6} style={{ textAlign: 'center', color: 'var(--muted)' }}>No orders yet.</td></tr>
            )}
          </tbody>
        </table>
        {data && <Pagination page={page} pageSize={10} total={data.totalCount} onPage={setPage} />}
      </div>
    </>
  )
}
