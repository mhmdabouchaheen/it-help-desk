export function formatAverageResolution(minutes: number | null | undefined) {
  if (minutes == null || !Number.isFinite(minutes) || minutes < 0) return "N/A";
  if (minutes < 60) return `${Math.round(minutes)}m`;
  if (minutes < 1440) return `${Math.floor(minutes / 60)}h ${Math.round(minutes % 60)}m`;
  return `${Math.floor(minutes / 1440)}d ${Math.floor((minutes % 1440) / 60)}h`;
}