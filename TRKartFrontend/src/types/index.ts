// Auth types
export interface LoginRequest {
  email: string;
  password: string;
  rememberMe: boolean;
}

export interface SessionCheckResponse {
  hasValidSession: boolean;
  email: string | null;
}

export interface AuthResponse {
  message: string;
  token: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
}

// User types
export interface User {
  customerID: number;
  email: string;
  fullName: string;
}

// UserCard types
export interface UserCard {
  cardID: number;
  customerID: number;
  cardNumber: string;
  balance: number;
  status: string;
  createdAt: string;
}

export interface CreateUserCardRequest {
  customerID: number;
  cardNumber: string;
  balance: number;
}

export interface DeleteUserCardRequest {
  cardID: number;
}

// Transaction types
export interface Transaction {
  transactionID: number;
  cardID: number;
  cardNumber: string;
  amount: number;
  transactionType: string;
  description?: string;
  transactionDate: string;
  createdAt: string;
}

export interface CreateTransactionRequest {
  cardID: number;
  amount: number;
  transactionType: string;
  description?: string;
}

export interface TransactionFeasibilityResponse {
  isFeasible: boolean;
  currentBalance: number;
  projectedBalance: number;
  message: string;
  cardNumber: string;
}

export interface TransactionResponse {
  success: boolean;
  message: string;
  transaction?: Transaction;
  error?: string;
}

export interface FeasibilityCheckResponse {
  success: boolean;
  feasibility: TransactionFeasibilityResponse;
}

export interface CardBalanceResponse {
  success: boolean;
  cardId: number;
  balance: number;
}

export interface CardExistsResponse {
  success: boolean;
  exists: boolean;
  message: string;
  card?: {
    cardID: number;
    cardNumber: string;
    balance: number;
    cardStatus: string;
    customerID: number;
  };
}

// Input Validation types
export interface ValidationError {
  field: string;
  error: string;
  value: string;
}

export interface InputValidationResponse {
  isValid: boolean;
  errors: ValidationError[];
  message: string;
}

export interface InputValidationApiResponse {
  success: boolean;
  validation: InputValidationResponse;
}

// API Response types
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
  totalPages: number;
}