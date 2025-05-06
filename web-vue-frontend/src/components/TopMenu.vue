<script setup lang="ts">
import { ref, reactive, watch, onMounted } from 'vue';
import UserService from '@/services/userService';
// Імпортуємо потрібні типи з сервісу
// Прибрано IUserProfile та INewUser, оскільки повний об'єкт профілю більше не потрібен у user.value для цього сценарію
import type { ILoginResponse, ICreatedUser, ILoginCredentials } from '@/services/userService';
import axios from 'axios';


interface ApiError extends Error {
  message: string;
  statusCode?: number;
}

// --- Стани компонента ---
const isLoggedIn = ref(false);
// user ref видалено, оскільки ми більше не зберігаємо повний об'єкт профілю тут
// const user = ref<User | null>(null);
const currentUsername = ref<string | null>(null); // Зберігає ім'я користувача
const currentEmail = ref<string | null>(null); // НОВИЙ стан для зберігання email
const isLoading = ref(false);
const userService = new UserService();

// --- Модальні вікна ---
const showLoginModal = ref(false);
const showSignupModal = ref(false);
// showEditModal перейменовано на showProfileDetails
const showProfileDetails = ref<boolean>(false);

// --- Форми (форму редагування видалено) ---
const loginForm = reactive({
  username: '',
  password: '',
});

const signupForm = reactive({
  username: '',
  email: '',
  password: '',
});

// editForm reactive object видалено, оскільки форми редагування більше немає
// const editForm = reactive({ ... });


// --- Повідомлення (повідомлення про редагування видалено) ---
const createUserError = ref<string | null>(null);
const createdUserMessage = ref<string | null>(null);
const loginError = ref<string | null>(null);
// editProfileError та editProfileSuccessMessage видалено
// const editProfileError = ref<string | null>(null);
// const editProfileSuccessMessage = ref<string | null>(null);


// --- Функції ---

const closeModals = () => {
  showLoginModal.value = false;
  showSignupModal.value = false;
  // showEditModal замінено на showProfileDetails
  showProfileDetails.value = false;
  loginError.value = null;
  createUserError.value = null;
  createdUserMessage.value = null;
  // Повідомлення про редагування також очищаються при закритті (видалені)
  // editProfileError.value = null;
  // editProfileSuccessMessage.value = null;
  // Скидання форми редагування більше не потрібне
  // Watcher скидання форми редагування видалено
};

const resetForms = () => {
  Object.assign(loginForm, { username: '', password: '' });
  Object.assign(signupForm, { username: '', email: '', password: '' });
  // editForm скидання видалено
  // Object.assign(editForm, { username: '', email: '', password: '' });
};

// --- Вхід користувача (оновлено для отримання email також) ---
const handleLogin = async () => {
   if (!loginForm.username || !loginForm.password) {
     loginError.value = "Будь ласка, введіть ім'я користувача та пароль.";
     return;
   }
   isLoading.value = true;
   loginError.value = null;
   // user.value = null; // user ref видалено
   currentUsername.value = null;
   currentEmail.value = null; // Очищаємо email
   isLoggedIn.value = false;
   localStorage.removeItem('authToken');
   delete axios.defaults.headers.common['Authorization'];

   try {
     const credentialsToSend: ILoginCredentials = {
       Username: loginForm.username,
       Password: loginForm.password
     };
     const loginResponse: ILoginResponse = await userService.login(credentialsToSend);

     if (!loginResponse.token) {
         throw new Error("Не отримано токен авторизації після входу.");
     }
     localStorage.setItem('authToken', loginResponse.token);
     axios.defaults.headers.common['Authorization'] = `Bearer ${loginResponse.token}`;

      // Після успішного отримання токена, отримуємо ім'я та email
      try {
         // Можна виконувати паралельно для швидкості, або послідовно
         const username = await userService.getCurrentUser(); // Отримуємо ім'я
         const email = await userService.getCurrentEmail(); // Отримуємо email

         currentUsername.value = username;
         currentEmail.value = email; // Зберігаємо отриманий email
         isLoggedIn.value = true;
         console.log('Успішний вхід та отримання даних користувача (ім\'я та email).');

      } catch(profileError) {
         console.error('Помилка отримання даних користувача після входу:', profileError);
         // Вважаємо помилкою логіну, якщо не вдалося отримати дані профілю після токена
         localStorage.removeItem('authToken');
         delete axios.defaults.headers.common['Authorization'];
         isLoggedIn.value = false;
         // user.value = null; // user ref видалено
         currentUsername.value = null;
         currentEmail.value = null;
         throw new Error(`Вхід успішний, але не вдалося завантажити дані користувача: ${ (profileError as Error).message || 'Невідома помилка.'}`);
      }

     resetForms();
     closeModals();

   } catch (error) {
     const err = error as ApiError;
     console.error('Помилка входу:', err);
     loginError.value = err.message || 'Сталася невідома помилка під час входу.';
      isLoggedIn.value = false;
      // user.value = null; // user ref видалено
      currentUsername.value = null;
      currentEmail.value = null; // Очищаємо email
      localStorage.removeItem('authToken');
      delete axios.defaults.headers.common['Authorization'];
   } finally {
     isLoading.value = false;
   }
};

