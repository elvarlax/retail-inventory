import { get, post, del } from './client'
import type { CreateOrderItem, Order, OrderSummary, PagedResult, TopProduct } from '../types'

export const getOrders = (page = 1, pageSize = 10, status = '', search = '') => {
  const params = new URLSearchParams({ pageNumber: String(page), pageSize: String(pageSize), ...(status ? { status } : {}), ...(search ? { search } : {}) })
  return get<PagedResult<Order>>(`/api/v1/orders?${params}`)
}

export const getOrderSummary = () => get<OrderSummary>('/api/v1/orders/summary')

export const getOrderById = (id: string) =>
  get<Order>(`/api/v1/orders/${id}`)

export const placeOrder = (customerId: string, items: CreateOrderItem[]) =>
  post<{ orderId: string }>('/api/v1/orders', { customerId, items })

export const completeOrder = (id: string) => post<void>(`/api/v1/orders/${id}/complete`)
export const cancelOrder = (id: string) => post<void>(`/api/v1/orders/${id}/cancel`)
export const deleteOrder = (id: string) => del<void>(`/api/v1/orders/${id}`)

export const getTopProducts = (limit = 5) => get<TopProduct[]>(`/api/v1/orders/top-products?limit=${limit}`)
