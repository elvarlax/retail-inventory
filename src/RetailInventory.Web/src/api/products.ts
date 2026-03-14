import { get, post, put, del } from './client'
import type { PagedResult, Product } from '../types'

export const getProducts = (page = 1, pageSize = 10, sortBy = 'name', sortDirection = 'asc', search = '') => {
  const params = new URLSearchParams({
    pageNumber: String(page),
    pageSize: String(pageSize),
    sortBy,
    sortDirection,
    ...(search ? { search } : {}),
  })
  return get<PagedResult<Product>>(`/api/v1/products?${params}`)
}

export const searchProducts = (search: string, pageSize = 20) =>
  getProducts(1, pageSize, 'name', 'asc', search)

export const getProductById = (id: string) =>
  get<Product>(`/api/v1/products/${id}`)

export const createProduct = (data: Omit<Product, 'id'>) =>
  post<void>('/api/v1/products', data)

export const updateProduct = (id: string, data: Omit<Product, 'id'>) =>
  put<void>(`/api/v1/products/${id}`, data)

export const restockProduct = (id: string, quantity: number) =>
  post<void>(`/api/v1/products/${id}/restock`, { quantity })

export const deleteProduct = (id: string) =>
  del<void>(`/api/v1/products/${id}`)
