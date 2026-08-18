import { useCallback, useEffect, useState } from 'react'
import { getEligibleSupportUsersAsync } from '../api/tickets'
import type { SupportUserResponse } from '../types/tickets'

let generation = 0
let pending: { generation: number; promise: Promise<SupportUserResponse[]> } | null = null

function load() {
  if (pending?.generation === generation) return pending.promise
  const requestGeneration = generation
  const promise = getEligibleSupportUsersAsync().finally(() => {
    if (pending?.promise === promise) pending = null
  })
  pending = { generation: requestGeneration, promise }
  return promise
}

export function invalidateSupportUsers() { generation += 1; pending = null }

export function useSupportUsers(enabled: boolean) {
  const [users, setUsers] = useState<SupportUserResponse[]>([])
  const [isLoading, setIsLoading] = useState(enabled)
  const [error, setError] = useState<string>()
  const [reloadGeneration, setReloadGeneration] = useState(0)
  const reload = useCallback(() => { invalidateSupportUsers(); setError(undefined); setIsLoading(true); setReloadGeneration((x) => x + 1) }, [])
  useEffect(() => {
    if (!enabled) return
    let active = true
    const requestGeneration = generation
    load().then((value) => { if (active && requestGeneration === generation) setUsers(value) })
      .catch(() => { if (active && requestGeneration === generation) setError('Support users could not be loaded.') })
      .finally(() => { if (active && requestGeneration === generation) setIsLoading(false) })
    return () => { active = false }
  }, [enabled, reloadGeneration])
  return { users: enabled ? users : [], isLoading: enabled && isLoading, error: enabled ? error : undefined, reload }
}
