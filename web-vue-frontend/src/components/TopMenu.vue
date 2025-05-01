<template>
  <nav class="top-menu fixed-top">
    <template v-if="!isLoggedIn">
      <span class="greeting">Привіт, гість</span>
      <div class="menu-actions">
        <button @click="showSignupModal = true">Зареєструватися</button>
        <button @click="showLoginModal = true">Увійти</button>
      </div>
    </template>

    <template v-else>
      <span class="greeting">Привіт, {{ user?.username || 'Користувач' }}</span>
      <div class="menu-actions">
        <button @click="showEditModal = true">Редагувати профіль</button>
        <button @click="handleLogout">Вийти</button>
      </div>
    </template>
  </nav>

  <div v-if="showSignupModal" class="modal-overlay" @click.self="closeModals">
    <div class="modal-content">
      <h3>Реєстрація</h3>
      <form @submit.prevent="handleSignup">
        <div class="form-group">
          <label for="signup-username">Ім'я користувача:</label>
          <input id="signup-username" v-model="signupForm.username" type="text" required>
        </div>
        <div class="form-group">
          <label for="signup-email">Email:</label>
          <input id="signup-email" v-model="signupForm.email" type="email" required>
        </div>
        <div class="form-group">
          <label for="signup-password">Пароль:</label>
          <input id="signup-password" v-model="signupForm.password" type="password" required>
        </div>
        <div class="form-actions">
          <button type="submit">Зареєструватися</button>
          <button type="button" @click="closeModals">Скасувати</button>
        </div>
      </form>
    </div>
  </div>

  <div v-if="showLoginModal" class="modal-overlay" @click.self="closeModals">
    <div class="modal-content">
      <h3>Вхід</h3>
      <form @submit.prevent="handleLogin">
        <div class="form-group">
          <label for="login-email">Email:</label>
          <input id="login-email" v-model="loginForm.email" type="email" required>
        </div>
        <div class="form-group">
          <label for="login-password">Пароль:</label>
          <input id="login-password" v-model="loginForm.password" type="password" required>
        </div>
        <div class="form-actions">
          <button type="submit">Увійти</button>
          <button type="button" @click="handleForgotPassword">Забули пароль?</button>
          <button type="button" @click="closeModals">Скасувати</button>
        </div>
      </form>
    </div>
  </div>

  <div v-if="showEditModal" class="modal-overlay" @click.self="closeModals">
    <div class="modal-content">
      <h3>Редагувати профіль</h3>
      <form @submit.prevent="handleEditCredentials">
        <div class="form-group">
          <label for="edit-username">Ім'я користувача:</label>
          <input id="edit-username" v-model="editForm.username" type="text" required>
        </div>
        <div class="form-group">
          <label for="edit-email">Email:</label>
          <input id="edit-email" v-model="editForm.email" type="email" required>
        </div>
        <div class="form-group">
          <label for="edit-password">Новий пароль (залиште пустим, щоб не змінювати):</label>
          <input id="edit-password" v-model="editForm.password" type="password">
        </div>
        <div class="form-actions">
          <button type="submit">Зберегти</button>
          <button type="button" @click="handleDeleteAccount" class="delete-button">Видалити акаунт</button>
          <button type="button" @click="closeModals">Скасувати</button>
        </div>
      </form>
    </div>
  </div>

</template>

