import { get, post, put, del } from './client'
import type { Customer, PagedResult } from '../types'

export const getCustomers = (page = 1, pageSize = 10, search = '') => {
  const params = new URLSearchParams({ pageNumber: String(page), pageSize: String(pageSize), ...(search ? { search } : {}) })
  return get<PagedResult<Customer>>(`/api/v1/customers?${params}`)
}

export const getCustomerById = (id: string) =>
  get<Customer>(`/api/v1/customers/${id}`)

export const createCustomer = (data: Omit<Customer, 'id'>) =>
  post<void>('/api/v1/customers', data)

export const updateCustomer = (id: string, data: Omit<Customer, 'id'>) =>
  put<void>(`/api/v1/customers/${id}`, data)

export const deleteCustomer = (id: string) =>
  del<void>(`/api/v1/customers/${id}`)
