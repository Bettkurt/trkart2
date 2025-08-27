import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import userCardService from '@/services/userCardService';
import { UserCard } from '@/types';
import { CardStatus } from '@/types/cardStatus';
import { logger } from '@/utils/logger';
import { Eye, EyeOff } from 'lucide-react';

interface PasswordVerificationResponse {
  isValid: boolean;
  message?: string;
}

const CardDeletionPage: React.FC = () => {
  const { cardId } = useParams<{ cardId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [card, setCard] = useState<UserCard | null>(null);
  const [showConfirmation, setShowConfirmation] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [isVerifying, setIsVerifying] = useState(false);
  const [passwordError, setPasswordError] = useState('');

  // Load card details
  useEffect(() => {
    logger.info('CardDeletionPage', 'mount', 'Component mounted', { cardId, hasUser: !!user, customerId: user?.customerID });
    
    const fetchCardDetails = async () => {
      if (!cardId || !user?.customerID) {
        const errorMsg = 'Invalid card or user information.';
        const errorContext = { cardId, hasUser: !!user };
        const error = new Error(`${errorMsg} Context: ${JSON.stringify(errorContext)}`);
        logger.error('CardDeletionPage', 'fetchCardDetails', errorMsg, error);
        setError(errorMsg);
        setIsLoading(false);
        return;
      }

      try {
        // First try to get from localStorage
        const userKey = user?.email ? `trkart_cards_${user.email}` : 'trkart_cards_anonymous';
        const storedCards = localStorage.getItem(userKey);      
        if (storedCards) {
          const cards: UserCard[] = JSON.parse(storedCards);
          const foundCard = cards.find(c => c.cardID.toString() === cardId);
          
          if (foundCard) {
            logger.info('CardDeletionPage', 'fetchCardDetails', 'Card found in localStorage', { 
              cardId, 
              cardNumber: foundCard.cardNumber 
            });
            setCard(foundCard);
            setIsLoading(false);
            return;
          }
        }

        logger.debug('CardDeletionPage', 'fetchCardDetails', 'Card not in localStorage, fetching from API', { cardId });
        
        // If not found in localStorage, try to fetch from API
        const cardNumber = await getCardNumberFromId(parseInt(cardId));
        if (cardNumber) {
          logger.debug('CardDeletionPage', 'fetchCardDetails', 'Fetching card details from API', { cardNumber });
          const cardDetails = await userCardService.getUserCardByNumber(cardNumber);
          logger.info('CardDeletionPage', 'fetchCardDetails', 'Card details fetched successfully', { 
            cardId: cardDetails.cardID,
            cardNumber: cardDetails.cardNumber,
            balance: cardDetails.balance
          });
          setCard(cardDetails);
        } else {
          const errorMsg = 'Card not found.';
          logger.warn('CardDeletionPage', 'fetchCardDetails', errorMsg, { cardId });
          setError(errorMsg);
        }
      } catch (err) {
        const errorMsg = 'Failed to load card details. Please try again.';
        const error = err instanceof Error ? err : new Error(String(err));
        logger.error('CardDeletionPage', 'fetchCardDetails', errorMsg, error, { cardId });
        setError(errorMsg);
      } finally {
        setIsLoading(false);
      }
    };

    fetchCardDetails();
    
    return () => {
      logger.debug('CardDeletionPage', 'unmount', 'Component unmounting');
    };
  }, [cardId, user]);

  const getCardNumberFromId = async (cardId: number): Promise<string | null> => {
    try {
      logger.debug('CardDeletionPage', 'getCardNumberFromId', 'Fetching card number from ID', { cardId });
      
      // Try to get the card by ID from the API
      const response = await userCardService.getUserCards();
      const foundCard = response.find(card => card.cardID === cardId);
      
      if (foundCard) {
        logger.debug('CardDeletionPage', 'getCardNumberFromId', 'Card number found', { 
          cardId, 
          cardNumber: foundCard.cardNumber 
        });
        return foundCard.cardNumber;
      } 
      
      logger.warn('CardDeletionPage', 'getCardNumberFromId', 'Card not found', { cardId });
      return null;
    } catch (err) {
      const error = err instanceof Error ? err : new Error(String(err));
      logger.error('CardDeletionPage', 'getCardNumberFromId', 'Error fetching card number', error, { cardId });
      return null;
    }
  };

  const verifyPassword = async (): Promise<boolean> => {
    if (!user?.email) {
      const errorMsg = 'No user email available for password verification';
      logger.error('CardDeletionPage', 'verifyPassword', errorMsg);
      setError('Authentication error. Please log in again.');
      return false;
    }
    
    if (!password) {
      const errorMsg = 'Password is required';
      logger.warn('CardDeletionPage', 'verifyPassword', errorMsg);
      setPasswordError(errorMsg);
      return false;
    }
    
    logger.info('CardDeletionPage', 'verifyPassword', 'Initiating password verification');
    setIsVerifying(true);
    setPasswordError('');
    
    try {
      // Call the password verification endpoint
      logger.debug('CardDeletionPage', 'verifyPassword', 'Sending password verification request');
      const response = await fetch('/api/auth/verify-password', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          email: user.email,
          password: password
        }),
        credentials: 'include'
      });

      const data: PasswordVerificationResponse = await response.json();
      
      if (!response.ok) {
        const errorMsg = data.message || 'Password verification failed';
        logger.warn('CardDeletionPage', 'verifyPassword', `Password verification failed: ${errorMsg}`, { 
          status: response.status 
        });
        throw new Error(errorMsg);
      }

      if (data.isValid) {
        logger.info('CardDeletionPage', 'verifyPassword', 'Password verification successful');
        return true;
      } else {
        const errorMsg = data.message || 'Incorrect password. Please try again.';
        logger.warn('CardDeletionPage', 'verifyPassword', 'Password verification failed: Invalid credentials');
        setPasswordError(errorMsg);
        return false;
      }
    } catch (err) {
      const error = err instanceof Error ? err : new Error(String(err));
      const errorMsg = 'Failed to verify password. Please try again.';
      logger.error('CardDeletionPage', 'verifyPassword', 'Password verification error', error);
      setPasswordError(errorMsg);
      return false;
    } finally {
      setIsVerifying(false);
    }
  };

  const handleDeactivation = async (): Promise<void> => {
    if (!card) {
      logger.error('CardDeletionPage', 'handleDeactivation', 'No card selected for deletion');
      return;
    }
    
    if (!showConfirmation) {
      logger.info('CardDeletionPage', 'handleDeactivation', 'Showing deletion confirmation', { 
        cardId: card.cardID, 
        cardNumber: card.cardNumber 
      });
      setShowConfirmation(true);
      return;
    }

    // If we haven't verified the password yet, verify it first
    if (showConfirmation && !isDeleting) {
      logger.debug('CardDeletionPage', 'handleDeactivation', 'Verifying password before deletion');
      const isValid = await verifyPassword();
      if (!isValid) {
        logger.warn('CardDeletionPage', 'handleDeactivation', 'Password verification failed, aborting deletion');
        setPassword(''); // Clear the password field on error
        return;
      }
      // If password is valid, proceed with deletion
      logger.info('CardDeletionPage', 'handleDeactivation', 'Password verified, proceeding with deletion', { 
        cardId: card.cardID, 
        cardNumber: card.cardNumber 
      });
    }

    logger.info('CardDeletionPage', 'handleDeactivation', 'Initiating card deletion', { 
      cardId: card.cardID, 
      cardNumber: card.cardNumber,
      hasBalance: card.balance > 0
    });
    
    setIsDeleting(true);
    try {
      // Update card status to 'Deactivated' using the new endpoint
      logger.debug('CardDeletionPage', 'handleDeactivation', 'Sending status update request to API', { 
        cardId: card.cardID,
        newStatus: CardStatus.Deactivated
      });
      
      await userCardService.updateCardStatus({
        cardId: card.cardID,
        status: CardStatus.Deactivated
      });
      
      logger.info('CardDeletionPage', 'handleDeactivation', 'Card status updated to Deactivated', { 
        cardId: card.cardID, 
        cardNumber: card.cardNumber 
      });
      
      // Remove from localStorage
      if (user?.email) {
        const userKey = `trkart_cards_${user.email}`;
        const storedCards = localStorage.getItem(userKey);
        if (storedCards) {
          const cards: UserCard[] = JSON.parse(storedCards);
          const updatedCards = cards.filter(c => c.cardID !== card.cardID);
          localStorage.setItem(userKey, JSON.stringify(updatedCards));
          logger.debug('CardDeletionPage', 'handleDeactivation', 'Removed card from localStorage', { 
            cardId: card.cardID,
            remainingCards: updatedCards.length
          });
        }
      }
      
      logger.info('CardDeletionPage', 'handleDeactivation', 'Navigating to cards page after successful deletion');
      navigate('/cards');
    } catch (err) {
      const error = err instanceof Error ? err : new Error(String(err));
      const errorMsg = 'Failed to delete card. Please try again.';
      logger.error('CardDeletionPage', 'handleDeactivation', errorMsg, error, { 
        cardId: card.cardID,
        cardNumber: card.cardNumber 
      });
      setError(errorMsg);
      setIsDeleting(false);
      setShowConfirmation(false);
    }
  };

  const handleTransferFunds = (): void => {
    if (!card) {
      logger.error('CardDeletionPage', 'handleTransferFunds', 'No card selected for fund transfer');
      return;
    }
    
    logger.info('CardDeletionPage', 'handleTransferFunds', 'Initiating fund transfer before card deletion', {
      cardId: card.cardID,
      cardNumber: card.cardNumber,
      transferAmount: card.balance
    });
    
    navigate('/new-transfer', { 
      state: { 
        fromCardNumber: card.cardNumber,
        amount: card.balance,
        transferMode: true
      } 
    });
  };

  const handleCancel = (): void => {
    logger.info('CardDeletionPage', 'handleCancel', 'User canceled card deletion', {
      cardId: card?.cardID,
      cardNumber: card?.cardNumber,
      hadConfirmation: showConfirmation
    });
    navigate('/cards');
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error || !card) {
    return (
      <div className="min-h-screen bg-gray-50 p-6">
        <div className="max-w-2xl mx-auto bg-white p-6 rounded-lg shadow">
          <h1 className="text-2xl font-bold text-red-600 mb-4">Error</h1>
          <p className="mb-4 text-gray-700">
            {error || 'The requested card could not be found. It may have been deleted or you may not have permission to view it.'}
          </p>
          <div className="mt-6">
            <button
              onClick={handleCancel}
              className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 transition-colors"
            >
              Back to Cards
            </button>
          </div>
        </div>
      </div>
    );
  }

  const hasBalance = card.balance > 0;

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-2xl mx-auto bg-white p-6 rounded-lg shadow">
        <h1 className="text-2xl font-bold text-red-600 mb-6">Delete Card</h1>
        
        <div className="mb-6 p-4 border border-yellow-200 bg-yellow-50 rounded">
          <h2 className="font-semibold text-yellow-700 mb-2">Warning</h2>
          <p className="text-yellow-700">
            Deleting this card will permanently remove all its transaction history. This action cannot be undone.
          </p>
        </div>

        <div className="mb-6 p-4 border rounded">
          <h2 className="font-semibold mb-2">Card Details</h2>
          <p><span className="font-medium">Card Number:</span> {card.cardNumber}</p>
          <p><span className="font-medium">Balance:</span> {card.balance.toFixed(2)} TL</p>
          <p><span className="font-medium">Status:</span> {card.cardStatus}</p>
        </div>

        {hasBalance && (
          <div className="mb-6 p-4 border border-red-200 bg-red-50 rounded">
            <h2 className="font-semibold text-red-700 mb-2">Balance Warning</h2>
            {card.cardStatus === 2 ? (
              // Lost card message
              <div>
                <p className="text-red-700 mb-4">
                  This lost card has a balance of <strong>{card.balance.toFixed(2)} TL</strong>.
                  The remaining balance will be automatically transferred to one of your active cards in 7 business days.
                </p>
                <p className="text-red-700 mb-4">
                  If you choose to proceed with the deletion now, remaining funds will be lost.
                </p>
              </div>
            ) : (
              // Active/Inactive card message with transfer option
              <div>
                <p className="text-red-700 mb-4">
                  This card has a balance of <strong>{card.balance.toFixed(2)} TL</strong>. 
                  Deleting it will result in losing this balance.
                </p>
                <div className="flex flex-wrap gap-3 mt-3">
                  <button
                    onClick={handleTransferFunds}
                    className="btn-primary"
                  >
                    Transfer Balance to Another Card
                  </button>
                </div>
              </div>
            )}
          </div>
        )}

        {showConfirmation ? (
          <div className="mt-6 p-4 border border-red-200 bg-red-50 rounded">
            <h2 className="font-semibold text-red-700 mb-4">Are you sure you want to delete this card?</h2>
            <p className="text-red-700 mb-4">
              This action cannot be undone. All transaction history for this card will be permanently deleted.
              {hasBalance && ' Any remaining balance will be lost.'}
            </p>
            
            <div className="mb-4">
              <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1">
                Confirm your password to continue
              </label>
              <div className="relative">
                <input
                  id="password"
                  name="password"
                  type={showPassword ? 'text' : 'password'}
                  required
                  className="w-full px-3 py-2 border-2 border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-400 focus:border-transparent"
                  placeholder="Enter your password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  disabled={isVerifying || isDeleting}
                />
                <button
                  type="button"
                  className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-500 hover:text-gray-700"
                  onClick={() => setShowPassword(!showPassword)}
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                >
                  {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                </button>
              </div>
              {passwordError && (
                <p className="mt-1 text-sm text-red-600">{passwordError}</p>
              )}
            </div>
            
            <div className="flex gap-3">
              <button
                onClick={handleDeactivation}
                disabled={isVerifying || isDeleting || !password}
                className="px-6 py-2 bg-red-600 hover:bg-red-700 text-white font-semibold rounded-md shadow-md transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-opacity-50 disabled:opacity-70 disabled:cursor-not-allowed"
              >
                {isVerifying ? 'Verifying...' : isDeleting ? 'Deleting...' : 'Yes, Delete Permanently'}
              </button>
              <button
                onClick={() => setShowConfirmation(false)}
                disabled={isDeleting}
                className="btn-secondary"
              >
                Cancel
              </button>
            </div>
          </div>
        ) : (
          <div className="mt-6 flex gap-3">
            <button
              onClick={handleDeactivation}
              className="px-6 py-2 bg-red-600 hover:bg-red-700 text-white font-semibold rounded-md shadow-md transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-opacity-50 disabled:opacity-70 disabled:cursor-not-allowed"
              disabled={isDeleting}
            >
              Delete This Card
            </button>
            <button
              onClick={handleCancel}
              className="btn-secondary"
              disabled={isDeleting}
            >
              Cancel
            </button>
          </div>
        )}
      </div>
    </div>
  );
};

export default CardDeletionPage;
