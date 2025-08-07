import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import userCardService from '@/services/userCardService';
import { UserCard } from '@/types';
import LoadingSpinner from '@/components/LoadingSpinner';

// Format card number as TRK90 XXXX XXXX XXX
const formatCardNumber = (cardNumber: string): string => {
  if (!cardNumber) return '';
  // Remove any non-digit characters and take last 11 digits
  const digits = cardNumber.replace(/\D/g, '').slice(-11);
  // Format as TRK90 XXXX XXXX XXX
  return `TRK90 ${digits.substring(0, 4)} ${digits.substring(4, 8)} ${digits.substring(8, 11)}`;
};

const UserCardsPage: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [cards, setCards] = useState<UserCard[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  // Helper function to save cards to localStorage (user-specific)
  const saveCardsToStorage = (cards: UserCard[]) => {
    try {
      const userKey = user?.email ? `trkart_cards_${user.email}` : 'trkart_cards_anonymous';
      localStorage.setItem(userKey, JSON.stringify(cards));
    } catch (error) {
      console.error('Failed to save cards to localStorage:', error);
    }
  };

  // Helper function to load cards from localStorage (user-specific)
  const loadCardsFromStorage = (): UserCard[] => {
    try {
      const userKey = user?.email ? `trkart_cards_${user.email}` : 'trkart_cards_anonymous';
      const stored = localStorage.getItem(userKey);
      return stored ? JSON.parse(stored) : [];
    } catch (error) {
      console.error('Failed to load cards from localStorage:', error);
      return [];
    }
  };

  useEffect(() => {
    const fetchCards = async () => {
      // If no user email, show empty state
      if (!user?.email) {
        setCards([]);
        setIsLoading(false);
        return;
      }

      try {
        let cardsData: UserCard[] = [];
        
        // Try to fetch from API first if we have a customerID
        if (user?.customerID && user.customerID !== 0) {
          try {
            cardsData = await userCardService.getCardsByCustomerId(user.customerID);
            // Ensure each card has a cardStatus, default to 'Inactive' if not provided
            cardsData = cardsData.map(card => ({
              ...card,
              cardStatus: card.cardStatus || 'Inactive'
            }));
            saveCardsToStorage(cardsData);
          } catch (err) {
            console.error('Failed to load cards from API:', err);
            // If API fails, try to load from localStorage
            const storedCards = loadCardsFromStorage();
            if (storedCards.length > 0) {
              cardsData = storedCards;
            } else {
              setError('Failed to load cards. Using cached data if available.');
            }
          }
        } else {
          // If no customerID, try to load from localStorage
          const storedCards = loadCardsFromStorage();
          if (storedCards.length > 0) {
            cardsData = storedCards;
          }
        }
        
        setCards(cardsData);
      } catch (err) {
        console.error('Unexpected error loading cards:', err);
        setError('An unexpected error occurred while loading cards.');
      } finally {
        setIsLoading(false);
      }
    };

    fetchCards();
  }, [user]);

  if (isLoading) {
    return <LoadingSpinner />;
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center">
              <Link to="/dashboard" className="text-gray-600 hover:text-gray-900 mr-4">
                ← Back to Dashboard
              </Link>
              <h1 className="text-xl font-semibold text-gray-900">My Cards</h1>
            </div>
            <div className="flex items-center">
              <Link to="/add-card" className="btn-primary">
                Add New Card
              </Link>
            </div>
          </div>
        </div>
      </nav>

      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 py-6 sm:px-0">
          {/* User Status */}
          {user?.email && (
            <div className="bg-blue-50 border border-blue-200 text-blue-700 px-4 py-2 rounded mb-4 text-sm">
              <div className="flex items-center justify-between">
                <span>💳 Cards are saved locally for this user</span>
                <span className="text-xs bg-blue-100 px-2 py-1 rounded">
                  👤 {user.email}
                </span>
              </div>
            </div>
          )}

          {error && (
            <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
              {error}
            </div>
          )}

          {cards.length === 0 ? (
            <div className="text-center py-12">
              <h3 className="text-lg font-medium text-gray-900 mb-2">No cards found</h3>
              <p className="text-gray-600">You haven't added any cards yet.</p>
              <Link to="/add-card" className="btn-primary mt-4 inline-block">Add Your First Card</Link>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {cards.map((card) => (
                <div key={card.cardID} className="card">
                  <div className="space-y-4">
                    <div>
                      <label className="text-sm font-medium text-gray-500">Card Number</label>
                      <p className="text-base font-mono font-semibold text-gray-900 tracking-wider">
                        {formatCardNumber(card.cardNumber)}
                      </p>
                    </div>
                    
                    <div className="flex items-end justify-between">
                      <div>
                        <label className="text-sm font-medium text-gray-500">Balance</label>
                        <p className="text-xl font-bold text-gray-900">
                          {card.balance.toFixed(2)} TL
                        </p>
                        <div className="mt-2">
                          <div className="text-sm font-medium text-gray-500 mb-1">Card Status</div>
                          <span className={`inline-flex items-center px-3 py-1.5 rounded-md text-sm font-medium ${
                            card.cardStatus === 'Active'
                              ? 'bg-green-50 text-green-700 border border-green-200'
                              : card.cardStatus === 'Lost'
                                ? 'bg-red-50 text-red-700 border border-red-200'
                                : 'bg-yellow-50 text-yellow-700 border border-yellow-200'
                          }`}>
                            {card.cardStatus}
                          </span>
                        </div>
                      </div>
                    </div>
                    
                    <div>
                      <label className="text-sm font-medium text-gray-500">Created</label>
                      <p className="text-sm text-gray-900">
                        {new Date(card.createdAt).toLocaleDateString()}
                      </p>
                    </div>
                  </div>
                  
                  <div className="space-y-2">
                    <button
                      onClick={(e) => {
                        if (card.cardStatus !== 'Lost') {
                          e.preventDefault();
                          navigate(`/new-transaction`);
                        }
                      }}
                      disabled={card.cardStatus === 'Lost'}
                      className={`w-full px-4 py-2 font-semibold rounded-md shadow-md text-center transition-colors duration-200 focus:outline-none focus:ring-2 whitespace-nowrap overflow-hidden text-ellipsis ${
                        card.cardStatus === 'Lost'
                          ? 'bg-gray-300 text-gray-500 cursor-not-allowed'
                          : 'bg-yellow-500 hover:bg-yellow-600 text-white focus:ring-yellow-500 focus:ring-opacity-50'
                      }`}
                      title={card.cardStatus === 'Lost' ? 'You cannot add balance to a lost card' : ''}
                    >
                      {card.cardStatus === 'Lost' ? 'Cannot Add Balance (Card Lost)' : 'Add Balance'}
                    </button>
                    
                    <div className="flex gap-2">
                      <button
                        onClick={(e) => {
                          e.preventDefault();
                          navigate(`/transactions?filterType=cardID&selectedCardID=${card.cardID}`);
                        }}
                        className="flex-1 px-4 py-2 bg-yellow-500 hover:bg-yellow-600 text-white font-semibold rounded-md shadow-md text-center transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-yellow-500 focus:ring-opacity-50 whitespace-nowrap overflow-hidden text-ellipsis"
                      >
                        Transactions
                      </button>
                      <button
                        onClick={(e) => {
                          e.preventDefault();
                          navigate(`/delete-card/${card.cardID}`);
                        }}
                        className="flex-1 px-4 py-2 bg-red-500 hover:bg-red-600 text-white font-semibold rounded-md shadow-md text-center transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-opacity-50 whitespace-nowrap overflow-hidden text-ellipsis"
                      >
                        Delete Card
                      </button>
                    </div>
                    
                    <button
                      onClick={(e) => {
                        e.preventDefault();
                        if (card.cardStatus !== 'Lost') {
                          navigate(`/lost-card/${card.cardID}`);
                        }
                      }}
                      disabled={card.cardStatus === 'Lost'}
                      className={`w-full px-4 py-2 font-semibold rounded-md shadow-md text-center transition-colors duration-200 focus:outline-none focus:ring-2 whitespace-nowrap overflow-hidden text-ellipsis ${
                        card.cardStatus === 'Lost'
                          ? 'bg-gray-300 text-gray-500 cursor-not-allowed'
                          : 'bg-yellow-500 hover:bg-yellow-600 text-white focus:ring-yellow-500 focus:ring-opacity-50'
                      }`}
                      title={card.cardStatus === 'Lost' ? 'This card is already marked as lost' : 'Report this card as lost'}
                    >
                      {card.cardStatus === 'Lost' ? 'Card Marked as Lost' : 'Did You Lose Your Card?'}
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </main>
    </div>
  );
};

export default UserCardsPage; 