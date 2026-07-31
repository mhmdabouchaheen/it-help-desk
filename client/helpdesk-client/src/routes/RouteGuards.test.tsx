import { describe, expect, it } from 'vitest'
import { safeDestination } from '../utils/safeDestination'
describe('safeDestination', () => {
  it('accepts internal relative paths', () => expect(safeDestination('/app/home')).toBe('/app/home'))
  it.each(['https://evil.test','//evil.test',null])('rejects external or invalid destinations', value => expect(safeDestination(value)).toBe('/app/home'))
})
