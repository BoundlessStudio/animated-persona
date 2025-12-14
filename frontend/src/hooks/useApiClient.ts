import axios, { type AxiosRequestHeaders } from 'axios';
import { useAuth0 } from '@auth0/auth0-react';
import { useMemo } from 'react';

const baseURL = import.meta.env.VITE_API_BASE_URL || '/api';

export function useApiClient() {
  const { getAccessTokenSilently } = useAuth0();

  return useMemo(() => {
    const instance = axios.create({ baseURL });
    instance.interceptors.request.use(async (config) => {
      const token = await getAccessTokenSilently();
      const headers = (config.headers ?? {}) as AxiosRequestHeaders;
      headers.Authorization = `Bearer ${token}`;
      config.headers = headers;
      return config;
    });
    return instance;
  }, [getAccessTokenSilently]);
}
