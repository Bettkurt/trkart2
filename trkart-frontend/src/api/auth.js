import axios from 'axios';

// Use HTTP to avoid SSL issues during development
const API_BASE_URL = 'https://localhost:7037';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  }
});

export const register = async (userData) => {
  try {
    console.log('🚀 Making request to:', `${API_BASE_URL}/api/auth/register`);
    console.log('📤 Sending data:', userData);
    
    const response = await apiClient.post('/api/auth/register', userData);
    
    console.log('✅ Success:', response.data);
    return response;
  } catch (error) {
    console.error('❌ API Error:', {
      message: error.message,
      code: error.code,
      response: error.response?.data,
      status: error.response?.status
    });
    throw error;
  }
};

export const login = async (userData) => {
  try {
    const response = await apiClient.post('/api/auth/login', userData);
    return response;
  } catch (error) {
    console.error('Login API Error:', error);
    throw error;
  }
};









