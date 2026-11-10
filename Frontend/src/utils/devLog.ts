/**
 * Logging rule for API failures: at most a status line, and only in __DEV__.
 * Never log axios error/response objects (their config carries the Authorization
 * header and the request body), tokens, passwords or user data.
 */
export function logApiError(context: string, error: any): void {
  if (__DEV__) {
    console.log(`${context}: ${error?.response?.status ?? 'no response'}`);
  }
}
