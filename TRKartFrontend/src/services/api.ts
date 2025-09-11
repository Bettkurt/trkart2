import axios, { AxiosInstance, AxiosResponse, AxiosError, InternalAxiosRequestConfig } from 'axios';

// Create axios instance with credentials
export const api: AxiosInstance = axios.create({
  baseURL: (import.meta as any).env?.VITE_API_BASE_URL || 'http://localhost:7037/api',
  timeout: 10000,
  withCredentials: true, // Include cookies in requests
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
  },
});

// Store pending requests to retry after token refresh
let failedQueue: { resolve: Function; reject: Function }[] = [];

// Process the failed queue
const processQueue = (error: any | null) => {
  failedQueue.forEach(promise => {
    if (error) {
      promise.reject(error);
    } else {
      promise.resolve();
    }
  });

  failedQueue = [];
};

// Request interceptor to handle requests
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    // Add any custom headers here if needed
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
    const originalRequest = error.config as any;

    // Handle 401 Unauthorized errors that are not from a retry
    if (error.response?.status === 401 && !originalRequest._retry) {
      // Mark request as retry to prevent infinite loop
      originalRequest._retry = true;

      try {
        // Try to refresh the token
        await axios.post('/Auth/refresh-token', {}, { 
          withCredentials: true,
          baseURL: (import.meta as any).env?.VITE_API_BASE_URL || 'http://localhost:7037/api'
        });

        // Process any queued requests
        processQueue(null);

        // Retry the original request
        return api(originalRequest);
      } catch (refreshError) {
        // If refresh fails, clear the queue and redirect to login
        processQueue(refreshError);
        localStorage.removeItem('user');
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }
    
    return Promise.reject(error);
  }
);

export default api; 