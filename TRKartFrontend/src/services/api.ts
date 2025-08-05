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

// Flag to prevent infinite refresh loops
let isRefreshing = false;
// Store pending requests to retry after token refresh
let failedQueue: { resolve: Function; reject: Function }[] = [];

// Process the failed queue
const processQueue = (error: any | null, token: string | null = null) => {
  failedQueue.forEach(promise => {
    if (error) {
      promise.reject(error);
    } else {
      promise.resolve(token);
    }
  });

  failedQueue = [];
};

// Request interceptor to add the access token to requests
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    // Skip token for auth endpoints
    const isAuthEndpoint = 
      config.url?.includes('/Auth/login') || 
      config.url?.includes('/Auth/register') || 
      config.url?.includes('/Token/refresh');

    if (isAuthEndpoint) {
      return config;
    }

    // Get the access token from session storage
    const accessToken = sessionService.getAccessToken();

    // Add the token to the request header if it exists
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
    const originalRequest = error.config as any;

    // Handle 401 Unauthorized errors that are not from a retry
    if (error.response?.status === 401 && !originalRequest._retry) {
      // If we're already refreshing the token, add this request to the queue
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            originalRequest.headers.Authorization = `Bearer ${token}`;
            return axios(originalRequest);
          })
          .catch((err) => {
            return Promise.reject(err);
          });
      }

      // Mark request as retry to prevent infinite loop
      originalRequest._retry = true;

      // Check if refresh token exists and is not expired
      const refreshToken = sessionService.getRefreshToken();
      if (!refreshToken || sessionService.isRefreshTokenExpired()) {
        // Clear session and redirect to login
        sessionService.clearSession();
        localStorage.removeItem('user');
        window.location.href = '/login';
        return Promise.reject(error);
      }

      // Refresh the token
      isRefreshing = true;

      try {
        const response = await axios.post<{
          accessToken: string;
          refreshToken: string;
          accessTokenExpiration: string;
          refreshTokenExpiration: string;
        }>(
          `${api.defaults.baseURL}/Auth/refresh-token`,
          { refreshToken },
          { withCredentials: true }
        );

        if (!response.data) {
          throw new Error('No data in refresh token response');
        }

        // Update tokens in session
        sessionService.setUserSession({
          accessToken: response.data.accessToken,
          refreshToken: response.data.refreshToken,
          accessTokenExpiration: response.data.accessTokenExpiration,
          refreshTokenExpiration: response.data.refreshTokenExpiration,
          email: sessionService.getSessionData('userEmail') || ''
        });

        // Update authorization header
        api.defaults.headers.common['Authorization'] = `Bearer ${response.data.accessToken}`;
        originalRequest.headers.Authorization = `Bearer ${response.data.accessToken}`;

        // Process other requests in the queue
        processQueue(null, response.data.accessToken);

        // Retry the original request
        return axios(originalRequest);
      } catch (refreshError) {
        // Process queue with error
        processQueue(refreshError, null);
        // Clear session and redirect to login
        sessionService.clearSession();
        localStorage.removeItem('user');
        window.location.href = '/login';
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }
    
    return Promise.reject(error);
  }
);

export default api; 