import { useState, useEffect } from 'react'
import { getOrderSummary, getOrders, getTopProducts } from '../api/orders'
import { Link } from 'react-router-dom'
import type { Order, OrderSummary, TopProduct } from '../types'

export default function DashboardPage() {
  const [summary, setSummary] = useState<OrderSummary | null>(null)
  const [pendingOrders, setPendingOrders] = useState<Order[]>([])
  const [topProducts, setTopProducts] = useState<TopProduct[]>([])
  const [error, setError] = useState('')

  useEffect(() => {
    Promise.all([
      getOrderSummary(),
      getOrders(1, 10, 'Pending'),
      getTopProducts(5),
    ])
      .then(([sum, orders, products]) => {
        setSummary(sum)
        setPendingOrders(orders.items)
        setTopProducts(products)
      })
      .catch(err => setError((err as Error).message))
  }, [])

  return (
    <>
      <div className="page-header">
        <h1>Dashboard</h1>
      </div>

      {error && <div className="alert alert-error">{error}</div>}

      {summary && (
        <div className="summary-grid">
          <div className="summary-tile"><div className="label">Total Orders</div><div className="value">{summary.totalOrders}</div></div>
          <div className="summary-tile"><div className="label">Revenue</div><div className="value">${summary.totalRevenue.toFixed(2)}</div></div>
          <div className="summary-tile"><div className="label">Pending</div><div className="value">{summary.pendingOrders}</div></div>
          <div className="summary-tile"><div className="label">Completed</div><div className="value">{summary.completedOrders}</div></div>
          <div className="summary-tile"><div className="label">Cancelled</div><div className="value">{summary.cancelledOrders}</div></div>
        </div>
      )}

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
        <div className="card">
          <h2>Pending Orders</h2>
          <table>
            <thead><tr><th>Order ID</th><th>Customer</th><th>Total</th><th>Created</th><th></th></tr></thead>
            <tbody>
              {pendingOrders.map(o => (
                <tr key={o.id}>
                  <td><code title={o.id}>{o.id.slice(0, 8)}…</code></td>
                  <td>{o.customerName}</td>
                  <td>${o.totalAmount.toFixed(2)}</td>
                  <td>{new Date(o.createdAt).toLocaleDateString()}</td>
                  <td><Link className="btn btn-sm btn-outline" to={`/orders/${o.id}`}>View</Link></td>
                </tr>
              ))}
              {pendingOrders.length === 0 && <tr><td colSpan={5} style={{ textAlign: 'center', color: 'var(--muted)' }}>No pending orders.</td></tr>}
            </tbody>
          </table>
        </div>

        <div className="card">
          <h2>Top Selling Products</h2>
          <table>
            <thead><tr><th>Product</th><th>SKU</th><th>Units Sold</th><th>Revenue</th></tr></thead>
            <tbody>
              {topProducts.map((p, i) => (
                <tr key={p.productId}>
                  <td>
                    <span style={{ fontSize: 11, fontWeight: 700, color: 'var(--muted)', marginRight: '.4rem' }}>#{i + 1}</span>
                    {p.productName}
                  </td>
                  <td><code>{p.sku}</code></td>
                  <td>{p.unitsSold.toLocaleString()}</td>
                  <td style={{ fontWeight: 600 }}>${p.revenue.toFixed(2)}</td>
                </tr>
              ))}
              {topProducts.length === 0 && <tr><td colSpan={4} style={{ textAlign: 'center', color: 'var(--muted)' }}>No sales data yet.</td></tr>}
            </tbody>
          </table>
        </div>
      </div>
    </>
  )
}
