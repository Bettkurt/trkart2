import React, { useState, useRef, useEffect } from 'react';
import { UserCard } from '@/types';
import { MoreVertical, Copy, Save, ArrowRightLeft, Trash2, ArrowUpRight, ShieldAlert, PenLine } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { CardStatus } from '@/types/cardStatus';

const CARD_TYPE_MAP = {
  0: { 
    name: 'Standard', 
    className: 'bg-gray-100 text-gray-900',
    buttonBg: 'bg-gray-400 hover:bg-gray-500',
    stripeColor: 'bg-gray-400',
    hoverBg: 'hover:bg-gray-400'
  },
  1: { 
    name: 'Gold', 
    className: 'bg-yellow-100 text-yellow-900',
    buttonBg: 'bg-yellow-400 hover:bg-yellow-500',
    stripeColor: 'bg-yellow-400',
    hoverBg: 'hover:bg-yellow-400'
  },
  2: { 
    name: 'Platinum', 
    className: 'bg-cyan-100 text-cyan-900',
    buttonBg: 'bg-cyan-400 hover:bg-cyan-500',
    stripeColor: 'bg-cyan-400',
    hoverBg: 'hover:bg-cyan-400'
  }
} as const;

interface CardItemProps {
  card: UserCard;
  onNameEdit?: (cardId: number, newName: string) => void;
}

