import { create } from 'zustand'
import { persist, createJSONStorage } from 'zustand/middleware'
import type { UsuarioSesionDto } from '../types/auth.types'

interface AuthState {
  token: string | null
  refreshToken: string | null
  usuario: UsuarioSesionDto | null
  isAuthenticated: boolean
  setAuth: (token: string, refreshToken: string, usuario: UsuarioSesionDto) => void
  logout: () => void
  updateToken: (token: string, refreshToken: string) => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      refreshToken: null,
      usuario: null,
      isAuthenticated: false,
      setAuth: (token, refreshToken, usuario) =>
        set({ token, refreshToken, usuario, isAuthenticated: true }),
      logout: () =>
        set({ token: null, refreshToken: null, usuario: null, isAuthenticated: false }),
      updateToken: (token, refreshToken) =>
        set({ token, refreshToken }),
    }),
    {
      name: 'auth-storage',
      storage: createJSONStorage(() => sessionStorage),
    }
  )
)
