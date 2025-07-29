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