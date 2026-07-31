import { useCallback, useEffect, useState } from "react";
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
  const reload = useCallback(async () => {
    setLoading(true);
    setError(undefined);
    try {
      setTicket(await getTicketAsync(id));
    } catch (caught) {
      setError(message(caught));
    } finally {
      setLoading(false);
    }
  }, [id]);
  useEffect(() => {
    const controller = new AbortController();
    getTicketAsync(id, controller.signal)
      .then(setTicket)
      .catch((caught) => {
        if ((caught as Error).name !== "AbortError") setError(message(caught));
      })
      .finally(() => setLoading(false));
    return () => controller.abort();
  }, [id]);
  return { ticket, setTicket, loading, error, reload };
}
