import api from './api';
import { UserCard, CreateUserCardRequest, DeleteUserCardRequest } from '@/types';

class UserCardService {
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