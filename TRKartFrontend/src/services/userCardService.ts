import api from './api';
import { UserCard, CreateUserCardRequest, DeleteUserCardRequest } from '@/types';

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

  async deleteUserCard(deleteData: DeleteUserCardRequest): Promise<void> {
    await api.delete('/SecureUserCard/user/card', { data: deleteData });
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

  async deleteCard(deleteData: DeleteUserCardRequest): Promise<void> {
    await api.delete('/UserCard', { data: deleteData });
  }
}

export default new UserCardService(); 