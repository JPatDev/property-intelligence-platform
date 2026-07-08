const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5120';

export type ApiHealth = {
  status: string;
};

export async function getHealth(signal?: AbortSignal): Promise<ApiHealth> {
  const response = await fetch(`${apiBaseUrl}/health`, { signal });

  if (!response.ok) {
    throw new Error(`Health check failed with status ${response.status}`);
  }

  return response.json() as Promise<ApiHealth>;
}

export async function getModules(signal?: AbortSignal): Promise<string[]> {
  const response = await fetch(`${apiBaseUrl}/api/modules`, { signal });

  if (!response.ok) {
    throw new Error(`Module discovery failed with status ${response.status}`);
  }

  return response.json() as Promise<string[]>;
}

export const platformApi = {
  baseUrl: apiBaseUrl,
  swaggerUrl: `${apiBaseUrl}/swagger`,
};
