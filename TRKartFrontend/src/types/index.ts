// Auth Types
export interface LoginRequest {
  email: string;
  password: string;
  rememberMe: boolean;
}

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
}

export interface AuthResponse {
  message: string;
  token: string;
}

export interface User {
  customerID: number;
  email: string;
  fullName: string;
}

// User Card Types
export interface UserCard {
  cardID: number;
  customerID: number;
  cardNumber: string;
  balance: number;
  cardStatus: string;
  createdAt: string;
}

export interface CreateUserCardRequest {
  customerID: number;
}

export interface DeleteUserCardRequest {
  cardNumber: string;
}

// Transaction Types
export interface Transaction {
  transactionID: number;
  cardID: number;
  cardNumber: string;
  amount: number;
  transactionType: string;
  description?: string;
  transactionDate: string;
  transactionStatus?: string;
}

export interface CreateTransactionRequest {
  cardID: number;
  amount: number;
  transactionType: string;
  description: string;
}

// API Response Types
export interface ApiResponse<T> {
  data: T;
  message?: string;
  success: boolean;
}

export interface PaginatedResponse<T> {
  data: T[];
  total: number;
  page: number;
  pageSize: number;
} 