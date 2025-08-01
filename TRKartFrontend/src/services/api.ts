import axios, { AxiosInstance, AxiosResponse, AxiosError, InternalAxiosRequestConfig } from 'axios';
import sessionService from './sessionService';

// Create axios instance with credentials
const api: AxiosInstance = axios.create({
  baseURL: (import.meta as any).env?.VITE_API_BASE_URL || 'https://localhost:7037/api',
  timeout: 10000,
  withCredentials: true, // This is important for sending/receiving cookies
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
  },
});

// Request interceptor to add JWT token
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
      // Don't clear rememberMe and email from localStorage
      // Just redirect to login page
      window.location.href = '/login';
    }
    
    return Promise.reject(error);
  }
);

export default api; 