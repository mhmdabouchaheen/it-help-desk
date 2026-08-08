# Persistent notifications

Notifications use the existing `Notifications` table and the stable types `TicketAssigned`, `TicketStatusChanged`, `TicketCommentAdded`, `TicketInternalCommentAdded`, `TicketCancelled`, and `TicketAttachmentAdded`.

Recipients are derived from persisted ticket ownership/assignment and trusted application context. Assignment targets receive assignment events. Creators receive status changes. Public comments and attachments notify the creator and current assignee; internal comments notify only a support-role assignee. Cancellation notifies the creator and previous assignee. Every rule deduplicates recipients and excludes the acting user.

Ticket data is saved before notification creation. Notification creation is an in-process, best-effort follow-up: a notification failure is safely logged without message content and does not roll back the successful ticket operation.

## Real-time invalidation

SignalR adds delivery hints over the authoritative persistent store. The authenticated hub is `/hubs/notifications`; it has no client-callable application methods. A validated JWT `sub` (with the API's existing `NameIdentifier` inbound-mapping fallback) is parsed as a non-empty GUID and the connection joins only `user:{userId:D}`. Clients cannot supply a recipient, group, role, email, or notification.

After a notification is saved, the server sends `NotificationCreated` to that one private group. Its payload contains only `NotificationId`, nullable `TicketId`, `Type`, and `CreatedAtUtc`. A send failure does not undo persistence or fail the successful ticket operation. Connection IDs are never persisted and there is no global or role broadcast.

The JWT handler accepts the access-token query parameter only when the request path is exactly `/hubs/notifications`; normal API routes continue to require the bearer header. Refresh tokens are never sent to SignalR. Production must use HTTPS and configure reverse proxies for WebSocket upgrades and to avoid retaining handshake query strings in infrastructure logs. Events only invalidate state: the REST list, unread count, and read operations remain authoritative and fully functional while disconnected.

`GET /api/notifications` returns only the authenticated JWT subject's non-expired notifications and supports pagination and an unread filter. `GET /api/notifications/unread-count` applies the same recipient and expiry boundaries. `POST /api/notifications/{id}/read` and `POST /api/notifications/read-all` are idempotent and recipient-scoped. Expired records are hidden from lists/counts but may still be marked read by their owner. No endpoint accepts a recipient ID or creates arbitrary notifications.
