import api from './api';
import { CardStatus } from '@/types/cardStatus';

export interface CreateTransferRequest {
  senderCardID: number;
  recipientCardNumber: string;
  amount: number;
}

export interface TransferResponse {
  success: boolean;
  message: string;
  transferOutTransactionId?: number;
  transferInTransactionId?: number;
  transferOutTransaction?: any;
  transferInTransaction?: any;
  error?: string;
}

export interface TransferValidationResponse {
  success: boolean;
  isValid: boolean;
  message: string;
  recipientCard?: {
    cardID: number;
    cardNumber: string;
    balance: number;
    cardStatus: CardStatus;
  };
}

class TransferService {
  async createTransfer(transferData: CreateTransferRequest): Promise<TransferResponse> {
    try {
      console.log('TransferService: Making API call to /Transfer/create');
      console.log('TransferService: Request data:', transferData);
      
      const response = await api.post<TransferResponse>('/Transfer/create', transferData, {
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      });
      
      console.log('TransferService: Response received:', response.data);
      console.log('TransferService: Response status:', response.status);
      console.log('TransferService: Response success:', response.data.success);
      
      return response.data;
    } catch (error: any) {
      console.error('TransferService: Error occurred:', error);
      console.error('TransferService: Error response:', error.response);
      console.error('TransferService: Error message:', error.message);
      
      // If the error has a response with data, use that message
      if (error.response?.data?.message) {
        throw new Error(error.response.data.message);
      }
      // If the error has a response but no message, use the status
      else if (error.response?.status) {
        throw new Error(`HTTP ${error.response.status}: ${error.response.statusText}`);
      }
      // Otherwise use the generic error message
      else {
        throw new Error(error.message || 'Transfer failed');
      }
    }
  }

  async validateRecipientCard(cardNumber: string): Promise<TransferValidationResponse> {
    try {
      const response = await api.get<TransferValidationResponse>(`/Transfer/validate-recipient/${cardNumber}`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      });
      return response.data;
    } catch (error: any) {
      throw new Error(error.response?.data?.message || 'Card validation failed');
    }
  }

  async getTransferHistory(cardID?: number): Promise<any[]> {
    try {
      const url = cardID ? `/Transfer/history/${cardID}` : '/Transfer/history';
      const response = await api.get<any[]>(url, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      });
      return response.data;
    } catch (error: any) {
      throw new Error(error.response?.data?.message || 'Failed to load transfer history');
    }
  }

  async getTransferDetails(transferTransactionID: number): Promise<any> {
    try {
      const response = await api.get<any>(`/Transfer/details/${transferTransactionID}`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      });
      return response.data;
    } catch (error: any) {
      throw new Error(error.response?.data?.message || 'Failed to load transfer details');
    }
  }
}

export default new TransferService(); 