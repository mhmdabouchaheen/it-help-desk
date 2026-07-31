export function normalizeApiBaseUrl(value: string | undefined): string {
  const normalized = value?.trim().replace(/\/+$/, '')
  if (!normalized) throw new Error('VITE_API_BASE_URL is required.')
  return normalized
}

export const apiBaseUrl = normalizeApiBaseUrl(import.meta.env.VITE_API_BASE_URL)
