import { useState, useEffect, useRef } from 'react'
import { useSearchParams } from 'react-router-dom'
import { getCustomers, createCustomer, updateCustomer, deleteCustomer } from '../api/customers'
import Pagination from '../components/Pagination'
import type { Customer, PagedResult } from '../types'

interface CustomerForm {
  firstName: string
  lastName: string
  email: string
}

const empty: CustomerForm = { firstName: '', lastName: '', email: '' }

export default function CustomersPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [data, setData] = useState<PagedResult<Customer> | null>(null)
  const page = parseInt(searchParams.get('page') ?? '1', 10)
  const setPage = (p: number) => setSearchParams(prev => {
    prev.set('page', String(p))
    return prev
  }, { replace: true })
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState<CustomerForm>(empty)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const isAdmin = localStorage.getItem('role') === 'Admin'

  const [editingId, setEditingId] = useState<string | null>(null)
  const [editForm, setEditForm] = useState<CustomerForm>(empty)
  const [search, setSearch] = useState('')
  const searchTimer = useRef<ReturnType<typeof setTimeout> | null>(null)

  const set = (field: keyof CustomerForm) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm(f => ({ ...f, [field]: e.target.value }))

  const setEdit = (field: keyof CustomerForm) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setEditForm(f => ({ ...f, [field]: e.target.value }))

  async function load(p = page, s = search) {
    try {
      const result = await getCustomers(p, 10, s)
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
      await createCustomer(form)
      setSuccess('Customer created.')
      setForm(empty)
      setShowForm(false)
      load()
    } catch (err) {
      setError((err as Error).message)
    }
  }

  function startEdit(c: Customer) {
    setEditingId(c.id)
    setEditForm({ firstName: c.firstName, lastName: c.lastName, email: c.email })
  }

  function cancelEdit() {
    setEditingId(null)
    setEditForm(empty)
  }

  async function handleDelete(id: string) {
    if (!window.confirm('Permanently delete this customer?')) return
    setError('')
    setSuccess('')
    try {
      await deleteCustomer(id)
      setSuccess('Customer deleted.')
      load()
    } catch (err) {
      setError((err as Error).message)
    }
  }

  async function handleEdit(e: React.FormEvent) {
    e.preventDefault()
    if (!editingId) return
    setError('')
    setSuccess('')
    try {
      await updateCustomer(editingId, editForm)
      setSuccess('Customer updated.')
      cancelEdit()
      load()
    } catch (err) {
      setError((err as Error).message)
    }
  }

  return (
    <>
      <div className="page-header">
        <h1>Customers</h1>
        <div className="search-wrap">
          <span className="search-icon">⌕</span>
          <input
            type="search"
            placeholder="Search by name or email..."
            value={search}
            onChange={e => handleSearch(e.target.value)}
          />
        </div>
        {isAdmin && (
          <button className="btn btn-primary" onClick={() => { setShowForm(s => !s); cancelEdit() }}>
            {showForm ? 'Cancel' : '+ New Customer'}
          </button>
        )}
      </div>

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      {showForm && (
        <div className="card">
          <h2>New Customer</h2>
          <form onSubmit={handleCreate}>
            <div className="form-row">
              <div className="form-group">
                <label>First name</label>
                <input value={form.firstName} onChange={set('firstName')} required />
              </div>
              <div className="form-group">
                <label>Last name</label>
                <input value={form.lastName} onChange={set('lastName')} required />
              </div>
              <div className="form-group">
                <label>Email</label>
                <input type="email" value={form.email} onChange={set('email')} required />
              </div>
            </div>
            <button className="btn btn-primary" type="submit">Create</button>
          </form>
        </div>
      )}

      {editingId && (
        <div className="card">
          <h2>Edit Customer</h2>
          <form onSubmit={handleEdit}>
            <div className="form-row">
              <div className="form-group">
                <label>First name</label>
                <input value={editForm.firstName} onChange={setEdit('firstName')} required />
              </div>
              <div className="form-group">
                <label>Last name</label>
                <input value={editForm.lastName} onChange={setEdit('lastName')} required />
              </div>
              <div className="form-group">
                <label>Email</label>
                <input type="email" value={editForm.email} onChange={setEdit('email')} required />
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
              <th>First name</th>
              <th>Last name</th>
              <th>Email</th>
              {isAdmin && <th></th>}
            </tr>
          </thead>
          <tbody>
            {data?.items.map(c => (
              <tr key={c.id}>
                <td>{c.firstName}</td>
                <td>{c.lastName}</td>
                <td>{c.email}</td>
                {isAdmin && (
                  <td>
                    <div style={{ display: 'flex', gap: '.25rem' }}>
                      <button
                        className="btn btn-sm btn-outline"
                        onClick={() => editingId === c.id ? cancelEdit() : startEdit(c)}
                      >
                        {editingId === c.id ? 'Cancel' : 'Edit'}
                      </button>
                      <button className="btn btn-sm btn-danger" onClick={() => handleDelete(c.id)}>Delete</button>
                    </div>
                  </td>
                )}
              </tr>
            ))}
            {data?.items.length === 0 && (
              <tr><td colSpan={isAdmin ? 4 : 3} style={{ textAlign: 'center', color: 'var(--muted)' }}>No customers yet.</td></tr>
            )}
          </tbody>
        </table>
        {data && <Pagination page={page} pageSize={10} total={data.totalCount} onPage={setPage} />}
      </div>
    </>
  )
}
