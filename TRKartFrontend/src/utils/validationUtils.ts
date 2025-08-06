// Frontend validation utilities for real-time input validation
export const validationUtils = {
  // Amount validation - simplified to just check if it's a positive number
  validateAmount: (amount: string): { isValid: boolean; error?: string } => {
    if (!amount || amount.trim() === '') {
      return { isValid: false, error: 'Amount is required' };
    }

    if (!/^\d+(\.\d{1,2})?$/.test(amount)) {
      return { isValid: false, error: 'Amount must be a valid number with up to 2 decimal places' };
    }

    const numAmount = parseFloat(amount);
    if (isNaN(numAmount) || numAmount <= 0) {
      return { isValid: false, error: 'Amount must be a positive number' };
    }

    return { isValid: true };
  },

  // Transaction type validation - updated with new allowed values
  validateTransactionType: (transactionType: string): { isValid: boolean; error?: string } => {
    if (!transactionType || transactionType.trim() === '') {
      return { isValid: false, error: 'Transaction type is required' };
    }

    const allowedTypes = ['Pay', 'Load', 'Refund', 'TransferIn', 'TransferOut'];
    if (!allowedTypes.includes(transactionType)) {
      return { 
        isValid: false, 
        error: `Transaction type must be one of: ${allowedTypes.join(', ')}` 
      };
    }

    return { isValid: true };
  },

  // Description validation - only letters and numbers, no special characters
  validateDescription: (description: string): { isValid: boolean; error?: string } => {
    if (!description || description.trim() === '') {
      return { isValid: false, error: 'Description is required' };
    }

    if (description.length > 500) {
      return { isValid: false, error: 'Description cannot exceed 500 characters' };
    }

    // Only allow letters (A-Z, a-z) and numbers (0-9), no special characters
    if (!/^[a-zA-Z0-9\s]+$/.test(description)) {
      return { 
        isValid: false, 
        error: 'Description can only contain letters and numbers. No special characters allowed.' 
      };
    }

    return { isValid: true };
  },

  // Card ID validation
  validateCardId: (cardId: string): { isValid: boolean; error?: string } => {
    if (!cardId || cardId.trim() === '') {
      return { isValid: false, error: 'Card ID is required' };
    }

    if (!/^\d+$/.test(cardId)) {
      return { isValid: false, error: 'Card ID must contain only numbers' };
    }

    const numCardId = parseInt(cardId);
    if (isNaN(numCardId) || numCardId <= 0) {
      return { isValid: false, error: 'Card ID must be a positive number' };
    }

    return { isValid: true };
  },

  // Real-time input sanitization
  sanitizeAmount: (input: string): string => {
    // Only allow numbers and one decimal point
    return input.replace(/[^0-9.]/g, '').replace(/(\..*)\./g, '$1');
  },

  sanitizeTransactionType: (input: string): string => {
    // Only allow letters
    return input.replace(/[^a-zA-Z]/g, '');
  },

  sanitizeDescription: (input: string): string => {
    // Only allow letters, numbers, and spaces
    return input.replace(/[^a-zA-Z0-9\s]/g, '');
  },

  sanitizeCardId: (input: string): string => {
    // Only allow numbers
    return input.replace(/[^0-9]/g, '');
  }
}; 