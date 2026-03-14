import { NavLink, useNavigate } from 'react-router-dom'

export default function Navbar() {
  const navigate = useNavigate()
  const role = localStorage.getItem('role') ?? ''
  const firstName = localStorage.getItem('firstName')
  const customerId = localStorage.getItem('customerId')

  function logout() {
    localStorage.clear()
    navigate('/login')
  }

  return (
    <nav>
      <span className="brand">Retail Inventory</span>
      {role === 'Admin' && <NavLink to="/dashboard">Dashboard</NavLink>}
      <NavLink to="/products">Products</NavLink>
      {role === 'Admin' && <NavLink to="/customers">Customers</NavLink>}
      <NavLink to="/orders">Orders</NavLink>
      {!role.includes('Admin') && customerId && <NavLink to="/profile">Profile</NavLink>}
      {firstName && <span style={{ color: '#94a3b8', fontSize: 13 }}>{firstName}</span>}
      <span className={`badge badge-${role.toLowerCase()}`}>{role}</span>
      <button className="logout" onClick={logout}>Logout</button>
    </nav>
  )
}
