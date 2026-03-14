import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { getProductById } from '../api/products'
import { placeOrder } from '../api/orders'
import type { Product } from '../types'

export default function ProductDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [product, setProduct] = useState<Product | null>(null)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [qty, setQty] = useState(1)
  const [ordering, setOrdering] = useState(false)

  const customerId = localStorage.getItem('customerId') ?? ''
  const isAdmin = localStorage.getItem('role') === 'Admin'

  useEffect(() => {
    if (!id) return
    getProductById(id)
      .then(setProduct)
      .catch(err => setError((err as Error).message))
  }, [id])

  async function handleOrder(e: React.FormEvent) {
    e.preventDefault()
    if (!product) return
    setError('')
    setSuccess('')
    try {
      await placeOrder(customerId, [{ productId: product.id, quantity: qty }])
      setSuccess('Order placed! Go to Orders to track it.')
      setOrdering(false)
      setQty(1)
    } catch (err) {
      setError((err as Error).message)
    }
  }

  if (error) return <div className="alert alert-error">{error}</div>
  if (!product) return <div style={{ color: 'var(--muted)', padding: '2rem' }}>Loading…</div>

  return (
    <>
      <div className="page-header">
        <button className="btn btn-outline btn-sm" onClick={() => navigate(-1)}>← Back</button>
      </div>

      {success && <div className="alert alert-success">{success}</div>}

      <div className="card" style={{ display: 'flex', gap: '2rem', flexWrap: 'wrap' }}>
        {product.imageUrl && (
          <div style={{ flexShrink: 0 }}>
            <img
              src={product.imageUrl}
              alt={product.name}
              style={{ width: 220, height: 220, objectFit: 'cover', borderRadius: 10, border: '1px solid var(--border)' }}
            />
          </div>
        )}

        <div style={{ flex: 1, minWidth: 240 }}>
          <h1 style={{ fontSize: 22, marginBottom: '.5rem' }}>{product.name}</h1>
          <code style={{ fontSize: 12, color: 'var(--muted)' }}>{product.sku}</code>

          <div style={{ marginTop: '1.5rem', display: 'grid', gap: '1rem' }}>
            <div>
              <div style={{ fontSize: 11, fontWeight: 600, color: 'var(--muted)', textTransform: 'uppercase', letterSpacing: '.4px' }}>Price</div>
              <div style={{ fontSize: 28, fontWeight: 700, color: 'var(--primary)', marginTop: 2 }}>${product.price.toFixed(2)}</div>
            </div>
            <div>
              <div style={{ fontSize: 11, fontWeight: 600, color: 'var(--muted)', textTransform: 'uppercase', letterSpacing: '.4px' }}>In Stock</div>
              <div style={{ marginTop: 2 }}>
                <span className={`badge ${product.stockQuantity > 0 ? 'badge-completed' : 'badge-cancelled'}`}>
                  {product.stockQuantity > 0 ? `${product.stockQuantity} available` : 'Out of stock'}
                </span>
              </div>
            </div>
          </div>

          {!isAdmin && customerId && product.stockQuantity > 0 && (
            <div style={{ marginTop: '1.5rem' }}>
              {!ordering ? (
                <button className="btn btn-primary" onClick={() => setOrdering(true)}>Order Now</button>
              ) : (
                <form onSubmit={handleOrder} style={{ display: 'flex', gap: '.5rem', alignItems: 'flex-end' }}>
                  <div className="form-group" style={{ marginBottom: 0 }}>
                    <label>Quantity</label>
                    <input
                      type="number"
                      min={1}
                      max={product.stockQuantity}
                      value={qty}
                      onChange={e => setQty(parseInt(e.target.value))}
                      required
                      style={{ width: 90 }}
                    />
                  </div>
                  <div style={{ fontSize: 13, color: 'var(--muted)', paddingBottom: 2 }}>
                    Total: <strong style={{ color: 'var(--text)' }}>${(product.price * qty).toFixed(2)}</strong>
                  </div>
                  <button className="btn btn-primary" type="submit">Place Order</button>
                  <button className="btn btn-outline" type="button" onClick={() => { setOrdering(false); setQty(1) }}>Cancel</button>
                </form>
              )}
            </div>
          )}
        </div>
      </div>
    </>
  )
}
