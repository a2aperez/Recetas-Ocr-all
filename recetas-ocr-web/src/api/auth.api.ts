import api from '../utils/axios.instance'
import type { ApiResponse, LoginRequestDto, LoginResponseDto } from '../types/auth.types'

export const authApi = {
  login: async (body: LoginRequestDto): Promise<LoginResponseDto> => {
    const { data } = await api.post<ApiResponse<LoginResponseDto>>('/auth/login', body)
    return data.data!
  },
  refresh: async (refreshToken: string): Promise<LoginResponseDto> => {
    const { data } = await api.post<ApiResponse<LoginResponseDto>>('/auth/refresh', { refreshToken })
    return data.data!
  },
  logout: async (): Promise<void> => {
    await api.post('/auth/logout')
  },
}