<script setup>
  import { ref, reactive, watch } from 'vue';

  // --- Стан компонента ---

  // Стан автентифікації (в реальному додатку це буде керуватися через store або provide/inject)
  const isLoggedIn = ref(false);
  const user = ref(null); // { id: 1, username: 'TestUser', email: 'test@example.com' }

  // Стан видимості модальних вікон
  const showLoginModal = ref(false);
  const showSignupModal = ref(false);
  const showEditModal = ref(false);

  // Дані форм
  const loginForm = reactive({
    email: '',
    password: '',
  });

  const signupForm = reactive({
    username: '',
    email: '',
    password: '',
  });

  const editForm = reactive({
    username: '',
    email: '',
    password: '', // Зазвичай відправляють тільки якщо змінюють
  });

  // --- Функції ---

  // Закриття всіх модальних вікон
  const closeModals = () => {
    showLoginModal.value = false;
    showSignupModal.value = false;
    showEditModal.value = false;
  };

  // Очищення полів форм
  const resetForms = () => {
    Object.assign(loginForm, { email: '', password: '' });
    Object.assign(signupForm, { username: '', email: '', password: '' });
    // Не очищуємо editForm повністю при закритті, а оновлюємо при відкритті
  };

  // Обробник Входу
  const handleLogin = async () => {
    console.log('Спроба входу:', loginForm);
    // !!! Тут має бути запит до вашого API для автентифікації !!!
    // Припустимо, API повертає дані користувача при успішному вході
    try {
      // const response = await fetch('/api/login', { /* ... */ });
      // const loggedInData = await response.json();
      // if (!response.ok) throw new Error(loggedInData.message || 'Login failed');

      // Симуляція успішного входу:
      await new Promise(resolve => setTimeout(resolve, 500)); // імітація затримки мережі
      const userData = { id: 1, username: 'DemoUser', email: loginForm.email }; // Симульовані дані
      user.value = userData;
      isLoggedIn.value = true;
      resetForms();
      closeModals();
      console.log('Успішний вхід:', user.value);

    } catch (error) {
      console.error("Помилка входу:", error);
      alert(`Помилка входу: ${error.message || 'Перевірте дані'}`); // Просте повідомлення про помилку
    }
  };

  // Обробник Реєстрації
  const handleSignup = async () => {
    console.log('Спроба реєстрації:', signupForm);
    // !!! Тут має бути запит до вашого API для реєстрації !!!
    // Припустимо, API повертає дані користувача після реєстрації та автоматично логінить його
    try {
      // const response = await fetch('/api/signup', { /* ... */ });
      // const signedUpData = await response.json();
      // if (!response.ok) throw new Error(signedUpData.message || 'Signup failed');

      // Симуляція успішної реєстрації:
      await new Promise(resolve => setTimeout(resolve, 500));
      const userData = { id: Date.now(), username: signupForm.username, email: signupForm.email }; // Симульовані дані
      user.value = userData;
      isLoggedIn.value = true;
      resetForms();
      closeModals();
      console.log('Успішна реєстрація та вхід:', user.value);

    } catch (error) {
      console.error("Помилка реєстрації:", error);
      alert(`Помилка реєстрації: ${error.message || 'Спробуйте ще раз'}`);
    }
  };

  // Обробник Виходу
  const handleLogout = async () => {
    console.log('Вихід...');
    // !!! Тут може бути запит до API для інвалідації сесії/токену !!!
    // await fetch('/api/logout', { /* ... */ });

    // Скидання стану на стороні клієнта
    isLoggedIn.value = false;
    user.value = null;
    resetForms(); // Очищаємо форми про всяк випадок
    closeModals(); // Закриваємо будь-які відкриті модалки
    console.log('Користувача розлогінено');
  };

  // Обробник "Забули пароль?"
  const handleForgotPassword = async () => {
    if (!loginForm.email) {
      alert("Будь ласка, введіть ваш Email у поле для входу, щоб ми могли надіслати посилання для відновлення.");
      return;
    }
    console.log('Запит на відновлення пароля для:', loginForm.email);
    // !!! Тут має бути запит до вашого API для відправки листа !!!
    try {
      // await fetch('/api/forgot-password', { method: 'POST', body: JSON.stringify({ email: loginForm.email }), /* ... */ });
      await new Promise(resolve => setTimeout(resolve, 500)); // Симуляція
      alert(`Посилання для відновлення пароля надіслано на ${loginForm.email} (якщо такий акаунт існує).`);
      closeModals(); // Можна закрити вікно входу після запиту
    } catch (error) {
      console.error("Помилка відновлення пароля:", error);
      alert("Не вдалося відправити запит на відновлення пароля.");
    }
  };

  // Обробник Збереження змін профілю
  const handleEditCredentials = async () => {
    console.log('Спроба оновлення профілю:', editForm);
    // !!! Тут має бути запит до вашого API для оновлення даних користувача !!!
    // Тіло запиту може містити тільки ті поля, що змінилися, або всі поля
    // Окремо перевіряти, чи введено новий пароль
    const dataToSend = {
      username: editForm.username,
      email: editForm.email,
      ...(editForm.password && { password: editForm.password }) // Додаємо пароль, тільки якщо він не пустий
    };

    try {
      // const response = await fetch(`/api/user/${user.value.id}`, { method: 'PUT', body: JSON.stringify(dataToSend), /* ... */ });
      // const updatedUserData = await response.json();
      // if (!response.ok) throw new Error(updatedUserData.message || 'Update failed');

      // Симуляція успішного оновлення
      await new Promise(resolve => setTimeout(resolve, 500));
      const updatedUserData = { ...user.value, username: editForm.username, email: editForm.email }; // Симульовані оновлені дані (без зміни паролю тут)
      user.value = updatedUserData; // Оновлюємо дані користувача в стані
      editForm.password = ''; // Очищуємо поле пароля після відправки
      closeModals();
      console.log('Профіль оновлено:', user.value);
      alert('Дані профілю успішно оновлено!');

    } catch (error) {
      console.error("Помилка оновлення профілю:", error);
      alert(`Помилка оновлення: ${error.message || 'Спробуйте ще раз'}`);
    }
  };

  // Обробник Видалення акаунту
  const handleDeleteAccount = async () => {
    if (!confirm('Ви ВПЕВНЕНІ, що хочете видалити свій акаунт? Цю дію неможливо скасувати.')) {
      return;
    }
    console.log('Видалення акаунту для:', user.value?.email);
    // !!! Тут має бути ЗАХИЩЕНИЙ запит до вашого API для видалення акаунту !!!
    try {
      // await fetch(`/api/user/${user.value.id}`, { method: 'DELETE', /* ... headers з токеном */ });
      await new Promise(resolve => setTimeout(resolve, 500)); // Симуляція
      console.log('Акаунт видалено');
      alert('Ваш акаунт було успішно видалено.');
      // Викликаємо логаут, щоб скинути стан на клієнті
      handleLogout();
    } catch (error) {
      console.error("Помилка видалення акаунту:", error);
      alert(`Помилка видалення: ${error.message || 'Спробуйте ще раз'}`);
    }
  };

  // Спостерігач для відкриття модального вікна редагування
  // Заповнює форму поточними даними користувача при відкритті
  watch(showEditModal, (newValue) => {
    if (newValue && user.value) {
      editForm.username = user.value.username;
      editForm.email = user.value.email;
      editForm.password = ''; // Завжди очищуємо поле пароля при відкритті
    }
  });

