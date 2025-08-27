import React from 'react';
import { UserCard } from '@/types';
import { Pencil, MoreVertical } from 'lucide-react';

const CARD_TYPE_MAP = {
  0: { 
    name: 'Standard', 
    className: 'bg-gray-100 text-gray-900',
    buttonBg: 'bg-gray-600 hover:bg-gray-500'
  },
  1: { 
    name: 'Gold', 
    className: 'bg-yellow-100 text-yellow-900',
    buttonBg: 'bg-yellow-600 hover:bg-yellow-500'
  },
  2: { 
    name: 'Platinum', 
    className: 'bg-cyan-100 text-cyan-900',
    buttonBg: 'bg-cyan-600 hover:bg-cyan-500'
  }
} as const;

interface CardItemProps {
  card: UserCard;
  onNameEdit?: () => void;
}

const CardItem: React.FC<CardItemProps> = ({ card, onNameEdit }) => {
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

  return (
    <div className="relative rounded-xl overflow-hidden shadow-lg h-48 w-80 bg-gradient-to-br from-gray-800 to-gray-900 text-white p-5 flex flex-col">
      {/* Card Header */}
      <div className="flex justify-between items-start mb-6">
        <div className="flex-1">
          <div className="flex items-center gap-2">
            <h3 className="text-lg font-semibold">
              {card.cardName || 'Name your card'}
            </h3>
            <button 
              onClick={onNameEdit}
              className="text-gray-300 hover:text-white transition-colors"
              aria-label="Edit card name"
            >
              <Pencil size={16} />
            </button>
          </div>
          <p className="text-sm text-gray-300">Virtual Card</p>
        </div>
        
        <span className={`px-2 py-1 rounded text-xs font-medium ${
          CARD_TYPE_MAP[card.cardType as keyof typeof CARD_TYPE_MAP]?.className || 'bg-gray-100 text-gray-900'
        }`}>
          {CARD_TYPE_MAP[card.cardType as keyof typeof CARD_TYPE_MAP]?.name || 'Standard'}
        </span>
      </div>
      
      {/* Card Number - Label removed and moved down */}
      <div className="mt-6 mb-4">
        <p className="text-lg font-mono tracking-wider">
          {formatCardNumber(card.cardNumber)}
        </p>
      </div>
      
      {/* Expiration - Moved up with negative margin */}
      <div className="-mt-4 mb-2">
        <p className="text-xs text-gray-400">Expiration Date</p>
        <p className="text-sm">{formatExpirationDate(card.cardExpirationDate)}</p>
      </div>
      
      {/* Three dots menu with dynamic colors based on card type */}
      <div className="absolute bottom-4 right-4">
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
