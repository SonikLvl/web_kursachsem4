// src/services/userService.ts
import axios from 'axios';

// Інтерфейс для даних створення користувача (без змін)
export interface INewUser {
  UserName: string;
  Email: string;
  Password: string;
  // Додайте інші поля, якщо потрібно
}

// Інтерфейс для даних, які повертає бекенд при створенні (без змін)
export interface ICreatedUser {
  userId: number; // Або string, залежно від бекенду
  UserName: string;
  Email: string;
  // ... інші поля
}

// --- Інтерфейси для Логіну (без змін) ---
export interface ILoginCredentials {
  Username: string; // Або Email - адаптуйте!
  Password: string;
}

export interface ILoginResponse {
  token: string;
  user: { // Передбачається, що бекенд повертає об'єкт користувача при логіні
    userId: number; // Або string
    username: string;
    email: string;
  };
}

export interface IUserProfile {
  userId: number; // Або string
  username: string;
  email?: string; // Email може бути відсутнім, якщо ендпоінт повертає лише ім'я
  // ... інші поля
}
// --- КІНЕЦЬ Інтерфейсів ---


export default class UserService {
  // Метод створення користувача (без змін)
  public async createUser(userData: INewUser): Promise<ICreatedUser> {
    try {
      const result = await axios.post<ICreatedUser>('/api/users', userData);
      console.log('User created successfully:', result.data);
      return result.data;
    } catch (error) {
      console.error('Error creating user:', error);
      if (axios.isAxiosError(error) && error.response) {
        console.error('Response data:', error.response.data);
        console.error('Response status:', error.response.status);
        throw new Error(`Failed to create user: ${error.response.status} - ${error.response.data?.message || error.message}`);
      }
      throw error;
    }
  }

  // Метод для Логіну (без змін у логіці сервісу)
  public async login(credentials: ILoginCredentials): Promise<ILoginResponse> {
    try {
      const result = await axios.post<ILoginResponse>('/api/auth/login', credentials); // Адаптуйте шлях!
      console.log('User logged in successfully:', result.data);
      return result.data;
    } catch (error) {
      console.error('Error logging in:', error);
      if (axios.isAxiosError(error) && error.response) {
        console.error('Response data:', error.response.data);
        console.error('Response status:', error.response.status);
        let errorMessage = 'Помилка входу.';
        if (error.response.status === 401) {
          errorMessage = 'Невірний логін або пароль.'; // Виправлено з email на логін
        } else if (error.response.data?.message) {
          errorMessage = error.response.data.message;
        } else {
          errorMessage = `Помилка ${error.response.status}. Спробуйте ще раз.`;
        }
         throw new Error(errorMessage);
      }
       throw new Error('Не вдалося увійти. Перевірте з\'єднання.');
    }
  }

  // Метод для отримання лише імені користувача (без змін)
  // Цей метод використовується для отримання імені для привітання після відновлення сесії.
  public async getCurrentUser(): Promise<string> {
    try {
      // !!! Шлях '/api/me/username' має повернути ЛИШЕ рядок з ім'ям користувача !!!
      const result = await axios.get<string>('/api/me/username'); // Очікуємо string
      console.log('Username fetched:', result.data);
      return result.data;
    } catch (error) {
      console.error('Error fetching username:', error);
      if (axios.isAxiosError(error) && error.response && error.response.status === 401) {
        throw new Error('Сесія недійсна або закінчилась. Будь ласка, увійдіть знову.');
      }
      if (axios.isAxiosError(error) && error.response) {
        if (error.response.data?.message) {
          throw new Error(`Не вдалося завантажити ім'я: ${error.response.status} - ${error.response.data.message}`);
        }
        throw new Error(`Не вдалося завантажити ім'я користувача: Помилка ${error.response.status}.`);
      }
      throw new Error('Не вдалося отримати ім\'я. Перевірте з\'єднання.');
    }
  }

  // --- НОВИЙ Метод для отримання лише Email користувача ---
  // Цей метод використовується для отримання email для відображення профілю.
  public async getCurrentEmail(): Promise<string> {
    try {
      // !!! Шлях '/api/me/email' має повернути ЛИШЕ рядок з email користувача !!!
      const result = await axios.get<string>('/api/me/email'); // Очікуємо string
      console.log('Email fetched:', result.data);
      return result.data;
    } catch (error) {
      console.error('Error fetching Email:', error);
      if (axios.isAxiosError(error) && error.response && error.response.status === 401) {
        throw new Error('Сесія недійсна або закінчилась. Будь ласка, увійдіть знову.');
      }
      if (axios.isAxiosError(error) && error.response) {
        if (error.response.data?.message) {
          throw new Error(`Не вдалося завантажити email: ${error.response.status} - ${error.response.data.message}`);
        }
        throw new Error(`Не вдалося завантажити email користувача: Помилка ${error.response.status}.`);
      }
      throw new Error('Не вдалося отримати email. Перевірте з\'єднання.');
    }
  }

  // --- Метод для Видалення Акаунту (без змін) ---
  // Видаляє акаунт поточного автентифікованого користувача, використовуючи токен.
  // Ендпоінт: DELETE /api/me
  public async deleteAccount(): Promise<void> { // Метод більше не приймає userId
    const token = localStorage.getItem('authToken');
    if (!token) {
        // Ця перевірка на клієнті, але бекенд теж має перевіряти токен
        throw new Error('Користувач не автентифікований. Неможливо видалити акаунт.');
    }

    try {
        // !!! ВАЖЛИВО: Використовуємо шлях '/api/me' для видалення поточного автентифікованого користувача !!!
        // Бекенд має ідентифікувати користувача за токеном в заголовку.
        const result = await axios.delete('/api/me', { // URL змінено на /api/me, параметр userId видалено
            headers: {
                Authorization: `Bearer ${token}` // Передача токену в заголовку для автентифікації
            }
        });

        console.log(`Authenticated user account deleted successfully.`, result.data);
        // Якщо бекенд повертає якесь повідомлення про успіх, ви можете його логувати або обробляти
        // Для DELETE /api/me зазвичай не очікується тіло відповіді або лише статус успіху (200, 204).

    } catch (error) {
        console.error(`Error deleting authenticated user account:`, error);
        if (axios.isAxiosError(error) && error.response) {
            console.error('Response data:', error.response.data);
            console.error('Response status:', error.response.status);
            let errorMessage = 'Не вдалося видалити акаунт.';
            if (error.response.status === 401 || error.response.status === 403) {
                 errorMessage = 'Немає прав для видалення цього акаунту або сесія недійсна. Будь ласка, увійдіть знову.'; // Повідомлення скориговано
            } else if (error.response.status === 404) {
                 // Це може означати, що токен був дійсний, але не був прив'язаний до користувача? Нетипово для /api/me, але можливо.
                 errorMessage = 'Акаунт для видалення не знайдено (можливо, він вже видалений).';
            } else if (error.response.data?.message) {
                 errorMessage = error.response.data.message;
            } else {
                 errorMessage = `Помилка ${error.response.status}. Спробуйте ще раз.`;
            }
            throw new Error(errorMessage); // Кидаємо кастомну помилку для обробки в компоненті
        }
        throw new Error('Не вдалося видалити акаунт. Перевірте з\'єднання.'); // Загальна помилка
    }
  }



}