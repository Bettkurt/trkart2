import api from './api';
import { UserCard, CreateUserCardRequest, CardStatusUpdateRequest } from '@/types';

class UserCardService {
  // New secure user-specific methods
  async getUserCards(): Promise<UserCard[]> {
    const response = await api.get<{ success: boolean; cards: UserCard[] }>('/SecureUserCard/user/cards');
    return response.data.cards;
  }

  async createUserCard(cardData: CreateUserCardRequest): Promise<UserCard> {
    const response = await api.post<{ success: boolean; card: UserCard }>('/SecureUserCard/user/card', cardData);
    return response.data.card;
  }

  async getUserCardByNumber(cardNumber: string): Promise<UserCard> {
    const response = await api.get<{ success: boolean; card: UserCard }>(`/SecureUserCard/user/card/${cardNumber}`);
    return response.data.card;
  }

  async getUserProfile(): Promise<any> {
    const response = await api.get<{ success: boolean; user: any }>('/SecureUserCard/user/profile');
    return response.data.user;
  }

  // Existing methods for backward compatibility
  async getCardsByCustomerId(customerId: number): Promise<UserCard[]> {
    const response = await api.get<UserCard[]>(`/UserCard/customer/${customerId}`);
    return response.data;
  }

  async getCardByNumber(cardNumber: string): Promise<UserCard> {
    const response = await api.get<UserCard>(`/UserCard/number/${cardNumber}`);
    return response.data;
  }

  async createCard(cardData: CreateUserCardRequest): Promise<UserCard> {
    const response = await api.post<UserCard>('/UserCard', cardData);
    return response.data;
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
      updateData
    );
    return response.data;
  }
}

export default new UserCardService();