// --- Реєстрація користувача (без змін) ---
const handleSignup = async () => {
   if (!signupForm.username.trim() || !signupForm.email.trim() || !signupForm.password.trim()) {
     createUserError.value = "Всі поля реєстрації обов'язкові.";
     return;
   }
   if (signupForm.password.length < 6) {
     createUserError.value = "Пароль повинен містити щонайменше 6 символів.";
     return;
   }

  isLoading.value = true;
  createUserError.value = null;
  createdUserMessage.value = null;

  try {
    const createdUserData: ICreatedUser = await userService.createUser({
      UserName: signupForm.username,
      Email: signupForm.email,
      Password: signupForm.password
    });

    createdUserMessage.value = `Ласкаво просимо, ${createdUserData.UserName}! Реєстрація успішна. Тепер ви можете увійти.`;
    resetForms();

    showSignupModal.value = false;
    showLoginModal.value = true;

  } catch (error) {
    const err = error as ApiError;
    console.error('Помилка реєстрації:', err);
    createUserError.value = `Не вдалося зареєструватися: ${err.message || 'Невідома помилка'}`;
  } finally {
    isLoading.value = false;
  }
};

// --- Вихід користувача (оновлено для очищення email) ---
const handleLogout = async () => {
    isLoading.value = true;

    try {
      // Optional: await userService.logout();

      localStorage.removeItem('authToken');
      delete axios.defaults.headers.common['Authorization'];

      isLoggedIn.value = false;
      // user.value = null; // user ref видалено
      currentUsername.value = null; // Скидаємо ім'я користувача
      currentEmail.value = null; // Скидаємо email
      resetForms();
      closeModals();
      console.log('User logged out.');

    } catch (error) {
      const err = error as ApiError;
      console.error('Помилка виходу:', err);
      localStorage.removeItem('authToken');
      delete axios.defaults.headers.common['Authorization'];
      isLoggedIn.value = false;
      // user.value = null; // user ref видалено
      currentUsername.value = null;
      currentEmail.value = null; // Скидаємо email
      resetForms();
      closeModals();
      alert(`Помилка виходу: ${err.message || 'Не вдалося вийти коректно. Стан очищено.'}`);
    } finally {
      isLoading.value = false;
    }
};


// --- Відновлення пароля (без змін) ---
const handleForgotPassword = async () => {
  alert('Функціонал відновлення пароля ще не реалізовано.');
};

// --- Функція редагування профілю видалена ---
// const handleEditCredentials = async () => { ... };


// --- Видалення акаунту (без змін) ---
const handleDeleteAccount = async () => {
   if (!isLoggedIn.value || !currentUsername.value) {
      console.warn('Спроба видалити акаунт без автентифікації.');
      alert('Ви не увійшли. Неможливо видалити акаунт.');
      return;
   }

   if (!confirm(`Ви ВПЕВНЕНІ, що хочете видалити акаунт "${currentUsername.value}"? Цю дію неможливо скасувати.`)) {
     return;
   }

   isLoading.value = true;

   try {
      await userService.deleteAccount();

      console.log('Акаунт успішно видалено на бекенді.');
      alert('Ваш акаунт успішно видалено.');

      handleLogout();

   } catch (error) {
      const err = error as ApiError;
      console.error('Помилка видалення акаунту:', err);
      alert(`Помилка видалення акаунту: ${err.message || 'Невідома помилка'}`);

   } finally {
      isLoading.value = false;
      closeModals(); // Закриваємо модалку профілю після спроби видалення
   }
};


// --- Watchers (видалено watcher для editModal) ---
// Watcher для showEditModal видалено, оскільки форми редагування більше немає.
// Якщо потрібна якась логіка при відкритті/закритті showProfileDetails, її треба додати тут.
// watch(showEditModal, ...) видалено.

// --- Перевірка стану авторизації при завантаженні компонента (оновлено для отримання email) ---
onMounted(async () => { // Робимо async, щоб використовувати await
  const token = localStorage.getItem('authToken');
  if (token) {
    console.log('Знайдено токен, спроба відновити сесію...');
    axios.defaults.headers.common['Authorization'] = `Bearer ${token}`;

    try {
        // Отримуємо ім'я та email при відновленні сесії
        const username = await userService.getCurrentUser();
        const email = await userService.getCurrentEmail();

        currentUsername.value = username;
        currentEmail.value = email; // Зберігаємо отриманий email
        isLoggedIn.value = true;
        console.log('Сесію відновлено (отримано ім\'я та email користувача).');

    } catch (error) {
      console.error('Не вдалося відновити сесію:', error);
      localStorage.removeItem('authToken');
      delete axios.defaults.headers.common['Authorization'];
      isLoggedIn.value = false;
      currentUsername.value = null;
      currentEmail.value = null; // Очищаємо email
    }
  }
});

