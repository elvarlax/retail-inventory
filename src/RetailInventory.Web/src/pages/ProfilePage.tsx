import { useState, useEffect } from 'react'
import { getCustomerById, updateCustomer } from '../api/customers'
import type { Customer } from '../types'

export default function ProfilePage() {
  const customerId = localStorage.getItem('customerId') ?? ''
  const [customer, setCustomer] = useState<Customer | null>(null)
  const [editing, setEditing] = useState(false)
  const [form, setForm] = useState({ firstName: '', lastName: '', email: '' })
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  useEffect(() => {
    if (!customerId) return
    getCustomerById(customerId)
      .then(c => { setCustomer(c); setForm({ firstName: c.firstName, lastName: c.lastName, email: c.email }) })
      .catch(err => setError((err as Error).message))
  }, [customerId])

  async function handleSave(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setSuccess('')
    try {
      await updateCustomer(customerId, form)
      localStorage.setItem('firstName', form.firstName)
      setCustomer(c => c ? { ...c, ...form } : c)
      setSuccess('Profile updated.')
      setEditing(false)
    } catch (err) {
      setError((err as Error).message)
    }
  }

  if (!customerId) return <div className="card">No customer profile linked to this account.</div>

  return (
    <>
      <div className="page-header">
        <h1>My Profile</h1>
        {!editing && <button className="btn btn-outline" onClick={() => setEditing(true)}>Edit</button>}
      </div>

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <div className="card">
        {!editing ? (
          <div style={{ display: 'grid', gap: '1rem' }}>
            <div><div style={{ fontSize: 11, fontWeight: 600, color: 'var(--muted)', textTransform: 'uppercase' }}>First Name</div>{customer?.firstName}</div>
            <div><div style={{ fontSize: 11, fontWeight: 600, color: 'var(--muted)', textTransform: 'uppercase' }}>Last Name</div>{customer?.lastName}</div>
            <div><div style={{ fontSize: 11, fontWeight: 600, color: 'var(--muted)', textTransform: 'uppercase' }}>Email</div>{customer?.email}</div>
          </div>
        ) : (
          <form onSubmit={handleSave}>
            <div className="form-row">
              <div className="form-group">
                <label>First Name</label>
                <input value={form.firstName} onChange={e => setForm(f => ({ ...f, firstName: e.target.value }))} required />
              </div>
              <div className="form-group">
                <label>Last Name</label>
                <input value={form.lastName} onChange={e => setForm(f => ({ ...f, lastName: e.target.value }))} required />
              </div>
              <div className="form-group">
                <label>Email</label>
                <input type="email" value={form.email} onChange={e => setForm(f => ({ ...f, email: e.target.value }))} required />
              </div>
            </div>
            <div style={{ display: 'flex', gap: '.5rem' }}>
              <button className="btn btn-primary" type="submit">Save</button>
              <button className="btn btn-outline" type="button" onClick={() => setEditing(false)}>Cancel</button>
            </div>
          </form>
        )}
      </div>
    </>
  )
}
