import { 
  TopUpRequest, 
  TopUpResponse, 
  TopUpValidationResponse, 
  TopUpListResponse, 
  TopUpSimulationResponse 
} from '../types';

const API_BASE_URL = 'http://localhost:7037';

// Helper function for API calls
async function apiCall<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    ...options,
    credentials: 'include', // Include cookies for authentication
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
  });

  if (!response.ok) {
    const errorData = await response.json().catch(() => null);
    throw new Error(errorData?.message || `HTTP ${response.status}: ${response.statusText}`);
  }

  return response.json();
}

export const topUpService = {
  /**
   * Process a top-up request
   */
  async topUp(request: TopUpRequest): Promise<TopUpResponse> {
    try {
      const response = await apiCall<TopUpResponse>('/api/securetransaction/topup', {
        method: 'POST',
        body: JSON.stringify(request),
      });
      return response;
    } catch (error) {
      console.error('TopUp service error:', error);
      throw error;
    }
  },

  /**
   * Validate a top-up request without processing
   */
  async validateTopUp(request: TopUpRequest): Promise<TopUpValidationResponse> {
    try {
      const response = await apiCall<TopUpValidationResponse>('/api/securetransaction/topup/validate', {
        method: 'POST',
        body: JSON.stringify(request),
      });
      return response;
    } catch (error) {
      console.error('TopUp validation service error:', error);
      throw error;
    }
  },

  /**
   * Get user's top-up transactions
   */
  async getUserTopUps(pageSize: number = 50, pageNumber: number = 1): Promise<TopUpListResponse> {
    try {
      const response = await apiCall<TopUpListResponse>(
        `/api/securetransaction/topups?pageSize=${pageSize}&pageNumber=${pageNumber}`
      );
      return response;
    } catch (error) {
      console.error('Get user top-ups service error:', error);
      throw error;
    }
  },

  /**
   * Simulate approval for a top-up transaction (development only)
   */
  async simulateApproval(transactionId: number): Promise<TopUpSimulationResponse> {
    try {
      const response = await apiCall<TopUpSimulationResponse>(
        `/api/securetransaction/topup/${transactionId}/simulate-approval`,
        {
          method: 'POST',
        }
      );
      return response;
    } catch (error) {
      console.error('TopUp simulation service error:', error);
      throw error;
    }
  },

  /**
   * Check if a card number is valid and active for top-up
   */
  async checkCardForTopUp(cardNumber: string): Promise<{ isValid: boolean; message: string; cardInfo?: any }> {
    try {
      // Create a minimal validation request
      const validationRequest: TopUpRequest = {
        targetCardNumber: cardNumber,
        amount: 10, // Minimum amount for validation
        paymentMethod: 'CreditCard'
      };

      const response = await this.validateTopUp(validationRequest);
      
      if (response.success && response.validation) {
        return {
          isValid: response.validation.isValid && !response.validation.message.includes('Amount'),
          message: response.validation.message,
          cardInfo: {
            cardNumber: response.validation.cardNumber,
            cardStatus: response.validation.cardStatus,
            currentBalance: response.validation.currentBalance
          }
        };
      }

      return {
        isValid: false,
        message: 'Card validation failed'
      };
    } catch (error) {
      console.error('Card check service error:', error);
      return {
        isValid: false,
        message: 'Error checking card validity'
      };
    }
  },

  /**
   * Get available payment methods
   */
  getPaymentMethods(): { value: string; label: string }[] {
    return [
      { value: 'CreditCard', label: 'Credit Card' },
      { value: 'Wire', label: 'Wire Transfer' },
      { value: 'Cash', label: 'Cash' },
      { value: 'BankTransfer', label: 'Bank Transfer' },
      { value: 'PayPal', label: 'PayPal' },
      { value: 'Stripe', label: 'Stripe' }
    ];
  },

  /**
   * Calculate net amount after fees
   */
  calculateNetAmount(amount: number, feeAmount?: number): number {
    return amount - (feeAmount || 0);
  },

  /**
   * Validate amount within limits
   */
  validateAmount(amount: number): { isValid: boolean; message: string } {
    const min = 10;
    const max = 10000;

    if (amount < min) {
      return { isValid: false, message: `Amount must be at least $${min}` };
    }

    if (amount > max) {
      return { isValid: false, message: `Amount cannot exceed $${max}` };
    }

    return { isValid: true, message: 'Amount is valid' };
  },

  /**
   * Validate fee amount
   */
  validateFeeAmount(amount: number, feeAmount?: number): { isValid: boolean; message: string } {
    if (!feeAmount) {
      return { isValid: true, message: 'No fee specified' };
    }

    if (feeAmount < 0) {
      return { isValid: false, message: 'Fee amount cannot be negative' };
    }

    if (feeAmount >= amount) {
      return { isValid: false, message: 'Fee amount cannot be equal to or greater than the top-up amount' };
    }

    return { isValid: true, message: 'Fee amount is valid' };
  },

  /**
   * Format currency for display
   */
  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  },

  /**
   * Generate a unique external reference
   */
  generateExternalRef(): string {
    // Use crypto.randomUUID if available, fallback to enhanced timestamp + random
    if (typeof crypto !== 'undefined' && crypto.randomUUID) {
      return `TOPUP_${crypto.randomUUID().replace(/-/g, '')}`.toUpperCase();
    }
    
    // Enhanced uniqueness with performance.now() for microsecond precision
    const timestamp = Date.now();
    const performanceNow = Math.floor(performance.now() * 10000); // 0.1ms precision
    const random1 = Math.random().toString(36).substring(2, 10);
    const random2 = Math.random().toString(36).substring(2, 6);
    return `TOPUP_${timestamp}_${performanceNow}_${random1}${random2}`.toUpperCase();
  }
};
