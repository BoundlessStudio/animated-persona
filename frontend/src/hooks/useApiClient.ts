import axios from 'axios';
import { useAuth0 } from '@auth0/auth0-react';
import { useMemo } from 'react';

const baseURL = import.meta.env.VITE_API_BASE_URL || '/api';

export function useApiClient() {
  const { getAccessTokenSilently } = useAuth0();

  return useMemo(() => {
    const instance = axios.create({ baseURL });
    instance.interceptors.request.use(async (config) => {
      try {
        const token = await getAccessTokenSilently();
        config.headers?.set?.('Authorization', `Bearer ${token}`);
      } catch (err) {
        // Allow unauthenticated request to continue; callers can handle 401s.
        console.warn('Unable to acquire access token', err);
      }
      return config;
    });
    return instance;
  }, [getAccessTokenSilently]);
}
