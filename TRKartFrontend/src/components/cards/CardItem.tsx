import React, { useState, useRef, useEffect } from 'react';
import { UserCard } from '@/types';
import { Pencil, MoreVertical, Copy, Save } from 'lucide-react';

const CARD_TYPE_MAP = {
  0: { 
    name: 'Standard', 
    className: 'bg-gray-100 text-gray-900',
    buttonBg: 'bg-gray-400 hover:bg-gray-500',
    stripeColor: 'bg-gray-400'
  },
  1: { 
    name: 'Gold', 
    className: 'bg-yellow-100 text-yellow-900',
    buttonBg: 'bg-yellow-400 hover:bg-yellow-500',
    stripeColor: 'bg-yellow-400'
  },
  2: { 
    name: 'Platinum', 
    className: 'bg-cyan-100 text-cyan-900',
    buttonBg: 'bg-cyan-400 hover:bg-cyan-500',
    stripeColor: 'bg-cyan-400'
  }
} as const;

interface CardItemProps {
  card: UserCard;
  onNameEdit?: (cardId: number, newName: string) => void;
}

const CardItem: React.FC<CardItemProps> = ({ card, onNameEdit }) => {
  const [showCopied, setShowCopied] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [editedName, setEditedName] = useState(card.cardName || '');
  const [showSaveStatus, setShowSaveStatus] = useState(false);
  const [saveStatus, setSaveStatus] = useState<{ type: 'success' | 'error'; message: string } | null>(null);
  const editContainerRef = useRef<HTMLDivElement>(null);

  const formatCardNumber = (cardNumber: string): string => {
    if (!cardNumber) return '';
    const digits = cardNumber.replace(/\D/g, '').slice(-11);
    return `TRK90 ${digits.substring(0, 4)} ${digits.substring(4, 8)} ${digits.substring(8, 11)}`;
  };

  const formatExpirationDate = (dateString: string): string => {
    if (!dateString) return 'MM/YY';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { month: '2-digit', year: '2-digit' });
  };

  const getCleanCardNumber = (cardNumber: string): string => {
    if (!cardNumber) return '';
    const digits = cardNumber.replace(/\D/g, '').slice(-11);
    return `TRK90${digits}`;
  };

  const handleCopyCardNumber = async () => {
    const cleanNumber = getCleanCardNumber(card.cardNumber);
    try {
      await navigator.clipboard.writeText(cleanNumber);
      setShowCopied(true);
      setTimeout(() => setShowCopied(false), 2000); // Hide after 2 seconds
    } catch (err) {
      console.error('Failed to copy card number:', err);
    }
  };

  const handleEditClick = () => {
    setIsEditing(true);
    setEditedName(card.cardName || '');
  };

  const handleSaveClick = async () => {
    if (onNameEdit && editedName.trim() !== card.cardName) {
      try {
        await onNameEdit(card.cardID, editedName.trim());
        // Show success message
        setSaveStatus({ type: 'success', message: 'Card name updated!' });
        setShowSaveStatus(true);
        setTimeout(() => {
          setShowSaveStatus(false);
          setSaveStatus(null);
        }, 3000); // Hide status after 3 seconds
        setIsEditing(false);
      } catch (error) {
        // Show error message
        const errorMessage = error instanceof Error ? error.message : 'Failed to save name';
        setSaveStatus({ type: 'error', message: errorMessage });
        setShowSaveStatus(true);
        setTimeout(() => {
          setShowSaveStatus(false);
          setSaveStatus(null);
        }, 5000); // Show error longer (5 seconds)
        // Don't exit edit mode on error so user can try again
      }
    } else {
      // No changes made
      setSaveStatus({ type: 'error', message: 'No changes to save' });
      setShowSaveStatus(true);
      setTimeout(() => {
        setShowSaveStatus(false);
        setSaveStatus(null);
      }, 3000);
      setIsEditing(false);
    }
  };

  const handleCancelEdit = () => {
    setIsEditing(false);
    setEditedName(card.cardName || '');
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleSaveClick();
    } else if (e.key === 'Escape') {
      handleCancelEdit();
    }
  };

  // Handle clicks outside the edit container
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (editContainerRef.current && !editContainerRef.current.contains(event.target as Node)) {
        handleCancelEdit();
      }
    };

    if (isEditing) {
      document.addEventListener('mousedown', handleClickOutside);
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isEditing]);

  return (
    <div className="relative rounded-xl overflow-hidden shadow-lg h-48 w-80 bg-gradient-to-br from-gray-800 to-gray-900 text-white p-5 flex flex-col">
      {/* Diagonal stripe reflecting card type - Layer 1 */}
      <div 
        className={`absolute bottom-60 left-24 w-8 h-80 ${CARD_TYPE_MAP[card.cardType as keyof typeof CARD_TYPE_MAP]?.stripeColor || 'bg-gray-400'}`}
        style={{
          transform: 'rotate(120deg)',
          transformOrigin: 'bottom left',
          zIndex: 1
        }}
      />
      
      {/* Card Header - Layer 2 */}
      <div className="flex justify-between items-start mb-6 relative z-10">
        <div className="flex-1">
          <div className="flex items-center gap-2">
            {isEditing ? (
              <div ref={editContainerRef} className="flex items-center gap-2">
                <input
                  type="text"
                  value={editedName}
                  onChange={(e) => {
                    const value = e.target.value;
                    if (value.length <= 16) {
                      setEditedName(value);
                    }
                  }}
                  onKeyDown={handleKeyPress}
                  className="bg-gray-700 text-white text-lg font-semibold px-2 py-0.5 rounded border border-gray-600 focus:outline-none focus:border-gray-400 w-40"
                  placeholder="Name your card"
                  maxLength={16}
                  autoFocus
                />
                <button 
                  onClick={handleSaveClick}
                  className="text-gray-300 hover:text-white transition-colors"
                  aria-label="Save card name"
                >
                  <Save size={16} />
                </button>
                {/* Save Status Message */}
                {showSaveStatus && (
                  <div className={`absolute -top-10 left-1/2 transform -translate-x-1/2 text-xs px-2 py-1 rounded shadow-lg z-20 animate-fade-in ${
                    saveStatus?.type === 'success' 
                      ? 'bg-green-500 text-white' 
                      : 'bg-red-500 text-white'
                  }`}>
                    {saveStatus?.message}
                  </div>
                )}
              </div>
            ) : (
              <div className="flex items-center gap-2">
                <h3 className="text-lg font-semibold">
                  {card.cardName || 'Name your card'}
                </h3>
                <button 
                  onClick={handleEditClick}
                  className="text-gray-300 hover:text-white transition-colors"
                  aria-label="Edit card name"
                >
                  <Pencil size={16} />
                </button>
              </div>
            )}
          </div>
          <p className="text-sm text-gray-300">Virtual Card</p>
        </div>
        
        <span className={`px-2 py-1 rounded text-xs font-medium ${
          CARD_TYPE_MAP[card.cardType as keyof typeof CARD_TYPE_MAP]?.className || 'bg-gray-100 text-gray-900'
        }`}>
          {CARD_TYPE_MAP[card.cardType as keyof typeof CARD_TYPE_MAP]?.name || 'Standard'}
        </span>
      </div>
      
      {/* Card Number with Copy Icon - Layer 2 */}
      <div className="mt-6 mb-4 relative z-10">
        <div className="flex items-center gap-2">
          <p className="text-lg font-mono tracking-wider">
            {formatCardNumber(card.cardNumber)}
          </p>
          <div className="relative">
            <button
              onClick={handleCopyCardNumber}
              className="text-gray-300 hover:text-white transition-colors p-1 rounded hover:bg-gray-700"
              aria-label="Copy card number"
              title="Copy card number"
            >
              <Copy size={16} />
            </button>
            {/* Floating "Copied!" notification */}
            {showCopied && (
              <div className="absolute -top-8 left-1/2 transform -translate-x-1/2 bg-green-500 text-white text-xs px-2 py-1 rounded shadow-lg z-20 animate-fade-in">
                Copied!
              </div>
            )}
          </div>
        </div>
      </div>
      
      {/* Expiration - Layer 2 */}
      <div className="-mt-4 mb-2 relative z-10">
        <p className="text-xs text-gray-400">Expiration Date</p>
        <p className="text-sm">{formatExpirationDate(card.cardExpirationDate)}</p>
      </div>
      
      {/* Three dots menu with dynamic colors based on card type - Layer 2 */}
      <div className="absolute bottom-4 right-4 z-10">
        <button 
          className={`rounded-full p-1 ${CARD_TYPE_MAP[card.cardType as keyof typeof CARD_TYPE_MAP]?.buttonBg} transition-colors duration-200`}
        >
          <MoreVertical className="w-5 h-5 text-white" />
        </button>
      </div>
    </div>
  );
};

export default CardItem;
