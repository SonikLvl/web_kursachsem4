import axios, { AxiosError } from 'axios';

/**
 * Інтерфейс для даних гри, що надсилаються на бекенд.
 * Ви можете розширити його, додавши інші поля, які ваша гра повинна зберігати.
 */
export interface IGameData {
  score: number;
}

/**
 * Інтерфейс для очікуваної відповіді від бекенду після оновлення рахунку.
 * Якщо ваш бекенд повертає конкретні дані, ви можете визначити їх тут.
 * Часто для PUT-запитів, які оновлюють дані, відповідь може бути порожньою (204 No Content)
 * або містити оновлений ресурс чи повідомлення про успіх.
 */
export interface IUpdateScoreResponse {
  message?: string; // Наприклад, "Рахунок успішно оновлено"
  updatedScore?: number; // Якщо бекенд повертає оновлений рахунок
  // ... інші поля, які може повернути ваш API
}

/**
 * Інтерфейс для відповіді від GET ендпоінту рахунку.
 * Припускаємо, що бекенд повертає об'єкт з полем 'score'.
 */
export interface IGetScoreResponse {
  score: number;
  // Можливо, інші поля, як-от дата останнього оновлення тощо.
}


// URL API ендпоінту для оновлення/збереження рахунку користувача.
const UPDATE_SCORE_API_URL = 'https://localhost:7167/api/me/score';
// URL API ендпоінту для отримання рахунку користувача.
const GET_SCORE_API_URL = 'https://localhost:7167/api/me/score'; // Припускаємо, що GET і PUT використовують один URL


/**
 * Клас GameDataService надає методи для взаємодії з API,
 * пов'язаними з даними гри користувача (наприклад, збереження рахунку).
 */
export default class GameDataService {

  /**
   * Асинхронно отримує поточний рахунок користувача з бекенду.
   * Використовує GET-запит та Bearer токен для авторизації.
   *
   * @param authToken - Рядок, що містить Bearer токен авторизації користувача.
   * @returns Promise, який вирішується з поточним рахунком (число) або null,
   * якщо рахунок не знайдено (наприклад, статус 404) або сталася помилка.
   * @throws Error - Викидає помилку у випадку проблем із запитом або відсутності токена.
   */
  public async getUserScore(authToken: string): Promise<number | null> {
    if (!authToken) {
      const errorMessage = 'Токен авторизації не надано. Неможливо отримати рахунок.';
      console.error(errorMessage);
      throw new Error(errorMessage);
    }

    try {
      const response = await axios.get<IGetScoreResponse>(
        GET_SCORE_API_URL,
        {
          headers: {
            'Authorization': `Bearer ${authToken}`, // Додаємо заголовок авторизації
          },
        }
      );

      // Обробка успішної відповіді (зазвичай 200 OK)
      if (response.status === 200 && response.data && typeof response.data.score === 'number') {
        console.log('Поточний рахунок користувача отримано:', response.data.score);
        return response.data.score;
      } else {
          // Якщо статус 200, але дані не відповідають очікуваному формату
          console.warn(`Отримано неочікуваний успішний статус (${response.status}) або формат даних при отриманні рахунку. Дані:`, response.data);
          // Можна повернути null або викинути помилку, залежно від очікуваної поведінки
          return null; // Або throw new Error('Неочікуваний формат даних відповіді');
      }

    } catch (error) {
      console.error('Помилка під час отримання рахунку користувача:', error);

      if (axios.isAxiosError(error)) {
        const axiosError = error as AxiosError<any>;
        if (axiosError.response) {
          const status = axiosError.response.status;
          const data = axiosError.response.data;

          // Обробка специфічних помилок, наприклад, 404 Not Found, якщо рахунок ще не існує
          if (status === 404) {
            console.log('Рахунок користувача не знайдено на бекенді (статус 404).');
            return null; // Повертаємо null, якщо рахунок не знайдено
          } else if (status === 401) {
              throw new Error('Помилка авторизації (401) під час отримання рахунку. Можливо, ваш токен недійсний.');
          } else if (status === 403) {
              throw new Error('Доступ заборонено (403) під час отримання рахунку. У вас недостатньо прав.');
          } else {
              // Інші помилки сервера
              let errorMessage = `Помилка отримання рахунку: ${status}.`;
              if (data?.message) errorMessage += ` Повідомлення: ${data.message}`;
              console.error('Дані помилки від сервера:', data);
              throw new Error(errorMessage);
          }
        } else if (axiosError.request) {
          // Запит зроблено, але відповіді немає (проблема з мережею)
          throw new Error('Не вдалося отримати рахунок. Сервер не відповів. Перевірте з\'єднання з мережею.');
        }
      }
      // Загальна помилка
      throw new Error('Не вдалося отримати рахунок. Сталася невідома помилка.');
    }
  }


