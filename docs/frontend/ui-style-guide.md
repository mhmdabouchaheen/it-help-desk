# UI style guide

The help desk uses a restrained enterprise SaaS visual language. All colors and dimensions are semantic CSS custom properties in `src/index.css`; components must consume tokens instead of copying literal colors.

## Foundations

- Backgrounds use `--color-bg`, `--color-surface`, and muted/strong surface variants.
- Text uses `--color-text` and `--color-text-muted`; borders use `--color-border`.
- State colors have paired foreground and soft-background tokens for primary, success, warning, danger, and information.
- The system font stack is used. Page titles are fluid, section headings are compact, labels are strong, and metadata is smaller and muted.
- Spacing follows the `--space-*` scale. Cards use the shared radius and shadow tokens.

## Components

Buttons retain semantic `button` or link elements. Primary actions use the filled primary treatment; secondary actions use bordered surfaces. Every interactive control has visible keyboard focus, hover, disabled, and reduced-motion behavior.

Inputs, selects, textareas, checkboxes, and files share height, border, radius, focus, error, and disabled treatments. Labels remain explicit. Errors use the danger surface and live/alert semantics already provided by the page.

Badges always include readable text. Known status and priority names map to semantic tones by name, never database ID; unknown values receive a neutral treatment. Cancellation is separate from workflow status.

Cards use white surfaces, subtle borders, restrained shadows, and consistent padding. Empty states use concise real-state copy and never fake records. Loading uses an announced compact spinner; errors expose safe detail and optional trace references only.

## Layout and responsive behavior

The desktop shell has a 17rem fixed sidebar, sticky header, and centered main content up to 90rem. At 850px the sidebar becomes an accessible drawer with a labelled menu button, overlay close control, route close behavior, and Escape handling. At 600px actions wrap, filters stack, summaries collapse, and data tables become bordered row cards. The minimum supported viewport is 320px.

Motion is limited to short interaction and drawer transitions and is disabled under `prefers-reduced-motion`. Color is never the only carrier of status, content remains normal React text, and focus outlines are never suppressed.
