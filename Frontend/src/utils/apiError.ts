export const FORBIDDEN_MESSAGE = 'Je hebt geen toegang tot deze actie';

/**
 * The backend's `message` if it sent one. A 403 without a message falls back to
 * FORBIDDEN_MESSAGE; anything else falls back to the caller's text.
 * Reads the message only, never the request or the rest of the response.
 */
export function getApiErrorMessage(error: any, fallback: string): string {
  const message = error?.response?.data?.message;
  if (typeof message === 'string' && message.trim()) return message;
  if (error?.response?.status === 403) return FORBIDDEN_MESSAGE;
  return fallback;
}
