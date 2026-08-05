import { apiRequest } from './apiClient'
import type { DashboardResponse } from '../types/dashboard'
export function getDashboardAsync(signal?: AbortSignal) { return apiRequest<DashboardResponse>('/api/dashboard', { method: 'GET', signal }) }
