import { HttpErrorResponse } from '@angular/common/http';

// The backend returns errors two different ways: ASP.NET's ProblemDetails
// JSON ({ title, detail, status }) from Problem(...) results, or a plain
// text body from BadRequest("some message"). Handle both rather than
// assuming one shape everywhere.
export function extractErrorMessage(err: unknown, fallback = 'Something went wrong. Please try again.'): string {
  if (!(err instanceof HttpErrorResponse)) {
    return fallback;
  }

  const body = err.error;

  if (typeof body === 'string' && body.trim().length > 0) {
    return body;
  }

  if (body && typeof body === 'object' && typeof body.detail === 'string') {
    return body.detail;
  }

  if (err.status === 0) {
    return 'Could not reach the server. Is the API running?';
  }

  return fallback;
}
