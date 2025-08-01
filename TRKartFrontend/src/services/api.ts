import axios, { AxiosInstance, AxiosResponse, AxiosError } from 'axios';

// Create axios instance
const api: AxiosInstance = axios.create({
  baseURL: (import.meta as any).env?.VITE_API_BASE_URL || 'http://localhost:7037/api',
  timeout: 10000,
  withCredentials: true, // Include cookies in requests
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor - no need to add headers for session token
// Session token is automatically sent with cookies
api.interceptors.request.use(
  (config) => {
    // Session token is automatically included in cookies
    // No need to manually add Authorization header
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor for error handling
api.interceptors.response.use(
  (response: AxiosResponse) => {
    return response;
  },
  (error: AxiosError) => {
    if (error.response?.status === 401) {
      // Session expired or invalid
      localStorage.removeItem('user');
      // Don't redirect automatically, let the component handle it
      console.log('Session expired or invalid');
    }
    return Promise.reject(error);
  }
);

export default api; 