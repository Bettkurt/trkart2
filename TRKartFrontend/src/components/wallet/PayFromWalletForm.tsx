import React, { useState, useEffect } from 'react';
import { walletService } from '@/services/walletService';
import userCardService from '@/services/userCardService';
import { logger } from '@/utils/logger';
import { CardStatus } from '@/types/cardStatus';
import { CreateWalletTransactionDto } from '@/types/wallet';

interface UserCard {
  cardID: number;
  cardNumber: string;
  cardHolderName?: string;
  cardStatus: number;
}

interface PayFromWalletFormProps {
  walletId: number;
  balance: number;
  onSuccess: () => void;
}

const PayFromWalletForm: React.FC<PayFromWalletFormProps> = ({ 
  walletId, 
  balance,
  onSuccess 
}) => {
  const [amount, setAmount] = useState('');
  const [selectedCard, setSelectedCard] = useState<UserCard | null>(null);
  const [description, setDescription] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [cards, setCards] = useState<UserCard[]>([]);

  // Fetch user's cards on component mount
  useEffect(() => {
    const fetchCards = async () => {
      try {
        const userCards = await userCardService.getUserCards();
        const activeCards = userCards.filter(card => card.cardStatus === CardStatus.Active);
        setCards(activeCards);
        if (activeCards.length > 0) {
          setSelectedCard(activeCards[0]);
        }
      } catch (err) {
        console.error('Error fetching cards:', err);
        setError('Failed to load cards. Please try again.');
      } finally {
        setIsLoading(false);
      }
    };

    fetchCards();
  }, []);


  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Reset previous states
    setError(null);
    setSuccess(false);

    // Validate amount
    const amountValue = parseFloat(amount);
    if (isNaN(amountValue) || amountValue <= 0) {
      setError('Please enter a valid amount greater than 0');
      return;
    }

    if (amountValue > balance) {
      setError('Insufficient balance');
      return;
    }

    if (!selectedCard) {
      setError('Please select a card');
      return;
    }

    const descriptionText = description.trim();
    if (!descriptionText) {
      setError('Please enter a description for this payment');
      return;
    }

    setIsLoading(true);

    try {
      // Prepare transaction data with proper types
      // The TransactionType.Pay is added by the payFromWallet service
      const transactionData: CreateWalletTransactionDto = {
        walletId,
        amount: amountValue, // Already validated as number
        cardId: selectedCard.cardID,
        description: descriptionText,
        referenceId: `WALLET_PAY_${Date.now()}`
      };
      
      logger.info('PayFromWalletForm', 'handleSubmit', 'Sending wallet payment request', transactionData);
      
      // Make the API call
      const response = await walletService.payFromWallet(transactionData);
      
      if (!response) {
        throw new Error('No response received from server');
      }

      // Update UI state on success
      setSuccess(true);
      setAmount('');
      setDescription('');
      
      // Notify parent component of success
      onSuccess();
      
      // Reset success message after 3 seconds
      const timer = setTimeout(() => setSuccess(false), 3000);
      
      // Cleanup timer on component unmount
      return () => clearTimeout(timer);
      
    } catch (err) {
      const error = err as Error & { response?: any };
      
      // Log detailed error information
      console.error('Payment error details:', {
        message: error.message,
        response: error.response?.data,
        status: error.response?.status,
        statusText: error.response?.statusText,
        config: error.response?.config
      });
      
      logger.error('PayFromWalletForm', 'handleSubmit', 'Failed to process payment', error);
      
      // Provide more user-friendly error messages
      let errorMessage = 'Failed to process payment. Please try again.';
      
      if (error.response?.status === 401) {
        errorMessage = 'Session expired. Please log in again.';
      } else if (error.response?.status === 400) {
        errorMessage = error.response.data?.message || 'Invalid request. Please check your input and try again.';
      } else if (error.message.includes('network')) {
        errorMessage = 'Network error. Please check your connection and try again.';
      } else if (error.response?.data?.message) {
        errorMessage = error.response.data.message;
      } else if (error.message) {
        errorMessage = error.message;
      }
      
      setError(errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  const formatCurrency = (value: number): string => {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: 'TRY',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(value);
  };

  const formatCardNumber = (cardNumber: string): string => {
    if (!cardNumber) return '';
    const cleanNumber = cardNumber.replace(/\D/g, '');
    const lastFour = cleanNumber.slice(-4);
    return `•••• •••• •••• ${lastFour}`;
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {error && (
        <div className="bg-red-50 border-l-4 border-red-500 p-4 mb-4">
          <div className="flex">
            <div className="flex-shrink-0">
              <svg className="h-5 w-5 text-red-500" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
            </div>
            <div className="ml-3">
              <p className="text-sm text-red-700">{error}</p>
            </div>
          </div>
        </div>
      )}

      {success && (
        <div className="bg-green-50 border-l-4 border-green-500 p-4 mb-4">
          <div className="flex">
            <div className="flex-shrink-0">
              <svg className="h-5 w-5 text-green-500" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
              </svg>
            </div>
            <div className="ml-3">
              <p className="text-sm text-green-700">Payment processed successfully!</p>
            </div>
          </div>
        </div>
      )}

      {/* Amount Input */}
      <div className="mb-4">
        <label htmlFor="amount" className="block text-sm font-medium text-gray-700 mb-1">
          Amount to Pay
        </label>
        <div className="mt-1 relative rounded-md shadow-sm">
          <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
            <span className="text-gray-500 sm:text-sm">₺</span>
          </div>
          <input
            type="number"
            id="amount"
            className="focus:ring-blue-500 focus:border-blue-500 block w-full pl-7 pr-12 sm:text-sm border-gray-300 rounded-md py-2 border"
            placeholder="0.00"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            step="0.01"
            min="0.01"
            max={balance}
            disabled={isLoading || balance <= 0}
            required
          />
          <div className="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none">
            <span className="text-gray-500 sm:text-sm">
              TRY
            </span>
          </div>
        </div>
        <p className="mt-1 text-xs text-gray-500">
          Available balance: {formatCurrency(balance)}
        </p>
      </div>

      {/* Card Selection */}
      <div className="mb-4">
        <label htmlFor="card" className="block text-sm font-medium text-gray-700 mb-1">
          Select Card
        </label>
        {isLoading ? (
          <div className="animate-pulse h-10 bg-gray-200 rounded-md"></div>
        ) : cards.length === 0 ? (
          <p className="text-sm text-gray-500">No active cards found. Please add a card first.</p>
        ) : (
          <div className="relative">
            <select
              id="card"
              value={selectedCard?.cardID || ''}
              onChange={(e) => {
                const card = cards.find(c => c.cardID === parseInt(e.target.value));
                setSelectedCard(card || null);
              }}
              className="block w-full pl-3 pr-10 py-2.5 text-base border border-gray-300 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm rounded-md appearance-none bg-white"
              disabled={isLoading || cards.length === 0}
              required
            >
              {cards.map((card) => (
                <option key={card.cardID} value={card.cardID}>
                  {formatCardNumber(card.cardNumber)} • {card.cardHolderName || 'No Name'}
                </option>
              ))}
            </select>
            <div className="pointer-events-none absolute inset-y-0 right-0 flex items-center px-2 text-gray-700">
              <svg className="fill-current h-4 w-4" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20">
                <path d="M9.293 12.95l.707.707L15.657 8l-1.414-1.414L10 10.828 5.757 6.586 4.343 8z" />
              </svg>
            </div>
          </div>
        )}
        {selectedCard && (
          <div className="mt-2 p-3 bg-gray-50 rounded-md border border-gray-200">
            <div className="flex justify-between text-sm">
              <span className="font-medium text-gray-700">Card Number:</span>
              <span className="font-mono">{formatCardNumber(selectedCard.cardNumber)}</span>
            </div>
            {selectedCard.cardHolderName && (
              <div className="flex justify-between text-sm mt-1">
                <span className="font-medium text-gray-700">Card Name:</span>
                <span>{selectedCard.cardHolderName}</span>
              </div>
            )}
          </div>
        )}
        <p className="mt-1 text-xs text-gray-500">
          Select a card to receive the payment
        </p>
      </div>

      <div className="mb-4">
        <label htmlFor="description" className="block text-sm font-medium text-gray-700 mb-1">
          Description (Optional)
        </label>
        <input
          type="text"
          id="description"
          className="shadow-sm focus:ring-blue-500 focus:border-blue-500 block w-full sm:text-sm border-gray-300 rounded-md py-2 px-3 border"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Enter payment description"
          disabled={isLoading}
        />
      </div>

      <div className="pt-2">
        <button
          type="submit"
          disabled={isLoading || balance <= 0}
          className={`w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 ${
            isLoading || balance <= 0 ? 'opacity-70 cursor-not-allowed' : ''
          }`}
        >
          {isLoading ? (
            <>
              <svg className="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              Processing...
            </>
          ) : balance <= 0 ? 'Insufficient Balance' : 'Make Payment'}
        </button>
      </div>
    </form>
  );
};

export default PayFromWalletForm;
