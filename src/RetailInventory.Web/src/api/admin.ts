import { post } from './client'

export const seedData = (customers: number, products: number, orders: number) =>
  post('/admin/seed', { customers, products, orders })
