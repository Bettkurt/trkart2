import axios, { AxiosInstance, AxiosResponse, AxiosError, InternalAxiosRequestConfig } from 'axios';
import sessionService from './sessionService';

// Create axios instance with credentials
const api: AxiosInstance = axios.create({
  baseURL: (import.meta as any).env?.VITE_API_BASE_URL || 'http://localhost:7037/api',
  timeout: 10000,
  withCredentials: true, // Include cookies in requests
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
  },
});

// Request interceptor - no need to add headers for session token
// Session token is automatically sent with cookies
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    // Get the access token from session storage
    const accessToken = sessionService.getAccessToken();
    
    // If we have an access token, add it to the Authorization header
    if (accessToken) {
      config.headers.Authorization = `Bearer ${accessToken}`;
    }
    
    return config;
  },
  (error: any) => {
    return Promise.reject(error);
  }
);

// Response interceptor for error handling
api.interceptors.response.use(
  (response: AxiosResponse) => {
    return response;
  },
  async (error: AxiosError) => {
    // Handle 401 Unauthorized errors
    if (error.response?.status === 401) {
      // Session expired or invalid
      localStorage.removeItem('user');
      // Don't redirect automatically, let the component handle it
      console.log('Session expired or invalid');
      window.location.href = '/login';
    }
    
    return Promise.reject(error);
  }
);

export default api; 