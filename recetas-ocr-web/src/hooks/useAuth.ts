import { useAuthStore } from '../store/auth.store'
import { authApi } from '../api/auth.api'

export function useAuth() {
  const store = useAuthStore()

  async function login(username: string, password: string) {
    const res = await authApi.login({ username, password })
    store.setAuth(res.token, res.refreshToken, res.usuario)
    return res
  }

  return { ...store, login }
}
