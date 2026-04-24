const BASE_URL = import.meta.env.VITE_API_BASE_URL

export function createEventSource(path: string): EventSource {
  return new EventSource(`${BASE_URL}${path}`, { withCredentials: true })
}
