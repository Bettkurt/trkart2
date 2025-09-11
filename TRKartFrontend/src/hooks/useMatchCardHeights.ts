import { useEffect, useRef } from 'react';

export const useMatchCardHeights = (expanded: boolean) => {
  const cardsRef = useRef<HTMLDivElement[]>([]);

  useEffect(() => {
    const matchHeights = () => {
      const cards = cardsRef.current;
      if (!cards.length) return;

      // Reset heights to auto to get natural heights
      cards.forEach(card => {
        if (!card.classList.contains('open')) {
          const collapsed = card.querySelector('.collapsed-content') as HTMLElement;
          if (collapsed) collapsed.style.height = 'auto';
        }
      });

      // Find the maximum height
      let maxHeight = 0;
      cards.forEach(card => {
        if (!card.classList.contains('open')) {
          const collapsed = card.querySelector('.collapsed-content') as HTMLElement;
          if (collapsed && collapsed.offsetHeight > maxHeight) {
            maxHeight = collapsed.offsetHeight;
          }
        }
      });

      // Apply the maximum height to all cards
      cards.forEach(card => {
        if (!card.classList.contains('open')) {
          const collapsed = card.querySelector('.collapsed-content') as HTMLElement;
          if (collapsed) collapsed.style.height = `${maxHeight}px`;
        }
      });
    };

    // Initial height matching
    matchHeights();

    // Re-calculate on window resize
    window.addEventListener('resize', matchHeights);
    return () => window.removeEventListener('resize', matchHeights);
  }, [expanded]);

  // Function to register card refs
  const registerCard = (el: HTMLDivElement | null) => {
    if (el && !cardsRef.current.includes(el)) {
      cardsRef.current.push(el);
    }
  };

  return { registerCard };
};
