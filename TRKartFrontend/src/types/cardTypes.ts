export enum CardType {
  Standard = 0,
  Gold = 1,
  Platinum = 2
}

export interface CardTypeInfo {
  id: CardType;
  name: string;
  description: string;
  limit: number;
}

export const CARD_TYPES: CardTypeInfo[] = [
  { 
    id: CardType.Standard, 
    name: 'Standard', 
    description: 'Standard card with basic features', 
    limit: 20000 
  },
  { 
    id: CardType.Gold, 
    name: 'Gold', 
    description: 'Premium card with additional benefits', 
    limit: 50000 
  },
  { 
    id: CardType.Platinum, 
    name: 'Platinum', 
    description: 'Elite card with premium benefits', 
    limit: 100000 
  }
];
