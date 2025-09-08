import { TransactionType, getTransactionTypeName, getUserCreatableTransactionTypes } from '../types/TransactionType';
import { UserCard } from '../types';
import { CardStatus } from '../types/cardStatus';

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

  // Transaction type validation - only accepts TransactionType enum
  validateTransactionType: (transactionType: TransactionType): { isValid: boolean; error?: string } => {
    // Only allow transaction types that users can create
    const allowedTypes = getUserCreatableTransactionTypes();
    if (!allowedTypes.includes(transactionType)) {
      const allowedNames = allowedTypes.map(type => getTransactionTypeName(type));
      return { 
        isValid: false, 
        error: `Transaction type must be one of: ${allowedNames.join(', ')}` 
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
  },

  // Transfer validation - check if sender and recipient are the same card
  validateTransferSelfTransfer: (senderCardID: string, recipientCardNumber: string, userCards: UserCard[]): { isValid: boolean; error?: string } => {
    if (!senderCardID || !recipientCardNumber) {
      return { isValid: true }; // No validation needed if data is missing
    }

    const selectedSenderCard = userCards.find(card => card.cardID.toString() === senderCardID);
    if (selectedSenderCard && selectedSenderCard.cardNumber === recipientCardNumber) {
      return {
        isValid: false,
        error: 'Cannot transfer to the same card. Please select a different recipient card.'
      };
    }

    return { isValid: true };
  },

  // Card status validation - check if card is active
  validateCardStatus: (card: UserCard | null, operation: 'transfer' | 'topup' = 'transfer'): { isValid: boolean; error?: string } => {
    if (!card) {
      return {
        isValid: false,
        error: 'Card not found'
      };
    }

    if (card.cardStatus !== CardStatus.Active) {
      const statusName = Object.keys(CardStatus).find(key => CardStatus[key as keyof typeof CardStatus] === card.cardStatus) || 'Unknown';
      return {
        isValid: false,
        error: `Cannot ${operation} to this card. Card status is ${statusName}. Only active cards are allowed.`
      };
    }

    return { isValid: true };
  },

  // Combined transfer validation - self-transfer and card status
  validateTransferRecipient: (senderCardID: string, recipientCardNumber: string, userCards: UserCard[]): { isValid: boolean; error?: string } => {
    // First check for self-transfer
    const selfTransferValidation = validationUtils.validateTransferSelfTransfer(senderCardID, recipientCardNumber, userCards);
    if (!selfTransferValidation.isValid) {
      return selfTransferValidation;
    }

    // For recipient validation, we only check if it's not the same as sender
    // The actual recipient card validation (existence, status, etc.) will be done by the backend
    //  since the frontend doesn't have access to all cards in the system
    return { isValid: true };
  }
}; 