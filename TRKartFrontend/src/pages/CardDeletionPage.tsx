import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import userCardService from '@/services/userCardService';
import { UserCard, DeleteUserCardRequest } from '@/types';
import LoadingSpinner from '@/components/LoadingSpinner';

const CardDeletionPage: React.FC = () => {
  const { cardId } = useParams<{ cardId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();

  
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [card, setCard] = useState<UserCard | null>(null);
  const [showConfirmation, setShowConfirmation] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  // Load card details
  useEffect(() => {
    const fetchCardDetails = async () => {
      if (!cardId || !user?.customerID) {
        setError('Invalid card or user information.');
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
            setCard(foundCard);
            setIsLoading(false);
            return;
          }
        }

        // If not found in localStorage, try to fetch from API
        const cardNumber = await getCardNumberFromId(parseInt(cardId));
        if (cardNumber) {
          const cardDetails = await userCardService.getUserCardByNumber(cardNumber);
          setCard(cardDetails);
        } else {
          setError('Card not found.');
        }
      } catch (err) {
        console.error('Error fetching card details:', err);
        setError('Failed to load card details. Please try again.');
      } finally {
        setIsLoading(false);
      }
    };

    fetchCardDetails();
  }, [cardId, user]);

  const getCardNumberFromId = async (cardId: number): Promise<string | null> => {
    try {
      // Try to get the card by ID from the API
      const response = await userCardService.getUserCards();
      const foundCard = response.find(card => card.cardID === cardId);
      return foundCard ? foundCard.cardNumber : null;
    } catch (err) {
      console.error('Error fetching card number:', err);
      return null;
    }
  };

  const handleDelete = async (): Promise<void> => {
    if (!card) return;
    
    if (!showConfirmation) {
      setShowConfirmation(true);
      return;
    }

    setIsDeleting(true);
    try {
      // Create a proper DeleteUserCardRequest object
      const deleteRequest: DeleteUserCardRequest = { 
        cardNumber: card.cardNumber 
      };
      
      await userCardService.deleteUserCard(deleteRequest);
      
      // Remove from localStorage
      if (user?.email) {
        const userKey = `trkart_cards_${user.email}`;
        const storedCards = localStorage.getItem(userKey);
        if (storedCards) {
          const cards: UserCard[] = JSON.parse(storedCards);
          const updatedCards = cards.filter(c => c.cardID !== card.cardID);
          localStorage.setItem(userKey, JSON.stringify(updatedCards));
        }
      }
      navigate('/cards');
    } catch (err) {
      console.error('Error deleting card:', err);
      setError('Failed to delete card. Please try again.');
      setIsDeleting(false);
      setShowConfirmation(false);
    }
  };

  const handleTransferFunds = (): void => {
    if (!card) return;
    navigate('/new-transaction', { 
      state: { 
        fromCardNumber: card.cardNumber,
        amount: card.balance,
        transferMode: true
      } 
    });
  };

  const handleCancel = (): void => {
    navigate('/cards');
  };

  if (isLoading) {
    return <LoadingSpinner />;
  }

  if (error || !card) {
    return (
      <div className="min-h-screen bg-gray-50 p-6">
        <div className="max-w-2xl mx-auto bg-white p-6 rounded-lg shadow">
          <h1 className="text-2xl font-bold text-red-600 mb-4">Error</h1>
          <p className="mb-4">{error || 'Card not found.'}</p>
          <button
            onClick={handleCancel}
            className="btn-secondary"
          >
            Back to Cards
          </button>
        </div>
      </div>
    );
  }

  const hasBalance = card.balance > 0;

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-2xl mx-auto bg-white p-6 rounded-lg shadow">
        <h1 className="text-2xl font-bold text-red-600 mb-6">Delete Card</h1>
        
        <div className="mb-6 p-4 border border-red-200 bg-red-50 rounded">
          <h2 className="font-semibold text-red-700 mb-2">Warning</h2>
          <p className="text-red-700">
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
          <div className="mb-6 p-4 border border-yellow-200 bg-yellow-50 rounded">
            <h2 className="font-semibold text-yellow-700 mb-2">Balance Warning</h2>
            <p className="text-yellow-700 mb-4">
              This card has a balance of {card.balance.toFixed(2)} TL. 
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

        {showConfirmation ? (
          <div className="mt-6 p-4 border border-red-200 bg-red-50 rounded">
            <h2 className="font-semibold text-red-700 mb-4">Are you sure you want to delete this card?</h2>
            <p className="text-red-700 mb-4">
              This action cannot be undone. All transaction history for this card will be permanently deleted.
              {hasBalance && ' Any remaining balance will be lost.'}
            </p>
            <div className="flex gap-3">
              <button
                onClick={handleDelete}
                disabled={isDeleting}
                className="px-6 py-2 bg-red-600 hover:bg-red-700 text-white font-semibold rounded-md shadow-md transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-opacity-50 disabled:opacity-70 disabled:cursor-not-allowed"
              >
                {isDeleting ? 'Deleting...' : 'Yes, Delete Permanently'}
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
              onClick={handleDelete}
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
