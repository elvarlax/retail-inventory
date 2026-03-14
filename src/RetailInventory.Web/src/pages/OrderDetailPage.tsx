import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { getOrderById, completeOrder, cancelOrder, deleteOrder } from '../api/orders'
import type { Order } from '../types'

export default function OrderDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const isAdmin = localStorage.getItem('role') === 'Admin'
  const customerId = localStorage.getItem('customerId') ?? ''

  const [order, setOrder] = useState<Order | null>(null)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  async function load() {
    try {
      const o = await getOrderById(id!)
      setOrder(o)
    } catch (err) {
      setError((err as Error).message)
    }
  }

  useEffect(() => { load() }, [id])

  async function handleComplete() {
    try { await completeOrder(id!); setSuccess('Order completed.'); load() }
    catch (err) { setError((err as Error).message) }
  }

  async function handleCancel() {
    try { await cancelOrder(id!); setSuccess('Order cancelled.'); load() }
    catch (err) { setError((err as Error).message) }
  }

  async function handleDelete() {
    if (!window.confirm('Permanently delete this order?')) return
    try { await deleteOrder(id!); navigate(-1) }
    catch (err) { setError((err as Error).message) }
  }

  if (!order) return <div className="card">{error || 'Loading…'}</div>

  const canCancel = order.status === 'Pending' && (isAdmin || order.customerId === customerId)
  const canComplete = order.status === 'Pending' && isAdmin

  return (
    <>
      <div className="page-header">
        <h1>Order Detail</h1>
        <button className="btn btn-outline" onClick={() => navigate(-1)}>← Back</button>
      </div>

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <div className="card">
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))', gap: '1rem', marginBottom: '1.5rem' }}>
          <div><div style={{ fontSize: 11, fontWeight: 600, color: 'var(--muted)', textTransform: 'uppercase' }}>Order ID</div><code style={{ fontSize: 13 }}>{order.id}</code></div>
          <div><div style={{ fontSize: 11, fontWeight: 600, color: 'var(--muted)', textTransform: 'uppercase' }}>Status</div><span className={`badge badge-${order.status.toLowerCase()}`}>{order.status}</span></div>
          <div><div style={{ fontSize: 11, fontWeight: 600, color: 'var(--muted)', textTransform: 'uppercase' }}>Total</div><strong>${order.totalAmount.toFixed(2)}</strong></div>
          <div><div style={{ fontSize: 11, fontWeight: 600, color: 'var(--muted)', textTransform: 'uppercase' }}>Created</div>{new Date(order.createdAt).toLocaleString()}</div>
        </div>

        <table>
          <thead>
            <tr>
              <th>Product</th>
              <th>Unit Price</th>
              <th>Qty</th>
              <th>Subtotal</th>
            </tr>
          </thead>
          <tbody>
            {order.items?.map((item, i) => (
              <tr key={i}>
                <td>{item.productName}</td>
                <td>${item.unitPrice.toFixed(2)}</td>
                <td>{item.quantity}</td>
                <td>${(item.unitPrice * item.quantity).toFixed(2)}</td>
              </tr>
            ))}
          </tbody>
        </table>

        <div style={{ display: 'flex', gap: '.5rem', marginTop: '1.25rem' }}>
          {canComplete && <button className="btn btn-success" onClick={handleComplete}>Mark Complete</button>}
          {canCancel && <button className="btn btn-danger" onClick={handleCancel}>Cancel Order</button>}
          {isAdmin && <button className="btn btn-danger" onClick={handleDelete}>Delete Order</button>}
        </div>
      </div>
    </>
  )
}
