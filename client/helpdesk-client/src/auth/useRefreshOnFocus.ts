import { useEffect, useRef } from 'react'

const DEFAULT_DEDUPLICATION_MS = 300

export function useRefreshOnFocus(
  refresh: () => void | Promise<void>,
  enabled = true,
  deduplicationMs = DEFAULT_DEDUPLICATION_MS,
) {
  const refreshRef = useRef(refresh)
  const lastRefreshAt = useRef(Number.NEGATIVE_INFINITY)
  useEffect(() => { refreshRef.current = refresh }, [refresh])

  useEffect(() => {
    if (!enabled) return
    const trigger = () => {
      const now = Date.now()
      if (now - lastRefreshAt.current < deduplicationMs) return
      lastRefreshAt.current = now
      void refreshRef.current()
    }
    const onVisibilityChange = () => {
      if (document.visibilityState === 'visible') trigger()
    }
    window.addEventListener('focus', trigger)
    document.addEventListener('visibilitychange', onVisibilityChange)
    return () => {
      window.removeEventListener('focus', trigger)
      document.removeEventListener('visibilitychange', onVisibilityChange)
    }
  }, [enabled, deduplicationMs])
}