  /**
   * Асинхронно надсилає запит на оновлення рахунку користувача на бекенді.
   * Бекенд повинен містити логіку порівняння та оновлювати рахунок лише
   * якщо новий рахунок більший за існуючий.
   *
   * @param gameData - Об'єкт з даними гри для надсилання (мінімум: score).
   * @param authToken - Рядок, що містить Bearer токен авторизації користувача.
   * @returns Promise, який вирішується з даними відповіді від бекенду або порожнім значенням (void)
   * залежно від відповіді бекенду (наприклад, 200 з даними або 204 без вмісту).
   * @throws Error - Викидає помилку у випадку проблем із запитом, авторизацією або помилки сервера.
   */
  public async updateUserScore(
    gameData: IGameData,
    authToken: string
  ): Promise<IUpdateScoreResponse | void> {
    if (!authToken) {
        const errorMessage = 'Токен авторизації не надано. Неможливо оновити рахунок.';
        console.error(errorMessage);
        throw new Error(errorMessage);
    }

    // **ВІДМОВЛЯЄМОСЬ ВІД КЛІЄНТСЬКОЇ ПЕРЕВІРКИ ТУТ!**
    // Логіка порівняння має бути на бекенді.
    // Ми просто надсилаємо новий рахунок.

    try {
        const response = await axios.put<IUpdateScoreResponse | void>(
            UPDATE_SCORE_API_URL,
            gameData, // Надсилаємо дані гри, що включають новий рахунок
            {
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${authToken}`,
                },
            }
        );

        // Обробка успішних відповідей від бекенду
        // Бекенд має повернути 2xx статус, якщо запит був успішним
        // (незалежно від того, чи був рахунок фактично оновлений через порівняння)
        if (response.status >= 200 && response.status < 300) {
            console.log(`PUT запит на оновлення рахунку успішний. Статус: ${response.status}.`);

            if (response.data) {
                console.log('Відповідь з даними:', response.data);
                // Припускаємо, що бекенд може повернути IUpdateScoreResponse,
                // навіть якщо рахунок не був оновлений, але запит був коректним
                // (наприклад, повідомлення "Рахунок не оновлено, оскільки він не більший").
                return response.data as IUpdateScoreResponse;
            } else {
                console.log('Відповідь без вмісту (наприклад, 204 No Content).');
                return; // Повертаємо void
            }
        } else {
             // Цей блок, ймовірно, не буде досягнутий, якщо axios кидає помилку на статуси >= 300
             // але як запасний варіант для неочікуваних успішних статусів >= 300:
             console.warn(`Отримано неочікуваний, але не помилковий статус: ${response.status}. Дані:`, response.data);
             return response.data; // Повертаємо те, що прийшло
        }

    } catch (error) {
        console.error('Помилка під час оновлення рахунку користувача:', error);

        if (axios.isAxiosError(error)) {
            const axiosError = error as AxiosError<any>;
            if (axiosError.response) {
                const status = axiosError.response.status;
                const data = axiosError.response.data;
                let errorMessage = `Помилка оновлення рахунку: ${status}.`;

                // Деталізація помилки з відповіді бекенду
                if (data?.message) {
                    errorMessage += ` Повідомлення: ${data.message}`;
                } else if (typeof data === 'string' && data.length > 0) {
                     // Якщо бекенд повернув просто текстове повідомлення про помилку
                    errorMessage += ` Деталі: ${data}`;
                } else if (data) {
                     // Якщо бекенд повернув об'єкт помилки іншого формату
                     errorMessage += ` Дані помилки: ${JSON.stringify(data)}`;
                }


                console.error('Дані помилки від сервера:', data);
                console.error('Статус помилки від сервера:', status);


                // Обробка стандартних помилок API
                if (status === 401) {
                    throw new Error('Помилка авторизації (401). Можливо, ваш токен недійсний або сесія закінчилася.');
                } else if (status === 403) {
                    throw new Error('Доступ заборонено (403). У вас недостатньо прав для виконання цієї операції.');
                } else if (status === 400 || status === 422) {
                    // Обробка помилок валідації або бізнес-логіки від бекенду,
                    // включаючи випадок, коли рахунок не був оновлений, бо він не вищий.
                    // Ваш бекенд може повернути 400 або 422 у цьому випадку.
                    // У цьому випадку, можливо, ви не хочете кидати помилку,
                    // а просто обробити повідомлення від сервера.
                    // Ви можете адаптувати це відповідно до того, як саме ваш бекенд сигналізує
                    // про "рахунок не оновлено, бо не більше".
                    if (data?.message) {
                         // Якщо бекенд повернув повідомлення про те, що рахунок не більший
                         console.log("Бекенд повідомив:", data.message);
                         // Можна повернути data або спеціальний об'єкт
                         return data as IUpdateScoreResponse;
                    }
                     throw new Error(errorMessage); // Кидаємо як загальну помилку валідації
                } else {
                    // Інші помилки сервера (5xx)
                    throw new Error(errorMessage);
                }
            } else if (axiosError.request) {
                // Запит зроблено, але відповіді немає (проблема з мережею)
                throw new Error('Не вдалося оновити рахунок. Сервер не відповів. Перевірте з\'єднання з мережею.');
            }
        }
        // Загальна помилка
        throw new Error('Не вдалося оновити рахунок. Сталася невідома помилка.');
    }
  }
}