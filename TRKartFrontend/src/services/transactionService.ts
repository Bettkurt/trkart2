import api from './api';
import { 
  CreateTransactionRequest, 
  Transaction, 
  TransactionResponse, 
  FeasibilityCheckResponse, 
  CardBalanceResponse,
  CardExistsResponse
} from '@/types';

class TransactionService {
  // New secure user-specific methods
  async getUserTransactions(): Promise<Transaction[]> {
    const response = await api.get<{ success: boolean; transactions: Transaction[] }>('/SecureTransaction/user/transactions');
    return response.data.transactions;
  }

  async getUserCards(): Promise<any[]> {
    const response = await api.get<{ success: boolean; cards: any[] }>('/SecureTransaction/user/cards');
    return response.data.cards;
  }

  async createUserTransaction(transactionData: CreateTransactionRequest): Promise<TransactionResponse> {
    console.log('Making API call to create transaction:', transactionData);
    const response = await api.post<TransactionResponse>('/SecureTransaction/user/transaction', transactionData, {
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${localStorage.getItem('token')}`
      }
    });
    console.log('API response:', response);
    return response.data;
  }

  async getUserCardBalance(cardId: number): Promise<CardBalanceResponse> {
    const response = await api.get<CardBalanceResponse>(`/SecureTransaction/user/card/${cardId}/balance`);
    return response.data;
  }

  async getUserCardTransactions(cardId: number): Promise<Transaction[]> {
    const response = await api.get<{ success: boolean; transactions: Transaction[] }>(`/SecureTransaction/user/card/${cardId}/transactions`);
    return response.data.transactions;
  }

  async checkUserTransactionFeasibility(transactionData: CreateTransactionRequest): Promise<FeasibilityCheckResponse> {
    const response = await api.post<FeasibilityCheckResponse>('/SecureTransaction/user/check-feasibility', transactionData);
    return response.data;
  }

  // Existing methods for backward compatibility
  async getTransactionsByCardNumber(cardNumber: string): Promise<Transaction[]> {
    const response = await api.get<Transaction[]>(`/Transaction/by-card/${cardNumber}`);
    return response.data;
  }

  async getTransactionsByCustomerId(customerId: number): Promise<Transaction[]> {
    const response = await api.get<Transaction[]>(`/Transaction/by-customer/${customerId}`);
    return response.data;
  }

  async createTransaction(transactionData: CreateTransactionRequest): Promise<TransactionResponse> {
    const response = await api.post<TransactionResponse>('/Transaction', transactionData);
    return response.data;
  }

  async checkTransactionFeasibility(transactionData: CreateTransactionRequest): Promise<FeasibilityCheckResponse> {
    const response = await api.post<FeasibilityCheckResponse>('/Transaction/check-feasibility', transactionData);
    return response.data;
  }

  async getCardBalance(cardId: number): Promise<CardBalanceResponse> {
    const response = await api.get<CardBalanceResponse>(`/Transaction/balance/${cardId}`);
    return response.data;
  }

  // Simplified validation methods
  async validateAmount(amount: string): Promise<{ success: boolean; isValid: boolean; message: string }> {
    const response = await api.get(`/Transaction/validate-amount?value=${encodeURIComponent(amount)}`);
    return response.data;
  }

  async checkCardExists(cardId: number): Promise<CardExistsResponse> {
    const response = await api.get<CardExistsResponse>(`/Transaction/check-card-exists/${cardId}`);
    return response.data;
  }
}

export default new TransactionService(); 