import { UserCard } from '@/types';
import { CardStatus } from '@/types/cardStatus';
import { validationUtils } from '@/utils/validationUtils';

export interface ValidationResult {
  isValid: boolean;
  error?: string;
}

export interface TransferValidationData {
  senderCardID: string;
  recipientCardNumber: string;
  amount: string;
  userCards: UserCard[];
}

export interface TransactionValidationData {
  cardID: string;
  amount: string;
  transactionType: string;
  description: string;
  userCards: UserCard[];
}

class ValidationService {
  /**
   * Validates sender card status for transfers
   */
  validateSenderCard(senderCardID: string, userCards: UserCard[]): ValidationResult {
    if (!senderCardID) {
      return { isValid: true }; // Valid (no card selected yet)
    }

    const selectedCard = userCards.find(card => card.cardID.toString() === senderCardID);
    return validationUtils.validateCardStatus(selectedCard || null, 'transfer');
  }

  /**
   * Validates recipient card for transfers (self-transfer blocking and status checking)
   */
  validateTransferRecipient(senderCardID: string, recipientCardNumber: string, userCards: UserCard[]): ValidationResult {
    // First validate sender card
    const senderValidation = this.validateSenderCard(senderCardID, userCards);
    if (!senderValidation.isValid) {
      return { isValid: false, error: 'Please select a valid sender card first' };
    }

    // Then validate recipient
    return validationUtils.validateTransferRecipient(senderCardID, recipientCardNumber, userCards);
  }

  /**
   * Validates complete transfer form
   */
  validateTransferForm(data: TransferValidationData): { [key: string]: string } {
    const errors: { [key: string]: string } = {};

    // Validate sender card
    if (!data.senderCardID) {
      errors.senderCardID = 'Please select a sender card';
    } else {
      const senderValidation = this.validateSenderCard(data.senderCardID, data.userCards);
      if (!senderValidation.isValid) {
        errors.senderCardID = 'Selected card is not active. Please choose an active card.';
      }
    }

    // Validate recipient card
    if (!data.recipientCardNumber) {
      errors.recipientCardNumber = 'Please enter recipient card number';
    } else if (data.recipientCardNumber.length !== 16) {
      errors.recipientCardNumber = 'Card number must be 16 characters';
    } else if (!errors.senderCardID) {
      // Only validate recipient if sender is valid
      const recipientValidation = this.validateTransferRecipient(data.senderCardID, data.recipientCardNumber, data.userCards);
      if (!recipientValidation.isValid) {
        errors.recipientCardNumber = 'Invalid recipient card. Please check the card number and status.';
      }
    }

    // Validate amount
    if (!data.amount) {
      errors.amount = 'Please enter transfer amount';
    } else {
      const amountValidation = validationUtils.validateAmount(data.amount);
      if (!amountValidation.isValid) {
        errors.amount = amountValidation.error || 'Invalid amount';
      }
    }

    return errors;
  }

  /**
   * Validates complete transaction form
   */
  validateTransactionForm(data: TransactionValidationData): { [key: string]: string } {
    const errors: { [key: string]: string } = {};

    // Validate card selection
    if (!data.cardID) {
      errors.cardID = 'Please select a card';
    } else {
      const selectedCard = data.userCards.find(card => card.cardID.toString() === data.cardID);
      if (!selectedCard) {
        errors.cardID = 'Selected card not found';
      } else if (selectedCard.cardStatus !== CardStatus.Active && selectedCard.cardStatus !== CardStatus.Inactive) {
        errors.cardID = 'Selected card is not available for transactions';
      }
    }

    // Validate amount
    if (!data.amount) {
      errors.amount = 'Please enter amount';
    } else {
      const amountValidation = validationUtils.validateAmount(data.amount);
      if (!amountValidation.isValid) {
        errors.amount = amountValidation.error || 'Invalid amount';
      }
    }

    // Validate transaction type
    if (!data.transactionType) {
      errors.transactionType = 'Please select transaction type';
    } else {
      const typeValidation = validationUtils.validateTransactionType(Number(data.transactionType));
      if (!typeValidation.isValid) {
        errors.transactionType = typeValidation.error || 'Invalid transaction type';
      }
    }

    // Validate description (optional)
    if (data.description) {
      const descValidation = validationUtils.validateDescription(data.description);
      if (!descValidation.isValid) {
        errors.description = descValidation.error || 'Invalid description';
      }
    }

    return errors;
  }

  /**
   * Filters cards based on status for different operations
   */
  getAvailableCardsForTransfer(userCards: UserCard[]): UserCard[] {
    return userCards.filter(card => card.cardStatus === CardStatus.Active);
  }

  getAvailableCardsForTransaction(userCards: UserCard[]): UserCard[] {
    return userCards.filter(card => 
      card.cardStatus === CardStatus.Active || card.cardStatus === CardStatus.Inactive
    );
  }

  getAvailableCardsForTopUp(userCards: UserCard[]): UserCard[] {
    return userCards.filter(card => card.cardStatus === CardStatus.Active);
  }
}

export const validationService = new ValidationService();
export default validationService;