const CardItem: React.FC<CardItemProps> = ({ card, onNameEdit }) => {
  const navigate = useNavigate();
  const [showCopied, setShowCopied] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [editedName, setEditedName] = useState(card.cardName || '');
  const [saveStatus, setSaveStatus] = useState<{ type: 'success' | 'error' | 'warning'; message: string } | null>(null);
  const [showDrawer, setShowDrawer] = useState(false);
  const editContainerRef = useRef<HTMLDivElement>(null);
  const measureRef = useRef<HTMLSpanElement>(null);

  const getCardOpacity = (status: CardStatus): string => {
    if (status === CardStatus.Active) return 'opacity-100';
    if (status === CardStatus.Inactive) return 'opacity-75';
    return 'opacity-50';
  };

  const getDisabledActionMessage = (status: CardStatus): string => {
    if (status === CardStatus.Inactive) return 'Inactive card';
    if (status === CardStatus.Lost) return 'Card marked as lost';
    if (status === CardStatus.Expired) return 'Expired card';
    if (status === CardStatus.Deactivated) return 'Deleted card';
    return 'Action not available';
  };

  const getStatusMessageStyle = (type: 'success' | 'error' | 'warning'): string => {
    if (type === 'success') return 'bg-green-500 text-white';
    if (type === 'warning') return 'bg-yellow-500 text-black';
    return 'bg-red-500 text-white';
  };

  const getTextWidth = (text: string): number => {
    if (!measureRef.current) return 138; // Default width, fits 'Name your card' exactly
    measureRef.current.textContent = text || 'Name your card';
    return Math.max(measureRef.current.offsetWidth, 138);
  };

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
    setSaveStatus(null); // Clear any previous status message
  };

  const handleSaveClick = async () => {
    if (onNameEdit && editedName.trim() !== card.cardName) {
      try {
        await onNameEdit(card.cardID, editedName.trim());
        
        // Determine message and type based on the new name
        if (editedName.trim() === '') {
          // Empty name - show warning
          setSaveStatus({ type: 'warning', message: 'Card name deleted' });
        } else {
          // Valid name - show success
          setSaveStatus({ type: 'success', message: 'Card name updated!' });
        }
        
        setIsEditing(false); // Exit edit mode immediately
        // Show status message for a few seconds
        setTimeout(() => {
          setSaveStatus(null);
        }, 2000);
      } catch (error) {
        // Show error message
        const errorMessage = error instanceof Error ? error.message : 'Failed to save name';
        setSaveStatus({ type: 'error', message: errorMessage });
        setIsEditing(false); // Exit edit mode immediately
        // Show error message for a few seconds
        setTimeout(() => {
          setSaveStatus(null);
        }, 3000);
      }
    } else {
      // No changes made - show info/warning instead of error
      setSaveStatus({ type: 'warning', message: 'No changes to save' });
      setIsEditing(false); // Exit edit mode immediately
      // Show warning message for a few seconds
      setTimeout(() => {
        setSaveStatus(null);
      }, 2000);
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

  const handleActionClick = (action: 'deposit' | 'transfer' | 'transactions' | 'lost-card' | 'delete-card') => {
    setShowDrawer(false); // Close drawer after action
    
    switch (action) {
      case 'deposit':
        // Navigate to new transaction page with card ID
        navigate(`/new-transaction?cardId=${card.cardID}`);
        break;
        
      case 'transfer':
        // Navigate to new transfer page with card ID
        navigate(`/new-transfer?fromCardId=${card.cardID}`);
        break;
        
      case 'transactions':
        // Navigate to transactions page with card filter
        navigate(`/transactions?filterType=cardID&selectedCardID=${card.cardID}`);
        break;
        
      case 'lost-card':
        // Navigate to report lost card page
        navigate(`/cards/lost/${card.cardID}`);
        break;
        
      case 'delete-card':
        // Navigate to delete card page
        navigate(`/delete-card/${card.cardID}`);
        break;
        
      default:
        console.log('Unknown action:', action);
    }
  };

  // Helper function to determine if an action is available for the card
  const isActionAvailable = (action: 'deposit' | 'transfer' | 'transactions' | 'lost-card' | 'delete-card'): boolean => {
    switch (action) {
      case 'deposit':
        return [CardStatus.Active, CardStatus.Inactive].includes(card.cardStatus);
      case 'transfer':
        return card.cardStatus === CardStatus.Active;
      case 'transactions':
        return true; // Always available
      case 'lost-card':
        return ![CardStatus.Lost, CardStatus.Deactivated].includes(card.cardStatus);
      case 'delete-card':
        return true; // Always available
      default:
        return false;
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

  // Handle ESC key to close drawer
  useEffect(() => {
    const handleEscKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && showDrawer) {
        setShowDrawer(false);
      }
    };

    document.addEventListener('keydown', handleEscKey);

    return () => {
      document.removeEventListener('keydown', handleEscKey);
    };
  }, [showDrawer]);

    return (
    <div className="relative group">
      {/* Hidden element to measure text width */}
      <span 
        ref={measureRef}
        className="absolute -top-96 left-0 text-lg font-semibold text-white opacity-0 pointer-events-none whitespace-nowrap"
        style={{ visibility: 'hidden' }}
      >
        Name your card
      </span>

      {/* Card Container */}
      <div className={`relative rounded-xl overflow-hidden shadow-lg h-48 w-80 bg-gradient-to-br from-gray-600 to-gray-900 text-white p-5 flex flex-col 
        ${getCardOpacity(card.cardStatus)}`}>
          
        {/* Diagonal stripe reflecting card type - Layer 1 */}
        <div 
          className={`absolute top-0 -right-4 w-10 h-full 
            ${CARD_TYPE_MAP[card.cardType as keyof typeof CARD_TYPE_MAP]?.stripeColor || 'bg-gray-400'} 
              ${getCardOpacity(card.cardStatus)}`}
          style={{
            transform: 'skewX(-35deg)',
            transformOrigin: 'top right',
            zIndex: 1
          }}
        />
        
        {/* Balance Overlay - Appears on hover */}
        <div className="absolute bottom-20 left-4 bg-black/80 opacity-0 group-hover:opacity-80 transition-opacity duration-200 rounded-md px-2 py-0.5 z-30 pointer-events-none min-w-20 w-fit">
          <div className="text-center">
            <p className="text-xs text-gray-300">Balance</p>
            <p className="text-sm font-bold text-green-400">{card.balance?.toFixed(2) || '0.00'}₺</p>
          </div>
        </div>
        
        {/* Card Header - Layer 2 */}
        <div className="flex justify-between items-start mb-6 relative z-10">
          <div className="flex-1">
            <div className="flex items-center gap-2">
              {isEditing ? (
                <div ref={editContainerRef} className="flex items-center gap-2 h-7">
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
                    className="bg-gray-700 text-white text-lg font-semibold rounded-md border-none focus:outline-none focus:border-gray-400 min-w-0"
                    style={{ width: `${getTextWidth(editedName)}px` }}
                    placeholder="Name your card"
                    maxLength={16}
                    autoFocus
                  />
                  <button 
                    onClick={handleSaveClick}
                    className={`text-white hover:text-black transition-colors p-1.5 rounded 
                      ${CARD_TYPE_MAP[card.cardType as keyof typeof CARD_TYPE_MAP]?.hoverBg || 'hover:bg-gray-700'}`}
                    aria-label="Save card name"
                  >
                    <Save size={16} />
                  </button>
                </div>
              ) : (
              <div className="flex items-center gap-2 h-7">
                <h3 className={`text-lg font-semibold ${!card.cardName ? 'text-gray-400' : 'text-white'}`}>
                  {card.cardName || 'Name your card'}
                </h3>
                <div className="relative group/pen">
                  <button 
                    onClick={handleEditClick}
                    className={`text-white hover:text-black transition-colors p-1.5 rounded 
                      ${CARD_TYPE_MAP[card.cardType as keyof typeof CARD_TYPE_MAP]?.hoverBg || 'hover:bg-gray-700'}`}
                    aria-label="Edit card name"
                  >
                    <PenLine size={16} />
                  </button>
                  
                </div>
              </div>
              )}
            </div>
            <p className="text-sm text-gray-300">Virtual Card</p>
          </div>
        </div>

        {/* Status Message for Card Name Edit - Inside card's overflow-hidden */}
          {saveStatus && (
            <div className={`absolute top-12 left-1/2 transform -translate-x-1/2 text-xs px-2 py-1 rounded shadow-lg z-50 animate-fade-in whitespace-nowrap 
              ${getStatusMessageStyle(saveStatus.type)}`}
                style={{ left: 'calc(50% + 8px)' }}>
                {saveStatus.message}
            </div>
          )}
        
        {/* Card Number with Copy Icon - Layer 2 */}
        <div className="mt-6 mb-4 relative z-10">
          <div className="flex items-center gap-2">
            <div className="w-50"> {/* Fixed width container */}
              <p className="text-lg font-mono tracking-widest">
                <span className="group-hover:hidden">
                  TRK90 {card.cardNumber.slice(-11, -7).replace(/\d/g, 'X')} {card.cardNumber.slice(-7, -3).replace(/\d/g, 'X')} <span className="text-black">{card.cardNumber.slice(-3)}</span>
                </span>
                <span className="hidden group-hover:inline">
                  {formatCardNumber(card.cardNumber).slice(0, -3)}<span className="text-black">{formatCardNumber(card.cardNumber).slice(-3)}</span>
                </span>
              </p>
            </div>
            <div className="relative">
              <button
                onClick={handleCopyCardNumber}
                className={`text-white hover:text-black transition-colors p-1.5 rounded 
                  ${CARD_TYPE_MAP[card.cardType as keyof typeof CARD_TYPE_MAP]?.hoverBg || 'hover:bg-gray-700'}`}
                aria-label="Copy card number"
                title="Copy card number"
              >
                <Copy size={16} />
              </button>

              {/* Floating "Copied!" notification */}
              {showCopied && (
                <div className="absolute -bottom-7 -left-10 transform -translate-x-1/2 bg-green-500 text-black text-xs px-2 py-1.5 rounded shadow-lg z-20 animate-fade-in">
                  Copied!
                </div>
              )}
            </div>
          </div>
        </div>
        
        {/* Expiration - Layer 2 */}
        <div className="-mt-4 mb-2 relative z-10">
          <p className="text-xs text-gray-400">Expiration Date</p>
          <p className="text-sm -my-0.5">{formatExpirationDate(card.cardExpirationDate)}</p>
        </div>
        
        {/* Three dots menu with dynamic colors based on card type - Layer 2 */}
        <div className="absolute bottom-4 right-4 z-10">
          <button 
            onClick={() => setShowDrawer(!showDrawer)}
            className={`rounded-full p-1 
              ${CARD_TYPE_MAP[card.cardType as keyof typeof CARD_TYPE_MAP]?.buttonBg} transition-colors duration-200`}
          >
            <MoreVertical className="w-5 h-5 text-white" />
          </button>
        </div>
      </div>

      {/* Action Menu - Outside card's overflow-hidden */}
      {showDrawer && (
        <div className="absolute top-full left-0 w-80 mt-3 z-20">
          {/* Upward Arrow */}
          <div className="w-0 h-0 border-l-4 border-r-4 border-b-4 border-transparent border-b-gray-800 mx-auto"></div>
          
          {/* Menu Panel */}
          <div className="bg-gray-800 rounded-lg p-2 shadow-lg">
            <div className="flex items-center gap-2">
              
            {/* Column 1: Deposit */}
            <div className="relative group/deposit flex-1">
              <button
                onClick={() => handleActionClick('deposit')}
                disabled={!isActionAvailable('deposit')}
                className={`flex flex-col items-center gap-1 transition-colors py-3 w-full ${
                  isActionAvailable('deposit') 
                    ? 'text-white hover:text-green-400 cursor-pointer' 
                    : 'text-gray-500 cursor-not-allowed'
                }`}
              >
                <span className="text-sm">₺</span>
                <span className="text-sm">Deposit</span>
              </button>
              
              {/* Tooltip for disabled deposit */}
              {!isActionAvailable('deposit') && (
                <div className="absolute -top-10 left-1/2 transform -translate-x-1/2 bg-gray-700 text-white text-xs px-2 py-1 rounded shadow-lg opacity-0 group-hover/deposit:opacity-100 transition-opacity duration-200 pointer-events-none whitespace-nowrap z-50">
                  {getDisabledActionMessage(card.cardStatus)}
                  {/* Tooltip arrow */}
                  <div className="absolute top-full left-1/2 transform -translate-x-1/2 w-0 h-0 border-l-2 border-r-2 border-t-2 border-transparent border-t-gray-700"></div>
                </div>
              )}
            </div>
              
              <div className="w-px h-16 bg-gray-400"></div>
              
            {/* Column 2: Transfer */}
            <div className="relative group/transfer flex-1">
              <button
                onClick={() => handleActionClick('transfer')}
                disabled={!isActionAvailable('transfer')}
                className={`flex flex-col items-center gap-1 transition-colors py-3 w-full ${
                  isActionAvailable('transfer') 
                    ? 'text-white hover:text-green-400 cursor-pointer' 
                    : 'text-gray-500 cursor-not-allowed'
                }`}
              >
                <ArrowUpRight size={16} />
                <span className="text-sm">Transfer</span>
              </button>
              
              {/* Tooltip for disabled transfer */}
              {!isActionAvailable('transfer') && (
                <div className="absolute -top-10 left-1/2 transform -translate-x-1/2 bg-gray-700 text-white text-xs px-2 py-1 rounded shadow-lg opacity-0 group-hover/transfer:opacity-100 transition-opacity duration-200 pointer-events-none whitespace-nowrap z-50">
                  {getDisabledActionMessage(card.cardStatus)}
                  
                  {/* Tooltip arrow */}
                  <div className="absolute top-full left-1/2 transform -translate-x-1/2 w-0 h-0 border-l-2 border-r-2 border-t-2 border-transparent border-t-gray-700"></div>
                </div>
              )}
            </div>
              
              <div className="w-px h-16 bg-gray-400"></div>
              
            {/* Column 3: History */}
            <div className="relative group/history flex-1">
              <button
                onClick={() => handleActionClick('transactions')}
                disabled={!isActionAvailable('transactions')}
                className={`flex flex-col items-center gap-1 transition-colors py-3 w-full ${
                  isActionAvailable('transactions') 
                    ? 'text-white hover:text-green-400 cursor-pointer' 
                    : 'text-gray-500 cursor-not-allowed'
                }`}
              >
                <ArrowRightLeft size={16} />
                <span className="text-sm">History</span>
              </button>
              
              {/* Tooltip for disabled history */}
              {!isActionAvailable('transactions') && (
                <div className="absolute -top-10 left-1/2 transform -translate-x-1/2 bg-gray-700 text-white text-xs px-2 py-1 rounded shadow-lg opacity-0 group-hover/history:opacity-100 transition-opacity duration-200 pointer-events-none whitespace-nowrap z-50">
                  {getDisabledActionMessage(card.cardStatus)}
                  {/* Tooltip arrow */}
                  <div className="absolute top-full left-1/2 transform -translate-x-1/2 w-0 h-0 border-l-2 border-r-2 border-t-2 border-transparent border-t-gray-700"></div>
                </div>
              )}
            </div>
              
              <div className="w-px h-16 bg-gray-400"></div>
              
                {/* Column 4: Lost and Delete stacked */}
                <div className="flex flex-col gap-1 flex-1">
                  <div className="relative group/lost">
                    <button
                      onClick={() => handleActionClick('lost-card')}
                      disabled={!isActionAvailable('lost-card')}
                      className={`flex items-center justify-center gap-1 transition-colors py-1 w-full ${
                        isActionAvailable('lost-card') 
                          ? 'text-yellow-300 hover:text-yellow-500 cursor-pointer' 
                          : 'text-gray-500 cursor-not-allowed'
                      }`}
                    >
                      <ShieldAlert size={16} />
                      <span className="text-xs">Lost?</span>
                    </button>

                    {/* Tooltip for marking a card as Lost*/}
                    {card.cardStatus !== CardStatus.Lost && (
                      <div className="absolute -top-10 left-1/2 transform -translate-x-1/2 bg-gray-700 text-white text-xs px-2 py-1 rounded shadow-lg opacity-0 group-hover/lost:opacity-100 transition-opacity duration-200 pointer-events-none whitespace-nowrap z-50">
                        Did you lose your card?
                        {/* Tooltip arrow */}
                        <div className="absolute top-full left-1/2 transform -translate-x-1/2 w-0 h-0 border-l-2 border-r-2 border-t-2 border-transparent border-t-gray-700"></div>
                      </div>
                    )}  
                    {/* Tooltip for already lost cards */}
                    {card.cardStatus === CardStatus.Lost && (
                      <div className="absolute -top-10 left-1/2 transform -translate-x-1/2 bg-gray-700 text-white text-xs px-2 py-1 rounded shadow-lg opacity-0 group-hover/lost:opacity-100 transition-opacity duration-200 pointer-events-none whitespace-nowrap z-50">
                        Already marked as lost
                        {/* Tooltip arrow */}
                        <div className="absolute top-full left-1/2 transform -translate-x-1/2 w-0 h-0 border-l-2 border-r-2 border-t-2 border-transparent border-t-gray-700"></div>
                      </div>
                    )}
                  </div>
                
                <div className="w-full h-px bg-gray-400"></div>
                
                <div className="relative group/delete">
                  <button
                    onClick={() => handleActionClick('delete-card')}
                    disabled={!isActionAvailable('delete-card')}
                    className={`flex items-center justify-center gap-1 transition-colors py-1 w-full ${
                      isActionAvailable('delete-card') 
                        ? 'text-red-300 hover:text-red-500 cursor-pointer' 
                        : 'text-gray-500 cursor-not-allowed'
                    }`}
                  >
                    <Trash2 size={16} />
                    <span className="text-xs">Delete</span>
                  </button>
                  
                  {/* Tooltip for disabled delete */}
                  {!isActionAvailable('delete-card') && (
                    <div className="absolute -top-10 left-1/2 transform -translate-x-1/2 bg-gray-700 text-white text-xs px-2 py-1 rounded shadow-lg opacity-0 group-hover/delete:opacity-100 transition-opacity duration-200 pointer-events-none whitespace-nowrap z-50">
                      {getDisabledActionMessage(card.cardStatus)}
                      {/* Tooltip arrow */}
                      <div className="absolute top-full left-1/2 transform -translate-x-1/2 w-0 h-0 border-l-2 border-r-2 border-t-2 border-transparent border-t-gray-700"></div>
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default CardItem;
