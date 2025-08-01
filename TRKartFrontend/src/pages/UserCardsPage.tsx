import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import userCardService from '@/services/userCardService';
import { UserCard } from '@/types';
import LoadingSpinner from '@/components/LoadingSpinner';

const UserCardsPage: React.FC = () => {
  const { user } = useAuth();
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

      // If no user or no customerID, try to load from localStorage
      if (!user?.customerID || user.customerID === 0) {
        const storedCards = loadCardsFromStorage();
        setCards(storedCards);
        setIsLoading(false);
        return;
      }
      
      try {
        const data = await userCardService.getCardsByCustomerId(user.customerID);
        setCards(data);
        saveCardsToStorage(data);
      } catch (err) {
        console.error('Failed to load cards:', err);
        setError('Failed to load cards.');
        // Try to load from localStorage as fallback
        const storedCards = loadCardsFromStorage();
        setCards(storedCards);
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
              <button className="btn-primary">
                Add New Card
              </button>
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
              <button className="btn-primary mt-4">Add Your First Card</button>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {cards.map((card) => (
                <div key={card.cardID} className="card">
                  <div className="flex items-center justify-between mb-4">
                    <h3 className="text-lg font-medium text-gray-900">Card #{card.cardID}</h3>
                    <div className={`px-2 py-1 text-xs font-medium rounded-full ${
                      card.status === 'Active'
                        ? 'bg-green-100 text-green-800'
                        : 'bg-red-100 text-red-800'
                    }`}>
                      {card.status}
                    </div>
                  </div>
                  
                  <div className="space-y-3">
                    <div>
                      <label className="text-sm font-medium text-gray-500">Card Number</label>
                      <p className="text-sm text-gray-900 font-mono">
                        {card.cardNumber}
                      </p>
                    </div>
                    
                    <div>
                      <label className="text-sm font-medium text-gray-500">Balance</label>
                      <p className="text-lg font-semibold text-gray-900">
                        {card.balance.toFixed(2)} TL
                      </p>
                    </div>
                    
                    <div>
                      <label className="text-sm font-medium text-gray-500">Created</label>
                      <p className="text-sm text-gray-900">
                        {new Date(card.createdAt).toLocaleDateString()}
                      </p>
                    </div>
                  </div>
                  
                  <div className="mt-6 flex space-x-2">
                    <button className="btn-primary flex-1">View Details</button>
                    <button className="btn-secondary">Delete</button>
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