import api from './api';
import { UserCard, CardStatusUpdateRequest } from '../types';
import { CardType } from '../types/cardTypes';

export interface CreateUserCardRequest {
  customerID: number;
  cardType: CardType;
  cardName?: string;
}

class UserCardService {
  /**
   * Get all cards for the current authenticated user
   */
  async getUserCards(): Promise<UserCard[]> {
    const response = await api.get<{ success: boolean; cards: UserCard[] }>('/SecureUserCard/user/cards');
    return response.data.cards;
  }

  /**
   * Create a new card for the current user
   */
  async createUserCard(cardData: CreateUserCardRequest): Promise<UserCard> {
    const requestData = {
      customerID: cardData.customerID,
      cardType: cardData.cardType,
      ...(cardData.cardName && { cardName: cardData.cardName })
    };
    
    const response = await api.post<{ success: boolean; card: UserCard }>('/SecureUserCard/user/card', requestData);
    return response.data.card;
  }

  /**
   * Get a specific card by its number
   */
  async getUserCardByNumber(cardNumber: string): Promise<UserCard> {
    const response = await api.get<{ success: boolean; card: UserCard }>(`/SecureUserCard/user/card/${cardNumber}`);
    return response.data.card;
  }

  /**
   * Get the current user's profile information
   */
  async getUserProfile(): Promise<any> {
    const response = await api.get<{ success: boolean; user: any }>('/SecureUserCard/user/profile');
    return response.data.user;
  }

  /**
   * @deprecated Use getUserCards() instead
   * Get all cards for the current authenticated user
   */
  async getCardsByCustomerId(): Promise<UserCard[]> {
    return this.getUserCards();
  }

  /**
   * @deprecated Use getUserCardByNumber() instead
   * Get a specific card by its number
   */
  async getCardByNumber(cardNumber: string): Promise<UserCard> {
    return this.getUserCardByNumber(cardNumber);
  }

  /**
   * @deprecated Use createUserCard() instead
   * Create a new card for the current user
   */
  async createCard(cardData: CreateUserCardRequest): Promise<UserCard> {
    return this.createUserCard(cardData);
  }

  /**
   * Updates the status of a user's card (e.g., to 'Deactivated' or 'Lost')
   * @param updateData Object containing cardId and the new status
   */
  async updateCardStatus(updateData: CardStatusUpdateRequest) {
    // Using the secure endpoint which requires authentication
    const response = await api.put<{
      success: boolean; 
      message: string; 
      card: UserCard 
    }>(
      '/SecureUserCard/user/card/status',
      {
        cardId: updateData.cardId,
        status: updateData.status
      }
    );
    return response.data;
  }
}

export default new UserCardService();