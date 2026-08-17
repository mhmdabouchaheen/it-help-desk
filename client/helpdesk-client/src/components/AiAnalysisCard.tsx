import { useEffect, useRef, useState } from "react";
import { Sparkles } from "lucide-react";
import { analyzeTicketAsync } from "../api/ai";
import type { AiTicketAnalysisResponse } from "../types/ai";
export function AiAnalysisCard({ ticketId }: { ticketId: string }) {
  const [result, setResult] = useState<AiTicketAnalysisResponse>();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(false);
  const controller = useRef<AbortController | undefined>(undefined);
  useEffect(() => () => controller.current?.abort(), []);
  async function analyze() {
    if (loading) return;
    controller.current?.abort();
    const abort = new AbortController();
    controller.current = abort;
    setLoading(true);
    setError(false);
    try {
      setResult(await analyzeTicketAsync(ticketId, abort.signal));
    } catch {
      if (!abort.signal.aborted) setError(true);
    } finally {
      if (!abort.signal.aborted) setLoading(false);
    }
  }
  return (
    <section className="ai-analysis" aria-labelledby="ai-analysis-heading">
      <div className="section-heading">
        <div>
          <h2 id="ai-analysis-heading">
            <Sparkles aria-hidden="true" />
            AI Analysis
          </h2>
          <p>Optional suggestions to support human review.</p>
        </div>
        <button type="button" onClick={() => void analyze()} disabled={loading}>
          {loading ? "Analyzing…" : "Analyze Ticket"}
        </button>
      </div>
      {error && (
        <div className="error-summary" role="alert">
          AI analysis is temporarily unavailable. The ticket can still be
          managed normally.
        </div>
      )}
      {result && (
        <div className="ai-result">
          <span className="badge badge-accent">AI suggestion</span>
          <h3>Summary</h3>
          <p>{result.summary}</p>
          <dl>
            <div>
              <dt>Suggested category</dt>
              <dd>
                {result.recommendedCategoryName ?? "No category suggestion"}
              </dd>
            </div>
            <div>
              <dt>Suggested priority</dt>
              <dd>
                {result.recommendedPriorityName ?? "No priority suggestion"}
              </dd>
            </div>
          </dl>
          <h3>Troubleshooting suggestions</h3>
          {result.troubleshootingSuggestions.length ? (
            <ul>
              {result.troubleshootingSuggestions.map((item, index) => (
                <li key={index}>{item}</li>
              ))}
            </ul>
          ) : (
            <p>No troubleshooting suggestions were returned.</p>
          )}
          <p className="ai-disclaimer">
            <strong>Review before applying.</strong> {result.disclaimer}
          </p>
        </div>
      )}
    </section>
  );
}
