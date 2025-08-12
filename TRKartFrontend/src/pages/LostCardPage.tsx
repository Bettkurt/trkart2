import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import userCardService from '@/services/userCardService';
import { UserCard } from '@/types';
import { logger } from '@/utils/logger';
import { Eye, EyeOff } from 'lucide-react';

interface PasswordVerificationResponse {
  isValid: boolean;
  message?: string;
}

const LostCardPage: React.FC = () => {
  const { cardId } = useParams<{ cardId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [card, setCard] = useState<UserCard | null>(null);
  const [showConfirmation, setShowConfirmation] = useState(false);
  const [isMarkingAsLost, setIsMarkingAsLost] = useState(false);
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [isVerifying, setIsVerifying] = useState(false);
  const [passwordError, setPasswordError] = useState('');

  // Load card details
  useEffect(() => {
    logger.info('LostCardPage', 'mount', 'Component mounted', { cardId, hasUser: !!user, customerId: user?.customerID });
    
    const fetchCardDetails = async () => {
      if (!cardId || !user?.customerID) {
        const errorMsg = 'Invalid card or user information.';
        const errorContext = { cardId, hasUser: !!user };
        const error = new Error(`${errorMsg} Context: ${JSON.stringify(errorContext)}`);
        logger.error('LostCardPage', 'fetchCardDetails', errorMsg, error);
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
            logger.info('LostCardPage', 'fetchCardDetails', 'Card found in localStorage', { 
              cardId, 
              cardNumber: foundCard.cardNumber 
            });
            setCard(foundCard);
            setIsLoading(false);
            return;
          }
        }

        // If not found in localStorage, try to fetch from API
        const cards = await userCardService.getUserCards();
        const foundCard = cards.find(c => c.cardID.toString() === cardId);
        
        if (foundCard) {
          logger.info('LostCardPage', 'fetchCardDetails', 'Card details fetched from API', { 
            cardId: foundCard.cardID,
            cardNumber: foundCard.cardNumber,
            balance: foundCard.balance
          });
          setCard(foundCard);
        } else {
          const errorMsg = 'Card not found.';
          logger.warn('LostCardPage', 'fetchCardDetails', errorMsg, { cardId });
          setError(errorMsg);
        }
      } catch (err) {
        const errorMsg = 'Failed to load card details. Please try again.';
        const error = err instanceof Error ? err : new Error(String(err));
        logger.error('LostCardPage', 'fetchCardDetails', errorMsg, error, { cardId });
        setError(errorMsg);
      } finally {
        setIsLoading(false);
      }
    };

    fetchCardDetails();
    
    return () => {
      logger.debug('LostCardPage', 'unmount', 'Component unmounting');
    };
  }, [cardId, user]);

  const verifyPassword = async (): Promise<boolean> => {
    if (!user?.email) {
      const errorMsg = 'No user email available for password verification';
      logger.error('LostCardPage', 'verifyPassword', errorMsg);
      setError('Authentication error. Please log in again.');
      return false;
    }
    
    if (!password) {
      const errorMsg = 'Password is required';
      logger.warn('LostCardPage', 'verifyPassword', errorMsg);
      setPasswordError(errorMsg);
      return false;
    }
    
    logger.info('LostCardPage', 'verifyPassword', 'Initiating password verification');
    setIsVerifying(true);
    setPasswordError('');
    
    try {
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
        logger.warn('LostCardPage', 'verifyPassword', `Password verification failed: ${errorMsg}`, { 
          status: response.status 
        });
        throw new Error(errorMsg);
      }

      if (data.isValid) {
        logger.info('LostCardPage', 'verifyPassword', 'Password verification successful');
        return true;
      } else {
        const errorMsg = data.message || 'Incorrect password. Please try again.';
        logger.warn('LostCardPage', 'verifyPassword', 'Password verification failed: Invalid credentials');
        setPasswordError(errorMsg);
        return false;
      }
    } catch (err) {
      const error = err instanceof Error ? err : new Error(String(err));
      const errorMsg = 'Failed to verify password. Please try again.';
      logger.error('LostCardPage', 'verifyPassword', 'Password verification error', error);
      setPasswordError(errorMsg);
      return false;
    } finally {
      setIsVerifying(false);
    }
  };

  const handleMarkAsLost = async (): Promise<void> => {
    if (!card) {
      logger.error('LostCardPage', 'handleMarkAsLost', 'No card selected');
      return;
    }
    
    if (!showConfirmation) {
      logger.info('LostCardPage', 'handleMarkAsLost', 'Showing confirmation dialog', { 
        cardId: card.cardID, 
        cardNumber: card.cardNumber 
      });
      setShowConfirmation(true);
      return;
    }

    // Verify password if not already verified
    if (showConfirmation && !isMarkingAsLost) {
      logger.debug('LostCardPage', 'handleMarkAsLost', 'Verifying password before marking as lost');
      const isValid = await verifyPassword();
      if (!isValid) {
        logger.warn('LostCardPage', 'handleMarkAsLost', 'Password verification failed, aborting');
        setPassword('');
        return;
      }
      logger.info('LostCardPage', 'handleMarkAsLost', 'Password verified, proceeding', { 
        cardId: card.cardID, 
        cardNumber: card.cardNumber 
      });
    }

    logger.info('LostCardPage', 'handleMarkAsLost', 'Marking card as lost', { 
      cardId: card.cardID, 
      cardNumber: card.cardNumber,
      hasBalance: card.balance > 0
    });
    
    setIsMarkingAsLost(true);
    try {
      // Update card status to 'Lost' using the API
      logger.debug('LostCardPage', 'handleMarkAsLost', 'Sending status update request to API', { 
        cardId: card.cardID,
        newStatus: 'Lost'
      });
      
      await userCardService.updateCardStatus({
        cardId: card.cardID,
        status: 'Lost'
      });
      
      logger.info('LostCardPage', 'handleMarkAsLost', 'Card status updated to Lost', { 
        cardId: card.cardID, 
        cardNumber: card.cardNumber 
      });
      
      // Update localStorage
      if (user?.email) {
        const userKey = `trkart_cards_${user.email}`;
        const storedCards = localStorage.getItem(userKey);
        if (storedCards) {
          const cards: UserCard[] = JSON.parse(storedCards);
          const updatedCards = cards.map(c => 
            c.cardID === card.cardID ? { ...c, cardStatus: 'Lost' } : c
          );
          localStorage.setItem(userKey, JSON.stringify(updatedCards));
          logger.debug('LostCardPage', 'handleMarkAsLost', 'Updated card status in localStorage', { 
            cardId: card.cardID,
            newStatus: 'Lost'
          });
        }
      }
      
      // Navigate to cards page with success message
      logger.info('LostCardPage', 'handleMarkAsLost', 'Navigating to cards page after marking as lost');
      navigate('/cards', { 
        state: { 
          message: 'Card has been marked as lost successfully.',
          messageType: 'success'
        } 
      });
    } catch (err) {
      const error = err instanceof Error ? err : new Error(String(err));
      const errorMsg = 'Failed to mark card as lost. Please try again.';
      logger.error('LostCardPage', 'handleMarkAsLost', errorMsg, error, { 
        cardId: card.cardID,
        cardNumber: card.cardNumber 
      });
      setError(errorMsg);
      setIsMarkingAsLost(false);
      setShowConfirmation(false);
    }
  };

  const handleTransferFunds = (): void => {
    if (!card) {
      logger.error('LostCardPage', 'handleTransferFunds', 'No card selected for fund transfer');
      return;
    }
    
    logger.info('LostCardPage', 'handleTransferFunds', 'Initiating fund transfer before marking as lost', {
      cardId: card.cardID,
      cardNumber: card.cardNumber,
      transferAmount: card.balance
    });
    
    navigate('/new-transaction', { 
      state: { 
        fromCardNumber: card.cardNumber,
        amount: card.balance,
        transferMode: true,
        returnUrl: `/cards/lost/${card.cardID}`
      } 
    });
  };

  const handleCancel = (): void => {
    logger.info('LostCardPage', 'handleCancel', 'User canceled marking card as lost', {
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
        <h1 className="text-2xl font-bold text-amber-600 mb-6">Report Lost Card</h1>
        
        <div className="mb-6 p-4 border border-amber-200 bg-amber-50 rounded">
          <h2 className="font-semibold text-amber-700 mb-2">Important Notice</h2>
          <p className="text-amber-700">
            Marking your card as lost will immediately block all transactions. Please read the restrictions below carefully.
          </p>
        </div>

        <div className="mb-6 p-4 border rounded">
          <h2 className="font-semibold mb-2">Card Details</h2>
          <p><span className="font-medium">Card Number:</span> {card.cardNumber}</p>
          <p><span className="font-medium">Balance:</span> {card.balance.toFixed(2)} TL</p>
          <p><span className="font-medium">Status:</span> {card.cardStatus}</p>
        </div>

        <div className="mb-6 p-4 border border-red-100 bg-red-50 rounded">
          <h2 className="font-semibold text-red-700 mb-2">What happens when you mark a card as lost?</h2>
          <ul className="list-disc pl-5 space-y-2 text-red-700">
            <li>You won't be able to use this card for any transactions</li>
            <li>Adding money to this card will be disabled</li>
            <li>All payment and transfer attempts will be blocked</li>
            <li>You won't be able to receive any transfers or refunds</li>
            <li>To reactivate this card, you'll need to visit a bank branch in person</li>
          </ul>
        </div>

        {hasBalance && (
          <div className="mb-6 p-4 border border-yellow-200 bg-yellow-50 rounded">
            <h2 className="font-semibold text-yellow-700 mb-2">Balance Notice</h2>
            <p className="text-yellow-700 mb-4">
              This card has a balance of {card.balance.toFixed(2)} TL. 
              Consider transferring this amount to another card before marking as lost.
            </p>
            <div className="flex flex-wrap gap-3 mt-3">
              <button
                onClick={handleTransferFunds}
                className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-md transition-colors"
              >
                Transfer Balance to Another Card
              </button>
            </div>
          </div>
        )}

        {showConfirmation ? (
          <div className="mt-6 p-4 border border-red-200 bg-red-50 rounded">
            <h2 className="font-semibold text-red-700 mb-4">Are you sure you want to mark this card as lost?</h2>
            <p className="text-red-700 mb-4">
              This action is irreversible. The card will be immediately blocked and cannot be used for any transactions.
              {hasBalance && ' Any remaining balance will be inaccessible until you visit a bank branch.'}
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
                  disabled={isVerifying || isMarkingAsLost}
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
                onClick={handleMarkAsLost}
                disabled={isVerifying || isMarkingAsLost || !password}
                className="px-6 py-2 bg-amber-600 hover:bg-amber-700 text-white font-semibold rounded-md shadow-md transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-amber-500 focus:ring-opacity-50 disabled:opacity-70 disabled:cursor-not-allowed"
              >
                {isVerifying ? 'Verifying...' : isMarkingAsLost ? 'Marking as Lost...' : 'Yes, Mark as Lost'}
              </button>
              <button
                onClick={() => setShowConfirmation(false)}
                disabled={isMarkingAsLost}
                className="px-4 py-2 border border-gray-300 bg-white text-gray-700 font-medium rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:opacity-50"
              >
                Cancel
              </button>
            </div>
          </div>
        ) : (
          <div className="mt-6 flex gap-3">
            <button
              onClick={handleMarkAsLost}
              className="px-6 py-2 bg-amber-600 hover:bg-amber-700 text-white font-semibold rounded-md shadow-md transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-amber-500 focus:ring-opacity-50 disabled:opacity-70 disabled:cursor-not-allowed"
              disabled={isMarkingAsLost}
            >
              Mark This Card as Lost
            </button>
            <button
              onClick={handleCancel}
              className="px-4 py-2 border border-gray-300 bg-white text-gray-700 font-medium rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:opacity-50"
              disabled={isMarkingAsLost}
            >
              Cancel
            </button>
          </div>
        )}
      </div>
    </div>
  );
};

export default LostCardPage;
