import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { register } from '../api/auth'

interface Form {
  firstName: string
  lastName: string
  email: string
  password: string
}

export default function RegisterPage() {
  const navigate = useNavigate()
  const [form, setForm] = useState<Form>({ firstName: '', lastName: '', email: '', password: '' })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const set = (field: keyof Form) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm(f => ({ ...f, [field]: e.target.value }))

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const data = await register(form.firstName, form.lastName, form.email, form.password)
      localStorage.setItem('token', data.accessToken)
      localStorage.setItem('role', data.role)
      if (data.customerId) localStorage.setItem('customerId', data.customerId)
      if (data.firstName) localStorage.setItem('firstName', data.firstName)
      navigate('/products')
    } catch (err) {
      setError((err as Error).message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="auth-wrapper">
      <div className="auth-card">
        <h1>Create account</h1>
        {error && <div className="alert alert-error">{error}</div>}
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label>First name</label>
            <input value={form.firstName} onChange={set('firstName')} required autoFocus />
          </div>
          <div className="form-group">
            <label>Last name</label>
            <input value={form.lastName} onChange={set('lastName')} required />
          </div>
          <div className="form-group">
            <label>Email</label>
            <input type="email" value={form.email} onChange={set('email')} required />
          </div>
          <div className="form-group">
            <label>Password</label>
            <input type="password" value={form.password} onChange={set('password')} required />
          </div>
          <button className="btn btn-primary" type="submit" disabled={loading}>
            {loading ? 'Creating account…' : 'Create account'}
          </button>
        </form>
        <p className="link">Already have an account? <Link to="/login">Sign in</Link></p>
      </div>
    </div>
  )
}
