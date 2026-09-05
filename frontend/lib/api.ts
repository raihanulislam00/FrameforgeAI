const API = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000';

export async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = typeof window !== 'undefined' ? localStorage.getItem('accessToken') : null;
  const response = await fetch(`${API}/api${path}`, { ...options, headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}), ...options.headers } });
  const body = await response.json();
  if (!response.ok || body.success === false) throw new Error(body.message ?? 'Request failed');
  return body.data as T;
}

export type User = { id: string; name: string; email: string };
export type Stats = { totalVideos: number; completedVideos: number; processingVideos: number; failedVideos: number };
export type Video = { id: string; title: string; status: string; progress: number; thumbnailUrl?: string; videoUrl?: string; duration: number; createdAt: string };
export type Plan = { title: string; targetAudience: string; duration: number; style: string; hook: string; scenes: { sceneNumber: number; duration: number; visual: string; narration: string; textOverlay: string }[]; music: { style: string; mood: string }; cta: string; emotionalTone?: string; keywords?: string[]; thumbnailConcept?: string };
