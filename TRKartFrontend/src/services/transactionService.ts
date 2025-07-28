import api from './api';
import { Transaction, CreateTransactionRequest } from '@/types';

class TransactionService {
  async getTransactionsByCardNumber(cardNumber: string): Promise<Transaction[]> {
    const response = await api.get<Transaction[]>(`/Transaction/by-card/${cardNumber}`);
    return response.data;
  }

  async getTransactionsByCustomerId(customerId: number): Promise<Transaction[]> {
    const response = await api.get<Transaction[]>(`/Transaction/by-customer/${customerId}`);
    return response.data;
  }

  async createTransaction(transactionData: CreateTransactionRequest): Promise<Transaction> {
    const response = await api.post<Transaction>('/Transaction', transactionData);
    return response.data;
  }
}

export default new TransactionService(); 