</script>

<style scoped>
  .top-menu {
    background-color: #f8f9fa;
    padding: 10px 20px; /* Паддінги зліва і справа */
    border-bottom: 1px solid #dee2e6;
    /* --- Нові стилі для Flexbox --- */
    display: flex; /* Вмикаємо Flexbox */
    justify-content: space-between; /* Розносимо дочірні елементи по краях */
    align-items: center; /* Вирівнюємо елементи по вертикалі по центру */
  }

  /* --- Стилі для фіксації --- */
  .fixed-top {
    position: fixed; /* Фіксоване позиціонування */
    top: 0; /* Прикріплюємо до верху */
    left: 0; /* Прикріплюємо до лівого краю */
    width: 100%; /* Займає всю ширину */
    z-index: 1001; /* Перекриває інший контент (окрім модалок) */
    box-sizing: border-box; /* Щоб padding не збільшував загальну ширину */
  }

  .greeting {
    font-weight: bold;
    /* Немає потреби в margin-right, space-between зробить відступ */
  }

  .menu-actions {
    display: flex; /* Вмикаємо Flexbox для групи кнопок */
    gap: 15px; /* Відстань між кнопками */
  }

  button {
    padding: 8px 15px;
    cursor: pointer;
    border: 1px solid #007bff;
    background-color: #007bff;
    color: white;
    border-radius: 4px;
    transition: background-color 0.2s ease;
  }

    button:hover {
      background-color: #0056b3;
      border-color: #0056b3;
    }

  /* Стилі для модальних вікон */
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
    z-index: 1000; /* Поверх інших елементів */
  }

  .modal-content {
    background-color: white;
    padding: 30px;
    border-radius: 8px;
    box-shadow: 0 4px 15px rgba(0, 0, 0, 0.2);
    min-width: 300px;
    max-width: 450px;
  }

    .modal-content h3 {
      margin-top: 0;
      margin-bottom: 20px;
      text-align: center;
    }

  .form-group {
    margin-bottom: 15px;
  }

    .form-group label {
      display: block;
      margin-bottom: 5px;
      font-weight: 500;
    }

    .form-group input[type="text"],
    .form-group input[type="email"],
    .form-group input[type="password"] {
      width: 100%;
      padding: 10px;
      border: 1px solid #ccc;
      border-radius: 4px;
      box-sizing: border-box; /* Важливо для input[type=text] */
    }

  .form-actions {
    margin-top: 25px;
    display: flex;
    justify-content: space-between; /* Розносимо основні кнопки */
    flex-wrap: wrap; /* Дозволяємо перенос кнопок */
    gap: 10px;
  }

    .form-actions button[type="button"] {
      background-color: #6c757d; /* Сірий для скасування */
      border-color: #6c757d;
    }

      .form-actions button[type="button"]:hover {
        background-color: #5a6268;
        border-color: #545b62;
      }

    .form-actions button.delete-button {
      background-color: #dc3545; /* Червоний для видалення */
      border-color: #dc3545;
      margin-left: auto; /* Притискаємо кнопку видалення праворуч, якщо є місце */
    }

      .form-actions button.delete-button:hover {
        background-color: #c82333;
        border-color: #bd2130;
      }

  /* Стиль для кнопки "Забули пароль?" */
  button[type="button"][data-action="forgot-password"] {
    background: none;
    border: none;
    color: #007bff;
    padding: 0;
    text-decoration: underline;
    cursor: pointer;
  }
</style>
