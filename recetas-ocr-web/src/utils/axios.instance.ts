import axios, { type InternalAxiosRequestConfig } from 'axios'
import { useAuthStore } from '../store/auth.store'
import type { ApiResponse, LoginResponseDto } from '../types/auth.types'

type RetryConfig = InternalAxiosRequestConfig & { _retry?: boolean }

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  timeout: 30000,
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().token
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config as RetryConfig | undefined
    const url = original?.url ?? ''
    if (
      error.response?.status === 401 &&
      !original?._retry &&
      !url.includes('/auth/login') &&
      !url.includes('/auth/refresh')
    ) {
      if (original) original._retry = true
      try {
        const store = useAuthStore.getState()
        const { data } = await axios.post<ApiResponse<LoginResponseDto>>(
          `${import.meta.env.VITE_API_BASE_URL}/auth/refresh`,
          { refreshToken: store.refreshToken }
        )
        const refreshed = data.data!
        useAuthStore.getState().updateToken(refreshed.token, refreshed.refreshToken)
        if (original) {
          original.headers['Authorization'] = `Bearer ${refreshed.token}`
        }
        return api(original!)
      } catch {
        useAuthStore.getState().logout()
        window.location.href = '/login'
      }
    }
    const resData = error.response?.data as ApiResponse<unknown> | undefined
    const message = resData?.errors?.[0] ?? resData?.message ?? 'Error desconocido'
    return Promise.reject(new Error(message))
  }
)

export default api
