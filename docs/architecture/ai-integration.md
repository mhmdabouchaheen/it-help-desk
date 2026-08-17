# AI ticket analysis

AI ticket analysis is optional and advisory. `POST /api/tickets/{ticketId}/ai-analysis` requires normal authentication and delegates ticket visibility to the existing `ITicketService` with the JWT-derived `TicketAccessContext`. It never performs ticket updates, assignment, status changes, comments, cancellation, or other mutations.

`IAiTicketAnalysisService` is vendor-neutral and uses `IAiTicketProvider`. `Ai:Provider` selects either `OpenAI` or `Ollama` without changing the endpoint, authorization, UI contract, or ticket-analysis service. OpenAI uses the Responses API with structured JSON output. Ollama uses its local `/api/chat` endpoint with streaming disabled, temperature zero, and a JSON schema. A missing provider, unavailable local service, missing local model, or missing OpenAI key does not prevent startup and produces a safe `503 ai_service_unavailable` response.

## Free local Ollama setup

Ollama runs the analysis model locally and does not require an OpenAI API key, credit card, or external AI billing. On Windows:

1. Install Ollama from <https://ollama.com/download/windows>.
2. Open PowerShell and download the configured small model:

   ```powershell
   ollama pull llama3.2:3b
   ```

3. Ensure the Ollama application is running. Its local API normally listens on `http://localhost:11434`.
4. Set the following in the ignored `server/HelpDesk.Api/appsettings.Local.json`:

   ```json
   {
     "Ai": {
       "Provider": "Ollama",
       "OllamaModel": "llama3.2:3b",
       "OllamaEndpoint": "http://localhost:11434/api/chat"
     }
   }
   ```

5. Restart the backend, open a ticket detail page, and select **Analyze Ticket**. The first analysis can take longer while the model loads into memory.

The OpenAI-specific `ApiKey`, `Model`, and `Endpoint` settings are ignored when Ollama is selected. Do not expose Ollama outside the local machine without adding appropriate network access controls and authentication.

Only the ticket title and description plus active category and priority names are sent. Emails, credentials, tokens, Identity security fields, attachment content/storage data, comments, internal notes, and user details are excluded. Ticket text is delimited as untrusted data, and provider instructions explicitly prohibit following instructions embedded in it, revealing secrets, accessing systems, claiming actions, or returning chain-of-thought.

Provider output is parsed into a private structured contract. Category and priority names are mapped only against active backend lookups; unknown values become null. Summary text is trimmed to 1,000 characters. A successful provider response must contain three to five troubleshooting suggestions of at most 500 characters each; empty or malformed suggestion sets fail safely rather than presenting an incomplete analysis. The prompt asks for safe, ticket-specific diagnostics, an observable result, verification, and evidence collection or escalation where appropriate. React renders all output as ordinary text.

Failures are mapped to stable 502/503 ProblemDetails without prompts, descriptions, provider responses, keys, or stack traces. Analysis requires an explicit button click, has no polling or automatic retry, and suppresses duplicate frontend calls. Production deployments should add provider-aware rate limits and budget monitoring.

Automated tests use mocks and never call a live AI service. No AI audit event is stored because repeated advisory analysis would add low-value activity noise and could encourage retaining provider metadata unnecessarily.

Before real employee tickets are sent to any third-party provider, the organization must review provider data-processing terms, retention, privacy obligations, company data classification, regional requirements, and applicable law. Third-party processing is not automatically acceptable. Local Ollama avoids sending the prompt to an external AI API, but operators must still protect the workstation, model service, application logs, and ticket database.
