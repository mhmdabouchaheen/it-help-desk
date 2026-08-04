import { useCallback, useEffect, useState } from 'react'
import { getEligibleSupportUsersAsync } from '../api/tickets'
import type { SupportUserResponse } from '../types/tickets'

let cache: SupportUserResponse[] | null = null
let pending: Promise<SupportUserResponse[]> | null = null

function load() {
  if (cache) return Promise.resolve(cache)
  if (!pending) pending = getEligibleSupportUsersAsync().then((users) => (cache = users)).finally(() => { pending = null })
  return pending
}

export function invalidateSupportUsers() { cache = null }

export function useSupportUsers(enabled: boolean) {
  const [users, setUsers] = useState<SupportUserResponse[]>(enabled ? cache ?? [] : [])
  const [isLoading, setIsLoading] = useState(enabled && !cache)
  const [error, setError] = useState<string>()
  const [generation, setGeneration] = useState(0)
  const reload = useCallback(() => { cache = null; setError(undefined); setIsLoading(true); setGeneration((x) => x + 1) }, [])
  useEffect(() => {
    if (!enabled) return
    let active = true
    load().then((value) => { if (active) setUsers(value) })
      .catch(() => { if (active) setError('Support users could not be loaded.') })
      .finally(() => { if (active) setIsLoading(false) })
    return () => { active = false }
  }, [enabled, generation])
  return { users: enabled ? users : [], isLoading: enabled && isLoading, error: enabled ? error : undefined, reload }
}
