export interface Product {
  id: string
  name: string
  sku: string
  imageUrl?: string
  price: number
  stockQuantity: number
}

export interface Customer {
  id: string
  firstName: string
  lastName: string
  email: string
}

export interface OrderItem {
  productId: string
  productName: string
  quantity: number
  unitPrice: number
}

export interface Order {
  id: string
  customerId: string
  customerName: string
  status: 'Pending' | 'Completed' | 'Cancelled'
  totalAmount: number
  createdAt: string
  completedAt?: string
  items: OrderItem[]
}

export interface OrderSummary {
  totalOrders: number
  totalRevenue: number
  pendingOrders: number
  completedOrders: number
  cancelledOrders: number
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
}

export interface AuthResponse {
  accessToken: string
  tokenType: string
  role: string
  customerId?: string
  firstName?: string
}

export interface TopProduct {
  productId: string
  productName: string
  sku: string
  unitsSold: number
  revenue: number
}

export interface CreateOrderItem {
  productId: string
  quantity: number
}
