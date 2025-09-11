import React, { useState, useEffect } from 'react';
import { walletService } from '@/services/walletService';
import userCardService from '@/services/userCardService';
import { logger } from '@/utils/logger';
import { UserCard } from '@/types';
import { CardStatus } from '@/types/cardStatus';
import { TransactionType } from '@/types/wallet';

interface LoadWalletFormProps {
  walletId: number;
  onSuccess: () => void;
}

const LoadWalletForm: React.FC<LoadWalletFormProps> = ({ walletId, onSuccess }) => {
  const [amount, setAmount] = useState('');
  const [selectedCard, setSelectedCard] = useState<UserCard | null>(null);
  const [userCards, setUserCards] = useState<UserCard[]>([]);
  const [isLoadingCards, setIsLoadingCards] = useState(true);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  // Fetch user's cards on component mount
  useEffect(() => {
    const fetchCards = async () => {
      try {
        const cards = await userCardService.getUserCards();
        const activeCards = cards.filter(card => card.cardStatus === CardStatus.Active);
        setUserCards(activeCards);
        if (activeCards.length > 0) {
          setSelectedCard(activeCards[0]);
        }
      } catch (err) {
        console.error('Error fetching cards:', err);
        setError('Failed to load cards');
      } finally {
        setIsLoadingCards(false);
      }
    };

    fetchCards();
  }, []);

  // Format card number to show only last 4 digits with masking
  const formatCardNumber = (cardNumber: string): string => {
    if (!cardNumber) return '';
    const cleanNumber = cardNumber.replace(/\D/g, '');
    const lastFour = cleanNumber.slice(-4);
    return `•••• •••• •••• ${lastFour}`;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    // Validate inputs
    const amountValue = parseFloat(amount);
    if (!amount || isNaN(amountValue) || amountValue <= 0) {
      setError('Please enter a valid amount');
      return;
    }

    if (!selectedCard) {
      setError('Please select a card');
      return;
    }

    setIsLoading(true);
    setError(null);
    setSuccess(false);

    try {
      const transactionData = {
        walletId,
        cardId: selectedCard.cardID,
        amount: amountValue,
        description: `Wallet load from card ${selectedCard.cardNumber.replace(/\D/g, '').slice(-4)}`,
        referenceId: `WALLET_LOAD_${Date.now()}`,
        transactionType: TransactionType.Load
      };
      
      console.log('Sending wallet load request:', transactionData);
      const response = await walletService.loadWallet(transactionData);
      
      if (!response) {
        throw new Error('No response received from server');
      }

      setSuccess(true);
      setAmount('');
      setSelectedCard(userCards.length > 0 ? userCards[0] : null);
      onSuccess();
      
      // Reset success message after 3 seconds
      setTimeout(() => setSuccess(false), 3000);
    } catch (err) {
      const error = err as Error;
      logger.error('LoadWalletForm', 'handleSubmit', 'Failed to load wallet', error);
      setError(error.message || 'Failed to load wallet. Please try again.');
    } finally {
      setIsLoading(false);
    }
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
              <p className="text-sm text-green-700">Wallet loaded successfully!</p>
            </div>
          </div>
        </div>
      )}

      <div>
        <label htmlFor="amount" className="block text-sm font-medium text-gray-700 mb-1">
          Amount to Load
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
            required
          />
        </div>
      </div>

      <div className="mb-4">
        <label htmlFor="card" className="block text-sm font-medium text-gray-700 mb-1">
          Select Card
        </label>
        {isLoadingCards ? (
          <div className="animate-pulse h-10 bg-gray-200 rounded-md"></div>
        ) : userCards.length === 0 ? (
          <p className="text-sm text-gray-500">No active cards found. Please add a card first.</p>
        ) : (
          <div className="relative">
            <select
              id="card"
              value={selectedCard?.cardID || ''}
              onChange={(e) => {
                const card = userCards.find(c => c.cardID === parseInt(e.target.value));
                setSelectedCard(card || null);
              }}
              className="block w-full pl-3 pr-10 py-2.5 text-base border border-gray-300 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm rounded-md appearance-none bg-white"
              disabled={isLoading || isLoadingCards || userCards.length === 0}
              required
            >
              {userCards.map((card) => (
                <option key={card.cardID} value={card.cardID}>
                  {formatCardNumber(card.cardNumber)} • {card.cardName || 'No Name'}
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
            {selectedCard.cardName && (
              <div className="flex justify-between text-sm mt-1">
                <span className="font-medium text-gray-700">Card Name:</span>
                <span>{selectedCard.cardName}</span>
              </div>
            )}
          </div>
        )}
        <p className="mt-1 text-xs text-gray-500">
          Select a card to load money from
        </p>
      </div>

      <div className="pt-2">
        <button
          type="submit"
          disabled={isLoading}
          className={`w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 ${
            isLoading ? 'opacity-70 cursor-not-allowed' : ''
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
          ) : (
            'Load Money'
          )}
        </button>
      </div>
    </form>
  );
};

export default LoadWalletForm;
