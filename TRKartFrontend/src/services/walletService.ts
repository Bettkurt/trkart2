import { AxiosError, AxiosResponse } from 'axios';
import { api } from './api';
import { 
  WalletDto, 
  WalletTransactionDto, 
  CreateWalletTransactionDto, 
  WalletBalanceDto,
  WalletTransactionResponse,
  TransactionType
} from '@/types/wallet';

// Custom error type that includes the response
type ApiError = Error & {
  response?: AxiosResponse;
};

// Add request interceptor to include auth token
api.interceptors.request.use(
  (config: any) => {
    // Get the access token from auth service
    const accessToken = localStorage.getItem('accessToken');
    if (accessToken) {
      config.headers.Authorization = `Bearer ${accessToken}`;
    }
    return config;
  },
  (error: any) => {
    return Promise.reject(error instanceof Error ? error : new Error(String(error)));
  }
);

export const walletService = {
  // Get wallet by customer ID (primary key)
  async getWalletByCustomerId(customerId: number): Promise<WalletDto> {
    try {
      const response = await api.get<WalletDto>(`/wallet/customer/${customerId}`);
      return response.data;
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || 'Failed to load wallet';
      console.error('Error in getWalletByCustomerId:', {
        error: errorMessage,
        status: error.response?.status,
        customerId,
        response: error.response?.data
      });
      
      if (error.response?.status === 400) {
        // If it's a 400 error, try to get the first available wallet
        console.log('Attempting to get first available wallet...');
        try {
          const allWallets = await api.get<WalletDto[]>('/wallet');
          if (allWallets.data?.length > 0) {
            console.log('Using first available wallet:', allWallets.data[0]);
            return allWallets.data[0];
          }
        } catch (e) {
          console.error('Error fetching all wallets:', e);
        }
      }
      
      throw new Error(errorMessage);
    }
  },

  // Load money into wallet
  async loadWallet(transaction: Omit<CreateWalletTransactionDto, 'walletId'> & { walletId: number }): Promise<WalletTransactionResponse> {
    console.log('Attempting to load wallet with transaction:', {
      WalletId: transaction.walletId,
      Amount: transaction.amount,
      CardId: transaction.cardId,
      ReferenceId: transaction.referenceId
    });

    try {
      // Ensure we're using PascalCase property names to match the backend DTO
      const requestData = {
        WalletId: transaction.walletId,
        CardId: transaction.cardId,
        Amount: transaction.amount,
        TransactionType: TransactionType.Load,
        Description: transaction.description || 'Wallet load',
        ReferenceId: transaction.referenceId || `WALLET_LOAD_${Date.now()}`,
        Status: 'Pending' // Matches TransactionStatus.Pending
      };
      
      console.log('Sending wallet load request with data:', JSON.stringify(requestData, null, 2));
      
      const response = await api.post<{ 
        success: boolean;
        data: WalletTransactionResponse;
        message: string;
      }>('/Wallet/load', requestData, {
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json'
        },
        validateStatus: (status) => status < 500 // Don't throw for 4xx errors
      });
      
      console.log('Wallet load response:', response.data);
      
      if (response.status >= 400 || !response.data.success) {
        const errorMessage = response.data?.message || 'Failed to process wallet load';
        const error: ApiError = new Error(errorMessage);
        error.response = response;
        throw error;
      }
      
      return response.data.data;
    } catch (error: any) {
      console.error('Raw error in loadWallet:', error);
      
      // Extract error details from response
      const responseData = error.response?.data;
      const errorMessage = responseData?.Message || 
                         responseData?.message || 
                         (typeof responseData === 'string' ? responseData : null) || 
                         error.message || 
                         'Failed to load wallet';
      
      const errorDetails = {
        error: errorMessage,
        status: error.response?.status,
        statusText: error.response?.statusText,
        request: {
          url: error.config?.url,
          method: error.config?.method,
          data: error.config?.data,
          headers: error.config?.headers
        },
        response: responseData,
        fullError: process.env.NODE_ENV === 'development' ? error : undefined
      };
      
      console.error('Error details in loadWallet:', errorDetails);
      
      // Handle specific error cases
      if (error.response?.status === 400) {
        throw new Error(`Invalid request: ${errorMessage}`);
      } else if (error.response?.status === 404) {
        throw new Error(`Wallet or card not found: ${errorMessage}`);
      } else if (error.response?.status === 401 || error.response?.status === 403) {
        throw new Error('Authentication required. Please log in again.');
      } else {
        throw new Error(`Failed to load wallet: ${errorMessage}`);
      }
    }
  },

  // Pay from wallet
  async payFromWallet(transaction: Omit<CreateWalletTransactionDto, 'walletId'> & { walletId: number }): Promise<WalletTransactionResponse> {
    try {
      // Log the raw input first
      console.log('Raw transaction input:', JSON.stringify(transaction, null, 2));
      
      // Ensure amount is properly formatted as a number
      const amount = typeof transaction.amount === 'string' 
        ? parseFloat(transaction.amount) 
        : Number(transaction.amount);

      if (isNaN(amount)) {
        throw new Error('Invalid amount. Please enter a valid number.');
      }

      // Ensure walletId is a number
      const walletId = Number(transaction.walletId);
      if (isNaN(walletId)) {
        throw new Error('Invalid wallet ID');
      }

      // Use PascalCase property names to match backend DTO
      // For wallet payments, we should not include CardId as it's a wallet-to-wallet transaction
      const payload = {
        WalletId: walletId,
        Amount: amount,
        TransactionType: 'WalletPay', // Use the string value directly to match backend
        Description: transaction.description || 'Wallet payment',
        ReferenceId: transaction.referenceId || `WALLET_PAY_${Date.now()}`,
        // Remove CardId for wallet payments as per the database constraint
        // CardId: null, // Explicitly set to null to ensure it's not included
        Status: 'Pending' // Matches TransactionStatus.Pending
      };
      
      console.log('Payment payload after cleanup:', JSON.stringify(payload, null, 2));
      
      console.log('Sending payment request to /Wallet/pay with payload:', JSON.stringify(payload, null, 2));
      
      const response = await api<{
        success: boolean;
        data: WalletTransactionResponse;
        message: string;
      }>({
        method: 'post',
        url: '/wallet/pay',
        data: payload,
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json'
        },
        validateStatus: (status) => status < 500 // Don't throw for 4xx errors
      });
      
      console.log('Payment response:', response.data);
      
      if (response.status >= 400 || !response.data?.success) {
        // Log the full response for debugging
        console.log('Payment failed with response:', JSON.stringify(response.data, null, 2));
        
        // Try to get a meaningful error message from the response
        const errorMessage = response.data?.message || 
                           response.statusText || 
                           'Payment failed';
        
        const error: ApiError = new Error(errorMessage);
        error.response = response;
        throw error;
      }
      
      return response.data.data;
    } catch (error: unknown) {
      const axiosError = error as AxiosError;
      const responseData = axiosError.response?.data as any;
      
      // Extract validation errors if they exist
      const validationErrors = responseData?.errors;
      let errorMessage = 'Payment failed';
      
      if (validationErrors) {
        // Format validation errors into a readable message
        const errorMessages = Object.entries(validationErrors)
          .map(([field, errors]) => `${field}: ${Array.isArray(errors) ? errors.join(', ') : errors}`)
          .join('; ');
        errorMessage = `Validation failed: ${errorMessages}`;
      } else if (responseData) {
        errorMessage = responseData.title || 
                      responseData.Message || 
                      responseData.message || 
                      'Payment failed';
      } else if (error instanceof Error) {
        errorMessage = error.message;
      }
      
      console.error('Error in payFromWallet:', {
        error: errorMessage,
        status: axiosError.response?.status,
        request: {
          url: axiosError.config?.url,
          method: axiosError.config?.method,
          data: axiosError.config?.data,
          headers: axiosError.config?.headers
        },
        response: responseData,
        validationErrors: validationErrors || 'No validation errors in response'
      });
      
      throw new Error(errorMessage);
    }
  },

  // Get wallet balance
  async getWalletBalance(walletId: number): Promise<number> {
    try {
      const response = await api.get<WalletBalanceDto>(`/wallet/${walletId}/balance`);
      return response.data.balance;
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || 'Failed to get wallet balance';
      console.error('Error in getWalletBalance:', {
        error: errorMessage,
        status: error.response?.status,
        walletId,
        response: error.response?.data
      });
      throw new Error(errorMessage);
    }
  },

  // Get wallet transactions
  async getWalletTransactions(walletId: number): Promise<WalletTransactionDto[]> {
    try {
      const response = await api.get<WalletTransactionDto[]>(`/wallet/${walletId}/transactions`);
      return response.data;
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || 'Failed to get wallet transactions';
      console.error('Error in getWalletTransactions:', {
        error: errorMessage,
        status: error.response?.status,
        walletId,
        response: error.response?.data
      });
      // Return empty array if no transactions found
      if (error.response?.status === 404) {
        return [];
      }
      throw new Error(errorMessage);
    }
  },

  // Format currency
  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: 'TRY',
      minimumFractionDigits: 2,
    }).format(amount);
  },

  // Format date
  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  },

  // Get transaction sign class
  getTransactionSignClass(transactionType: TransactionType | string | number | any): string {
    // If we receive the full transaction object
    if (transactionType && typeof transactionType === 'object') {
      const tx = transactionType as any;
      // Check for isWalletLoad flag or TRANSFERIN type
      if (tx.isWalletLoad === true || (tx.type && tx.type.toUpperCase() === 'TRANSFERIN')) {
        return 'text-green-600';
      }
      return 'text-red-600';
    }
    
    // Handle string/enum values for backward compatibility
    const typeStr = String(transactionType).toLowerCase();
    
    // Check for load/credit transactions (should be green)
    const isLoad = typeStr === 'walletload' || 
                  typeStr === 'load' ||
                  typeStr === '0' || // For numeric enum (Load = 0)
                  typeStr === 'transferin';
    
    return isLoad ? 'text-green-600' : 'text-red-600';
  },

  // Get transaction sign
  getTransactionSign(transactionType: TransactionType | string | number | any): string {
    // If we receive the full transaction object
    if (transactionType && typeof transactionType === 'object') {
      const tx = transactionType as any;
      // Only show sign for outgoing transactions
      return (tx.isWalletLoad === false || (tx.type && tx.type.toUpperCase() === 'TRANSFEROUT')) ? '-' : '';
    }
    
    // Handle string/enum values for backward compatibility
    const typeStr = String(transactionType).toLowerCase();
      
    // Only show sign for Pay transactions (negative)
    return (typeStr === 'walletpay' || typeStr === 'pay' || typeStr === '1' || typeStr === 'transferout') 
      ? '' 
      : '';
  },
};
