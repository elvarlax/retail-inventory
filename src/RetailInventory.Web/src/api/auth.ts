import { post } from './client'
import type { AuthResponse } from '../types'

export const login = (email: string, password: string) =>
  post<AuthResponse>('/auth/login', { email, password })

export const register = (firstName: string, lastName: string, email: string, password: string) =>
  post<AuthResponse>('/auth/register', { firstName, lastName, email, password })
