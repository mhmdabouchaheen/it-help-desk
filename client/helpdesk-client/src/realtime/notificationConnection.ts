import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import { apiBaseUrl } from '../app/env'
import type { NotificationRealtimeEvent } from '../types/notifications'

export type NotificationConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting'

export interface NotificationConnectionOptions {
  getAccessToken(): string | null
  onNotificationCreated(event: NotificationRealtimeEvent): void
  onStatusChanged?(status: NotificationConnectionStatus): void
}

export function isNotificationRealtimeEvent(value: unknown): value is NotificationRealtimeEvent {
  if (!value || typeof value !== 'object') return false
  const event = value as Record<string, unknown>
  return typeof event.notificationId === 'string' && event.notificationId.length > 0 &&
    (event.ticketId === null || typeof event.ticketId === 'string') &&
    typeof event.type === 'string' && event.type.length > 0 &&
    typeof event.createdAtUtc === 'string' && !Number.isNaN(Date.parse(event.createdAtUtc))
}

export function createNotificationConnection(options: NotificationConnectionOptions) {
  let connection: HubConnection | null = null
  let operation: Promise<void> | null = null
  let started = false
  const handler = (event: unknown) => { if (isNotificationRealtimeEvent(event)) options.onNotificationCreated(event) }

  const build = () => {
    const next = new HubConnectionBuilder()
      .withUrl(`${apiBaseUrl}/hubs/notifications`, {
        accessTokenFactory: () => options.getAccessToken() ?? '',
        withCredentials: false,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()
    next.on('NotificationCreated', handler)
    next.onreconnecting(() => options.onStatusChanged?.('reconnecting'))
    next.onreconnected(() => options.onStatusChanged?.('connected'))
    next.onclose(() => { started = false; options.onStatusChanged?.('disconnected') })
    return next
  }

  return {
    async start() {
      if (operation) return operation
      if (started) return
      if (connection && connection.state !== HubConnectionState.Disconnected) return
      connection ??= build()
      options.onStatusChanged?.('connecting')
      operation = connection.start().then(() => { started = true; options.onStatusChanged?.('connected') })
        .catch(() => { started = false; options.onStatusChanged?.('disconnected') })
        .finally(() => { operation = null })
      return operation
    },
    async stop() {
      if (operation) await operation
      if (!connection) { options.onStatusChanged?.('disconnected'); return }
      const current = connection
      connection = null
      started = false
      current.off('NotificationCreated', handler)
      try { await current.stop() } finally { options.onStatusChanged?.('disconnected') }
    },
  }
}
