import { useCallback, useEffect, useRef, useState } from "react";
import { getTicketAsync } from "../api/tickets";
import { ApiProblemError } from "../api/apiClient";
import type { TicketDetailResponse } from "../types/tickets";

function message(error: unknown) {
  const api = error instanceof ApiProblemError ? error : null;
  if (api?.status === 404) return "Ticket not found.";
  if (api?.status === 403) return "You do not have access to this ticket.";
  return api?.detail ?? "Ticket could not be loaded.";
}

export function useTicketDetail(id: string) {
  const [ticket, setTicket] = useState<TicketDetailResponse>();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>();
  const sequence = useRef(0);
  const controller = useRef<AbortController>(undefined);
  const reload = useCallback(async () => {
    controller.current?.abort();
    const current = ++sequence.current;
    const abort = new AbortController();
    controller.current = abort;
    setLoading(true);
    setError(undefined);
    try {
      const value = await getTicketAsync(id, abort.signal);
      if (current === sequence.current && !abort.signal.aborted) setTicket(value);
    } catch (caught) {
      if (current === sequence.current && !abort.signal.aborted) setError(message(caught));
    } finally {
      if (current === sequence.current && !abort.signal.aborted) setLoading(false);
    }
  }, [id]);
  useEffect(() => {
    // The initial request intentionally transitions this external request lifecycle to loading.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void reload();
    const active = controller.current;
    return () => active?.abort();
  }, [reload]);
  return { ticket, setTicket, loading, error, reload };
}
