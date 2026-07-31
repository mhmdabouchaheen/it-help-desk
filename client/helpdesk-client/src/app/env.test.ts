import { describe, expect, it } from 'vitest'
import { normalizeApiBaseUrl } from './env'
describe('environment', () => {
  it('normalizes trailing slashes', () => expect(normalizeApiBaseUrl(' https://api.test/// ')).toBe('https://api.test'))
  it('rejects a missing URL', () => expect(() => normalizeApiBaseUrl(undefined)).toThrow('VITE_API_BASE_URL'))
})
