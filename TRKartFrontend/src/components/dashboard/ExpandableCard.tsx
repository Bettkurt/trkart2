import React, { useState, useRef, useEffect, KeyboardEvent } from 'react';
import { Link } from 'react-router-dom';

interface ExpandableCardProps {
  title: string;
  description: string;
  isExpandable?: boolean;
  defaultExpanded?: boolean;
  children?: React.ReactNode;
  to?: string;
  onClick?: () => void;
}

const ExpandableCard: React.FC<ExpandableCardProps> = ({
  title,
  description,
  isExpandable = false,
  defaultExpanded = false,
  children,
  to,
  onClick,
}) => {
  const [isExpanded, setIsExpanded] = useState(defaultExpanded);
  const [isHovered, setIsHovered] = useState(false);
  const [contentHeight, setContentHeight] = useState(0);
  const contentRef = useRef<HTMLDivElement>(null);

  // Update height when content changes or when expanded state changes
  useEffect(() => {
    if (contentRef.current && isExpandable) {
      setContentHeight(isExpanded ? contentRef.current.scrollHeight : 0);
    }
  }, [isExpanded, isExpandable, children]);

  const handleToggle = (e: React.MouseEvent | KeyboardEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (isExpandable) {
      setIsExpanded(!isExpanded);
    }
  };

  const handleKeyDown = (e: KeyboardEvent<HTMLButtonElement>) => {
    if (e.key === 'Enter' || e.key === ' ') {
      handleToggle(e);
    }
  };

  const handleClick = (e: React.MouseEvent) => {
    if (isExpandable) {
      handleToggle(e);
    } else if (onClick) {
      onClick();
    }
  };

  const renderContent = () => (
    <div className="flex justify-between items-center w-full">
      <div>
        <h3 className="text-xl font-medium text-gray-900">{title}</h3>
        <p className="text-gray-600 mt-1 text-base">{description}</p>
      </div>
      {isExpandable && (
        <div className="text-gray-500">
          <svg
            className={`h-5 w-5 transform transition-transform duration-200 ${isExpanded ? 'rotate-180' : ''}`}
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            aria-hidden="true"
          >
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
          </svg>
        </div>
      )}
    </div>
  );

  const cardClasses = `card bg-yellow-400 border-yellow-600 shadow transition-shadow ${
    !isExpandable && isHovered ? 'shadow-lg' : ''
  }`;

  if (isExpandable) {
    return (
      <div className={cardClasses}>
        <button
          className="w-full text-left p-6 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-opacity-50"
          onClick={handleClick}
          onKeyDown={handleKeyDown}
          aria-expanded={isExpanded}
        >
          {renderContent()}
        </button>
        <div 
          ref={contentRef}
          className="px-6 pb-6 transition-all duration-200 ease-in-out overflow-hidden"
          style={{
            maxHeight: isExpanded ? `${contentHeight}px` : '0px',
            opacity: isExpanded ? 1 : 0,
            marginTop: isExpanded ? '0' : '-1rem',
          }}
        >
          <div className="pt-4 border-t border-yellow-200">
            {children}
          </div>
        </div>
      </div>
    );
  }

  if (to) {
    return (
      <Link
        to={to}
        className={`${cardClasses} block p-6`}
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
        onClick={onClick}
      >
        {renderContent()}
      </Link>
    );
  }

  return (
    <div
      className={cardClasses}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      <div className="p-6">{renderContent()}</div>
    </div>
  );
};

export default ExpandableCard;
