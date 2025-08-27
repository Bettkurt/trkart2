import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import userCardService from '@/services/userCardService';
import { UserCard } from '@/types';
import { CardStatus } from '@/types/cardStatus';
import LoadingSpinner from '@/components/LoadingSpinner';
import { logger } from '@/utils/logger';

const CARD_STATUS_MAP = {
  [CardStatus.Deactivated]: { label: 'Deactivated', className: 'bg-red-50 text-red-700 border border-red-200' },
  [CardStatus.Expired]: { label: 'Expired', className: 'bg-gray-50 text-gray-700 border border-gray-200' },
  [CardStatus.Lost]: { label: 'Lost', className: 'bg-red-50 text-red-700 border border-red-200' },
  [CardStatus.Inactive]: { label: 'Inactive', className: 'bg-yellow-50 text-yellow-700 border border-yellow-200' },
  [CardStatus.Active]: { label: 'Active', className: 'bg-green-50 text-green-700 border border-green-200' },
  default: { label: 'Inactive', className: 'bg-yellow-50 text-yellow-700 border border-yellow-200' }
} as const;

const CARD_TYPE_MAP = {
  0: { name: 'Standard', className: 'bg-gray-100 text-gray-900 border border-gray-300' }, // Light gray background, black text
  1: { name: 'Gold', className: 'bg-yellow-100/80 text-gray-900 border border-yellow-200' }, // Opaque gold background, black text
  2: { name: 'Platinum', className: 'bg-cyan-100/80 text-gray-900 border border-cyan-200' } // Opaque turquoise background, black text
} as const;

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
    const userKey = user?.email ? `trkart_cards_${user.email}` : 'trkart_cards_anonymous';
    try {
      localStorage.setItem(userKey, JSON.stringify(cards));
      logger.info('UserCardsPage', 'saveCardsToStorage', `Successfully saved ${cards.length} cards to localStorage`, { userKey, cardCount: cards.length });
    } catch (error) {
      logger.error('UserCardsPage', 'saveCardsToStorage', 'Failed to save cards to localStorage', error as Error, { userKey });
    }
  };

  // Helper function to load cards from localStorage (user-specific)
  const loadCardsFromStorage = (): UserCard[] => {
    const userKey = user?.email ? `trkart_cards_${user.email}` : 'trkart_cards_anonymous';
    try {
      const stored = localStorage.getItem(userKey);
      const cards = stored ? JSON.parse(stored) : [];
      logger.debug('UserCardsPage', 'loadCardsFromStorage', `Loaded ${cards.length} cards from localStorage`, { userKey, cardCount: cards.length });
      return cards;
    } catch (error) {
      logger.error('UserCardsPage', 'loadCardsFromStorage', 'Failed to load cards from localStorage', error as Error, { userKey });
      return [];
    }
  };

  useEffect(() => {
    logger.info('UserCardsPage', 'mount', 'Component mounted', { hasUser: !!user, userId: user?.customerID });
    
    const fetchCards = async () => {
      // If no user email, show empty state
      if (!user?.email) {
        logger.debug('UserCardsPage', 'fetchCards', 'No user email found, showing empty state');
        setCards([]);
        setIsLoading(false);
        return;
      }

      try {
        let cardsData: UserCard[] = [];
        
        // Try to fetch from API first if we have a customerID
        if (user?.customerID && user.customerID !== 0) {
          try {
            logger.debug('UserCardsPage', 'fetchCards', 'Fetching cards from secure API');
            cardsData = await userCardService.getUserCards();
            
            // Log API response
            logger.info('UserCardsPage', 'fetchCards', `Successfully fetched ${cardsData.length} cards from secure API`, { 
              cardCount: cardsData.length 
            });
            
            // Ensure each card has a cardStatus, default to Inactive (3) if not provided
            cardsData = cardsData.map(card => ({
              ...card,
              cardStatus: card.cardStatus || CardStatus.Inactive
            }));
            
            saveCardsToStorage(cardsData);
          } catch (err) {
            logger.error('UserCardsPage', 'fetchCards', 'Failed to load cards from API', err as Error, { 
              customerId: user.customerID 
            });
            
            // If API fails, try to load from localStorage
            const storedCards = loadCardsFromStorage();
            if (storedCards.length > 0) {
              logger.info('UserCardsPage', 'fetchCards', 'Falling back to cached cards from localStorage', { 
                cardCount: storedCards.length 
              });
              cardsData = storedCards;
            } else {
              const errorMsg = 'Failed to load cards. Using cached data if available.';
              logger.warn('UserCardsPage', 'fetchCards', errorMsg, { customerId: user.customerID });
              setError(errorMsg);
            }
          }
        } else {
          logger.debug('UserCardsPage', 'fetchCards', 'No customerID found, checking localStorage');
          // If no customerID, try to load from localStorage
          const storedCards = loadCardsFromStorage();
          if (storedCards.length > 0) {
            logger.info('UserCardsPage', 'fetchCards', 'Using cards from localStorage', { 
              cardCount: storedCards.length 
            });
            cardsData = storedCards;
          }
        }
        
        // Sort cards: Active (4) → Inactive (3) → Lost (2), then by cardID within each group
        const sortedCards = [...cardsData].sort((a, b) => {
          // First sort by status in reverse order (4 > 3 > 2)
          if (a.cardStatus !== b.cardStatus) {
            return b.cardStatus - a.cardStatus;
          }
          // Then by cardID for same status
          return a.cardID - b.cardID;
        });
        
        logger.debug('UserCardsPage', 'fetchCards', `Setting ${sortedCards.length} sorted cards to state`, { 
          cardCount: sortedCards.length,
          cards: sortedCards.map(c => ({
            id: c.cardID,
            status: c.cardStatus,
            number: c.cardNumber
          }))
        });
        setCards(sortedCards);
      } catch (err) {
        const errorMsg = 'An unexpected error occurred while loading cards.';
        logger.error('UserCardsPage', 'fetchCards', errorMsg, err as Error);
        setError(errorMsg);
      } finally {
        setIsLoading(false);
      }
    };

    fetchCards();
    
    // Cleanup function
    return () => {
      logger.debug('UserCardsPage', 'unmount', 'Component unmounting');
    };
  }, [user]);

  if (isLoading) {
    logger.debug('UserCardsPage', 'render', 'Rendering loading spinner');
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
              <Link to="/create-card" className="btn-primary">
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
              <Link to="/create-card" className="btn-primary mt-4 inline-block">Add Your First Card</Link>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {cards.map((card) => (
                <div key={card.cardID} className="card">
                  <div className="space-y-4">
                    <div>
                      <div className="flex justify-between items-start">
                        <div>
                          <label className="text-sm font-medium text-gray-500">Card Number</label>
                          <p className="text-base font-mono font-semibold text-gray-900 tracking-wider">
                            {formatCardNumber(card.cardNumber)}
                          </p>
                        </div>
                        <div className="mt-5">
                          <span className={`inline-flex items-center px-3 py-1 rounded-md text-sm font-medium ${
                            CARD_TYPE_MAP[card.cardType as keyof typeof CARD_TYPE_MAP]?.className || 'bg-gray-100 text-gray-800'
                          }`}>
                            {CARD_TYPE_MAP[card.cardType as keyof typeof CARD_TYPE_MAP]?.name || 'Standard'}
                          </span>
                        </div>
                      </div>
                    </div>
                    
                    <div className="flex items-end justify-between">
                      <div>
                        <label className="text-sm font-medium text-gray-500">Balance</label>
                        <p className="text-xl font-bold text-gray-900">
                          ₺{card.balance.toFixed(2)}
                        </p>
                        <div className="mt-2">
                          <div className="text-sm font-medium text-gray-500 mb-1">Card Status</div>
                          <span className={`inline-flex items-center px-3 py-1.5 rounded-md text-sm font-medium ${(CARD_STATUS_MAP[card.cardStatus as keyof typeof CARD_STATUS_MAP] || CARD_STATUS_MAP.default).className}`}>
                            {(CARD_STATUS_MAP[card.cardStatus as keyof typeof CARD_STATUS_MAP] || CARD_STATUS_MAP.default).label}
                          </span>
                        </div>
                      </div>
                    </div>
                    
                    <div>
                      <label className="text-sm font-medium text-gray-500">Expires At</label>
                      <p className="text-sm text-gray-900">
                        {card.cardExpirationDate ? new Date(card.cardExpirationDate).toLocaleDateString('en-US', { month: '2-digit', year: '2-digit' }) : 'N/A'}
                      </p>
                    </div>
                  </div>
                  
                  <div className="space-y-3 mt-4">
                    {/* Add Balance Button */}
                    <button
                      onClick={(e) => {
                        if ([CardStatus.Active, CardStatus.Inactive].includes(card.cardStatus)) {
                          e.preventDefault();
                          logger.info('UserCardsPage', 'addBalance', 'Navigating to new transaction page', { 
                            cardId: card.cardID,
                            cardNumber: card.cardNumber 
                          });
                          navigate(`/new-transaction?cardId=${card.cardID}`);
                        } else {
                          logger.debug('UserCardsPage', 'addBalance', `Attempted to add balance to ${card.cardStatus} card`, { 
                            cardId: card.cardID,
                            cardStatus: card.cardStatus 
                          });
                        }
                      }}
                      disabled={[CardStatus.Lost, CardStatus.Expired, CardStatus.Deactivated].includes(card.cardStatus)}
                      className={`w-full px-4 py-2 font-semibold rounded-md shadow-md text-center transition-colors duration-200 focus:outline-none focus:ring-2 whitespace-nowrap overflow-hidden text-ellipsis ${
                        [CardStatus.Lost, CardStatus.Expired, CardStatus.Deactivated].includes(card.cardStatus)
                          ? 'bg-gray-300 text-gray-500 cursor-not-allowed'
                          : 'bg-yellow-400 hover:bg-yellow-600 text-white focus:ring-yellow-500 focus:ring-opacity-50'
                      }`}
                      title={[CardStatus.Lost, CardStatus.Expired, CardStatus.Deactivated].includes(card.cardStatus) 
                        ? `You cannot add balance to a ${card.cardStatus} card` 
                        : ''}
                    >
                      {[CardStatus.Lost, CardStatus.Expired, CardStatus.Deactivated].includes(card.cardStatus) 
                        ? `Cannot Add Balance (${(CARD_STATUS_MAP[card.cardStatus as keyof typeof CARD_STATUS_MAP] || CARD_STATUS_MAP.default).label})` 
                        : 'Add Balance'}
                    </button>

                    {/* New Transfer Button */}
                    <button
                      onClick={(e) => {
                        if (card.cardStatus === CardStatus.Active) {  // Only allow active cards (status 4)
                          e.preventDefault();
                          logger.info('UserCardsPage', 'newTransfer', 'Navigating to new transfer page', { 
                            cardId: card.cardID,
                            cardNumber: card.cardNumber 
                          });
                          navigate(`/new-transfer?fromCardId=${card.cardID}`);
                        } else {
                          e.preventDefault();
                          logger.debug('UserCardsPage', 'newTransfer', `Attempted to transfer from card with status ${card.cardStatus}`, { 
                            cardId: card.cardID,
                            cardStatus: card.cardStatus 
                          });
                        }
                      }}
                      disabled={card.cardStatus !== CardStatus.Active}  // Only enable for card's with status Active (4)
                      className={`w-full px-4 py-2 font-semibold rounded-md shadow-md text-center transition-colors duration-200 focus:outline-none focus:ring-2 whitespace-nowrap overflow-hidden text-ellipsis ${
                        card.cardStatus !== CardStatus.Active
                          ? 'bg-gray-300 text-gray-500 cursor-not-allowed'
                          : 'bg-yellow-400 hover:bg-yellow-600 text-white focus:ring-yellow-600 focus:ring-opacity-50'
                      }`}
                      title={card.cardStatus !== CardStatus.Active 
                        ? `You cannot transfer from a ${(CARD_STATUS_MAP[card.cardStatus as keyof typeof CARD_STATUS_MAP] || CARD_STATUS_MAP.default).label || 'non-active'} card` 
                        : 'Make a new transfer from this card'}
                    >
                      {card.cardStatus !== CardStatus.Active 
                        ? `Cannot Transfer (${(CARD_STATUS_MAP[card.cardStatus as keyof typeof CARD_STATUS_MAP] || CARD_STATUS_MAP.default).label})` 
                        : 'New Transfer'}
                    </button>

                    {/* Transactions Button */}
                    <button
                      onClick={(e) => {
                        e.preventDefault();
                        logger.info('UserCardsPage', 'viewTransactions', 'Navigating to transactions page', { 
                          cardId: card.cardID,
                          cardNumber: card.cardNumber 
                        });
                        navigate(`/transactions?filterType=cardID&selectedCardID=${card.cardID}`);
                      }}
                      className="w-full px-4 py-2 bg-yellow-400 hover:bg-yellow-600 text-white font-semibold rounded-md shadow-md text-center transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-yellow-500 focus:ring-opacity-50 whitespace-nowrap overflow-hidden text-ellipsis"
                    >
                      Transactions
                    </button>

                    {/* Lost Card and Delete Card Buttons */}
                    <div className="flex gap-2 pt-1">
                      <button
                        onClick={(e) => {
                          e.preventDefault();
                          if (![CardStatus.Lost, CardStatus.Deactivated].includes(card.cardStatus)) {
                            logger.info('UserCardsPage', 'reportLostCard', 'Navigating to report lost card page', { 
                              cardId: card.cardID,
                              cardNumber: card.cardNumber,
                              currentStatus: card.cardStatus
                            });
                            navigate(`/cards/lost/${card.cardID}`);
                          } else {
                            logger.debug('UserCardsPage', 'reportLostCard', `Attempted to report ${card.cardStatus} card as lost`, { 
                              cardId: card.cardID,
                              cardStatus: card.cardStatus 
                            });
                          }
                        }}
                        disabled={[CardStatus.Lost, CardStatus.Deactivated].includes(card.cardStatus)}
                        className={`flex-1 px-3 py-1.5 text-sm font-semibold rounded-md shadow-md text-center transition-colors duration-200 focus:outline-none focus:ring-2 whitespace-nowrap overflow-hidden text-ellipsis ${
                          [CardStatus.Lost, CardStatus.Deactivated].includes(card.cardStatus)
                            ? 'bg-gray-300 text-gray-500 cursor-not-allowed'
                            : 'bg-yellow-400 hover:bg-yellow-600 text-white focus:ring-yellow-500 focus:ring-opacity-50'
                        }`}
                        title={[CardStatus.Lost, CardStatus.Deactivated].includes(card.cardStatus) 
                          ? `This card is already ${CARD_STATUS_MAP[card.cardStatus as keyof typeof CARD_STATUS_MAP]?.label.toLowerCase() || 'in an invalid state'}` 
                          : 'Report this card as lost'}
                      >
                        {[CardStatus.Deactivated, CardStatus.Expired, CardStatus.Lost].includes(card.cardStatus)
                          ? `${card.cardStatus === CardStatus.Lost ? 'Marked as' : 'Card is'} ${(CARD_STATUS_MAP[card.cardStatus as keyof typeof CARD_STATUS_MAP] || CARD_STATUS_MAP.default).label}`
                          : 'Lost Card?'}
                      </button>
                      
                      <button
                        onClick={(e) => {
                          e.preventDefault();
                          if (card.cardStatus !== CardStatus.Deactivated) {
                            logger.info('UserCardsPage', 'deleteCard', 'Navigating to delete card page', { 
                              cardId: card.cardID,
                              cardNumber: card.cardNumber,
                              status: card.cardStatus
                            });
                            navigate(`/delete-card/${card.cardID}`);
                          } else {
                            logger.debug('UserCardsPage', 'deleteCard', 'Attempted to navigate to delete card page for deactivated card', {
                              cardId: card.cardID,
                              status: card.cardStatus
                            });
                          }
                        }}
                        disabled={card.cardStatus === CardStatus.Deactivated}
                        className={`flex-1 px-3 py-1.5 text-sm font-semibold rounded-md shadow-md text-center transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-opacity-50 whitespace-nowrap overflow-hidden text-ellipsis ${
                          card.cardStatus === CardStatus.Deactivated
                            ? 'bg-gray-300 text-gray-500 cursor-not-allowed focus:ring-gray-400'
                            : 'bg-red-500 hover:bg-red-600 text-white focus:ring-red-500'
                        }`}
                        title={card.cardStatus === CardStatus.Deactivated ? 'Cannot delete a deactivated card' : 'Delete this card'}
                      >
                        Delete Card
                      </button>
                    </div>
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