</script>

<template>
  <div class="top-menu fixed-top">
    <div v-if="isLoading" class="loading-overlay">
      <div class="loading-spinner"></div>
    </div>

    <div v-if="isLoggedIn && currentUsername" class="greeting">
      {{ currentUsername }}
    </div>
    <div v-else class="greeting">
      Ви не увійшли
    </div>

    <div class="menu-actions">
      <template v-if="isLoggedIn">
        <button @click="showProfileDetails = true" :disabled="!isLoggedIn || isLoading">Мій профіль</button>
        <button @click="handleLogout" :disabled="isLoading">Вийти</button>
      </template>
      <template v-else>
        <button @click="showLoginModal = true" :disabled="isLoading">Увійти</button>
        <button @click="showSignupModal = true" :disabled="isLoading">Зареєструватися</button>
      </template>
    </div>

    <div v-if="showLoginModal" class="modal-overlay" @click.self="closeModals">
      <div class="modal-content">
        <h3>Вхід</h3>
        <form @submit.prevent="handleLogin">
          <div class="form-group">
            <label for="login-username">Логін:</label>
            <input
              type="text"
              id="login-username"
              v-model="loginForm.username"
              required
              placeholder="Введіть ваше ім'я користувача"
              :disabled="isLoading"
            />
          </div>
          <div class="form-group">
            <label for="login-password">Пароль:</label>
            <input
              type="password"
              id="login-password"
              v-model="loginForm.password"
              required
              placeholder="Введіть ваш пароль"
              :disabled="isLoading"
            />
          </div>
          <p v-if="loginError" class="error-message">{{ loginError }}</p>
          <div class="form-actions">
            <button type="button" class="forgot-password-button" @click="handleForgotPassword" :disabled="isLoading">Забули пароль?</button>
            <button type="button" @click="closeModals" :disabled="isLoading">Скасувати</button>
            <button type="submit" :disabled="isLoading || !loginForm.username || !loginForm.password">
              {{ isLoading ? 'Вхід...' : 'Увійти' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <div v-if="showSignupModal" class="modal-overlay" @click.self="closeModals">
      <div class="modal-content">
        <h3>Реєстрація</h3>
        <form @submit.prevent="handleSignup">
          <div class="form-group">
            <label for="signup-username">Ім'я користувача:</label>
            <input
              type="text"
              id="signup-username"
              v-model="signupForm.username"
              required
              placeholder="Введіть ім'я користувача"
              :disabled="isLoading"
            />
          </div>
          <div class="form-group">
            <label for="signup-email">Email:</label>
            <input
              type="text"
              id="signup-email"
              v-model="signupForm.email"
              required
              placeholder="Введіть ваш email"
              :disabled="isLoading"
            />
          </div>
          <div class="form-group">
            <label for="signup-password">Пароль (мін. 6 символів):</label>
            <input
              type="password"
              id="signup-password"
              v-model="signupForm.password"
              required
              placeholder="Введіть пароль"
              minlength="6"
              :disabled="isLoading"
            />
          </div>
          <p v-if="createUserError" class="error-message">{{ createUserError }}</p>
          <p v-if="createdUserMessage" class="success-message">{{ createdUserMessage }}</p>
          <div class="form-actions">
            <button type="button" @click="closeModals" :disabled="isLoading">Скасувати</button>
            <button type="submit" :disabled="isLoading || !signupForm.username || !signupForm.email || !signupForm.password || signupForm.password.length < 6">
              {{ isLoading ? 'Реєстрація...' : 'Зареєструватися' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <div v-if="showProfileDetails" class="modal-overlay" @click.self="closeModals">
  <div class="modal-content profile-card">
    <h3>Мій профіль</h3>

    <div class="profile-info">
      <div class="avatar-circle">
        <span>{{ currentUsername?.charAt(0).toUpperCase() || 'U' }}</span>
      </div>
      <div class="profile-details">
        <p><strong>Ім'я користувача:</strong> {{ currentUsername || 'Невідомо' }}</p>
        <p><strong>Email:</strong> {{ currentEmail || 'Невідомо' }}</p>
      </div>
    </div>

    <p v-if="!currentUsername && !currentEmail" class="error-message">Не вдалося завантажити дані профілю.</p>

    <div class="form-actions">
      <button type="button" class="delete-button" @click="handleDeleteAccount" :disabled="isLoading">Видалити акаунт</button>
      <button type="button" @click="closeModals" :disabled="isLoading">Закрити</button>
    </div>
  </div>
</div>

  </div>
</template>



<style scoped>
.profile-card {
  text-align: center;
}

.profile-info {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 20px;
  margin-bottom: 20px;
  flex-wrap: wrap;
}

.avatar-circle {
  width: 60px;
  height: 60px;
  background-color: #007bff;
  color: white;
  font-weight: bold;
  font-size: 1.5rem;
  display: flex;
  justify-content: center;
  align-items: center;
  border-radius: 50%;
  box-shadow: 0 2px 6px rgba(0,0,0,0.2);
}

.profile-details p {
  margin: 5px 0;
  font-size: 1rem;
  color: #333;
}

@media (max-width: 576px) {
  .profile-info {
    flex-direction: column;
    text-align: center;
  }

  .avatar-circle {
    margin-bottom: 10px;
  }
}

.top-menu {
  background-color: #f8f9fa;
  padding: 10px 20px;
  border-bottom: 1px solid #dee2e6;
  display: flex;
  justify-content: space-between;
  align-items: center;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.fixed-top {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  z-index: 1001;
  box-sizing: border-box;
}

.greeting {
  font-weight: bold;
  font-size: 1.1rem;
}

.menu-actions {
  display: flex;
  gap: 15px;
}

button {
  padding: 8px 15px;
  cursor: pointer;
  border: 1px solid #007bff;
  background-color: #007bff;
  color: white;
  border-radius: 4px;
  transition: all 0.2s ease;
  font-size: 0.9rem;
}

button:hover:not(:disabled) {
  background-color: #0056b3;
  border-color: #0056b3;
}

button:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}


.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.6);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}

.modal-content {
  background-color: white;
  padding: 30px;
  border-radius: 8px;
  box-shadow: 0 4px 15px rgba(0, 0, 0, 0.2);
  min-width: 300px;
  max-width: 450px;
  width: 90%;
}

.modal-content h3 {
  margin-top: 0;
  margin-bottom: 20px;
  text-align: center;
  color: #333;
}

.form-group {
  margin-bottom: 15px;
}

.form-group label {
  display: block;
  margin-bottom: 5px;
  font-weight: 500;
  color: #555;
}

.form-group input[type='text'],
.form-group input[type='email'],
.form-group input[type='password'] {
  width: 100%;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 4px;
  box-sizing: border-box;
  font-size: 1rem;
  transition: border-color 0.2s;
}

.form-group input:focus {
  border-color: #007bff;
  outline: none;
}

.form-actions {
  margin-top: 25px;
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  flex-wrap: wrap;
}

.form-actions button[type='button'] {
  background-color: #6c757d;
  border-color: #6c757d;
}

.form-actions button[type='button']:hover:not(:disabled) {
  background-color: #5a6268;
  border-color: #545b62;
}

/* Стиль для кнопки удаления аккаунта */
.form-actions button.delete-button {
  background-color: #dc3545;
  border-color: #dc3545;
  margin-right: auto;
}

.form-actions button.delete-button:hover:not(:disabled) {
  background-color: #c82333;
  border-color: #bd2130;
}

/* Стиль для кнопки "Забыли пароль?" */
.form-actions button[type='button'].forgot-password-button {
  background: none;
  border: none;
  color: #007bff;
  padding: 0;
  text-decoration: underline;
  cursor: pointer;
  margin-right: auto;
  font-size: 0.85rem;
}

.form-actions button[type='button'].forgot-password-button:hover {
  color: #0056b3;
}

/* Стили для сообщений */
.error-message {
  color: #dc3545;
  margin-top: 15px;
  text-align: center;
  font-size: 0.9rem;
}

.success-message {
  color: #28a745;
  margin-top: 15px;
  text-align: center;
  font-size: 0.9rem;
}

/* Индикатор загрузки */
.loading-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 2000;
}

.loading-spinner {
  border: 4px solid rgba(255, 255, 255, 0.3);
  border-radius: 50%;
  border-top: 4px solid #007bff;
  width: 40px;
  height: 40px;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

/* Адаптивность */
@media (max-width: 576px) {
  .top-menu {
    flex-direction: column;
    gap: 10px;
    padding: 10px;
  }
  
  .menu-actions {
    width: 100%;
    justify-content: center;
  }
  
  .modal-content {
    padding: 20px;
  }
  
  .form-actions {
    flex-direction: column;
    gap: 8px;
  }
  
  .form-actions button {
    width: 100%;
  }
  
  .form-actions button.delete-button {
    order: 1;
    margin-right: 0;
    margin-bottom: 10px;
  }
}
</style>