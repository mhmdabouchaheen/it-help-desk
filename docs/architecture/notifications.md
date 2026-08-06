# Persistent notifications

Notifications use the existing `Notifications` table and the stable types `TicketAssigned`, `TicketStatusChanged`, `TicketCommentAdded`, `TicketInternalCommentAdded`, `TicketCancelled`, and `TicketAttachmentAdded`.

Recipients are derived from persisted ticket ownership/assignment and trusted application context. Assignment targets receive assignment events. Creators receive status changes. Public comments and attachments notify the creator and current assignee; internal comments notify only a support-role assignee. Cancellation notifies the creator and previous assignee. Every rule deduplicates recipients and excludes the acting user.

Ticket data is saved before notification creation. Notification creation is an in-process, best-effort follow-up: a notification failure is safely logged without message content and does not roll back the successful ticket operation. SignalR is deferred.

`GET /api/notifications` returns only the authenticated JWT subject's non-expired notifications and supports pagination and an unread filter. `GET /api/notifications/unread-count` applies the same recipient and expiry boundaries. `POST /api/notifications/{id}/read` and `POST /api/notifications/read-all` are idempotent and recipient-scoped. Expired records are hidden from lists/counts but may still be marked read by their owner. No endpoint accepts a recipient ID or creates arbitrary notifications.
