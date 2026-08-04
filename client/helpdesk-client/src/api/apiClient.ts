import { apiBaseUrl } from '../app/env'
import { tokenStore } from '../auth/tokenStore'
import type { AuthResponse, ProblemDetails } from '../types/auth'

export class ApiProblemError extends Error {
  readonly status: number
  readonly title: string
  readonly detail?: string
  readonly code?: string
  readonly traceId?: string
  readonly validationErrors?: Record<string, string[]>
  constructor(
    status: number, title: string, detail?: string, code?: string, traceId?: string,
    validationErrors?: Record<string, string[]>,
  ) {
    super(detail || title); this.name = 'ApiProblemError'; this.status = status; this.title = title
    this.detail = detail; this.code = code; this.traceId = traceId; this.validationErrors = validationErrors
  }
}

interface ApiOptions extends RequestInit { skipRefresh?: boolean }
let refreshPromise: Promise<boolean> | null = null

async function parseError(response: Response): Promise<ApiProblemError> {
  let problem: ProblemDetails = {}
  if (response.headers.get('content-type')?.includes('json')) {
    try { problem = await response.json() as ProblemDetails } catch { /* safe generic fallback */ }
  }
  return new ApiProblemError(response.status, problem.title ?? 'Request failed', problem.detail,
    problem.code, problem.traceId, problem.errors)
}

async function refreshOnce(): Promise<boolean> {
  const refreshToken = tokenStore.getRefreshToken()
  if (!refreshToken) return false
  if (!refreshPromise) {
    refreshPromise = fetch(`${apiBaseUrl}/api/auth/refresh`, {
      method: 'POST', headers: { Accept: 'application/json', 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken }),
    }).then(async (response) => {
      if (!response.ok) throw await parseError(response)
      const auth = await response.json() as AuthResponse
      tokenStore.set({ accessToken: auth.accessToken, refreshToken: auth.refreshToken })
      return true
    }).catch(() => { tokenStore.clear(); return false }).finally(() => { refreshPromise = null })
  }
  return refreshPromise
}

export async function apiResponse(path: string, options: ApiOptions = {}): Promise<Response> {
  const { skipRefresh = false, headers: suppliedHeaders, ...requestOptions } = options
  const headers = new Headers(suppliedHeaders)
  headers.set('Accept', 'application/json')
  if (requestOptions.body != null && !(requestOptions.body instanceof FormData)) headers.set('Content-Type', 'application/json')
  const accessToken = tokenStore.getAccessToken()
  if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`)
  const response = await fetch(`${apiBaseUrl}${path}`, { ...requestOptions, headers })
  if (response.status === 401 && !skipRefresh && accessToken && tokenStore.getRefreshToken() &&
      !path.startsWith('/api/auth/')) {
    if (await refreshOnce()) return apiResponse(path, { ...options, skipRefresh: true })
  }
  if (!response.ok) throw await parseError(response)
  return response
}

export async function apiRequest<T>(path: string, options: ApiOptions = {}): Promise<T> {
  const response = await apiResponse(path, options)
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}
