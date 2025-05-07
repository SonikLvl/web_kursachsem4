<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import LeaderboardService from '@/services/leaderboard';
import GameDataService, { type IGameData } from '@/services/gameDataService';
import type { ILeader } from '@/types/leaders';
import TopMenu from '@/components/TopMenu.vue';

const leaderboard = ref<ILeader[]>([]);
const gameService = new GameDataService();

const fetchLeaderboard = async () => {
  try {
    const service = new LeaderboardService();
    const data = await service.getLeaderboard();
    leaderboard.value = data;
  } catch (err) {
    console.error('Помилка завантаження даних таблиці лідерів:', err);
  }
};

onMounted(async () => {
  await fetchLeaderboard();

  (window as any).sendGameDataToWeb = async (jsonData: string) => {
    console.log('Дані отримані з Unity:', jsonData);

    try {
      const parsedData = JSON.parse(jsonData);

      if (typeof parsedData.score === 'number') {
        const gamePayload: IGameData = {
          score: parsedData.score,
        };

        const authToken = localStorage.getItem('authToken');

        if (!authToken) {
          console.error('Токен авторизації не знайдено. Неможливо надіслати рахунок.');
          alert('Помилка: Ви не авторизовані. Рахунок не буде збережено.');
          return;
        }

        console.log('Надсилання даних гри на бекенд:', gamePayload);
        await gameService.updateUserScore(gamePayload, authToken);
        console.log('Рахунок успішно надіслано та оновлено на бекенді!');
        await fetchLeaderboard();
        
      } else {
        console.error('Отримані дані з Unity не містять валідного поля "score".', parsedData);
        alert('Помилка: Не вдалося обробити дані з гри.');
      }
    } catch (error) {
      console.error('Помилка обробки або надсилання даних з Unity:', error);
      let userMessage = 'Сталася помилка під час збереження вашого рахунку.';
      if (error instanceof Error) {
        if (error.message.includes("401") || error.message.toLowerCase().includes("unauthorized")) {
            userMessage = 'Помилка авторизації. Можливо, ваша сесія закінчилася. Спробуйте увійти знову.';
        } else if (error.message.includes("Network Error") || error.message.toLowerCase().includes("failed to fetch")){
            userMessage = 'Помилка мережі. Не вдалося з\'єднатися з сервером.';
        }
      }
      alert(userMessage);
    }
  };

  console.log('Глобальна функція window.sendGameDataToWeb визначена та готова приймати дані з Unity.');
});

const leaderboardCount = computed(() => leaderboard.value.length);
</script>

<template>
  <TopMenu />
  
  <div class="page-wrapper">
    <div class="page-content-wrapper">
      <div class="content-container">
        
        <div class="leaderboard-container">
          <h2>Топ 10 лідерів</h2>

          <div v-if="leaderboard.length" class="leaderboard-list">
            <div
              v-for="(leader, index) in leaderboard.slice(0, 10)"
              :key="leader.userId" 
              class="leaderboard-item"
            >
              <span class="leader-rank">#{{ index + 1 }}</span>
              <span class="leader-name">{{ leader.userName }}</span>
              <span class="leader-score">{{ leader.scoreValue }} балів</span>
            </div>
          </div>
          <div v-else class="no-data">
            Завантаження даних лідерів... Або наразі немає лідерів.
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
:global(body) {
  margin: 0;
  font-family: sans-serif;
  background-color: #121212;
  color: #e0e0e0;
  min-height: 100vh;
  overflow-x: hidden;
}

.page-wrapper {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  background-color: #121212;
}

.page-content-wrapper {
  display: flex;
  flex: 1;
  width: 100%;
  padding: 20px;
  box-sizing: border-box;
  justify-content: center;
  align-items: center;
  background-color: #121212;
}

.content-container {
  width: 100%;
  max-width: 2000px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 40px;
}

.leaderboard-container {
  width: 100%;
  max-width: 700px;
  padding: 30px 20px;
  background-color: #1e1e1e;
  border-radius: 12px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.4);
  border: 1px solid #333;
}

.leaderboard-container h2 {
  text-align: center;
  color: #4dabf7;
  margin-bottom: 25px;
  text-shadow: 0 2px 4px rgba(0, 0, 0, 0.3);
}

.leaderboard-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-top: 20px;
}

.leaderboard-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background-color: #2d2d2d;
  padding: 12px 20px;
  border-radius: 8px;
  border: 1px solid #444;
  transition: all 0.2s ease;
  font-size: 16px;
}

.leaderboard-item:hover {
  background-color: #383838;
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.2);
}

.leader-rank {
  font-weight: bold;
  color: #74c0fc;
  min-width: 50px;
  flex-shrink: 0;
}

.leader-name {
  flex-grow: 1;
  padding: 0 15px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  color: #e0e0e0;
  text-align: left;
}

.leader-score {
  font-weight: 600;
  color: #69db7c;
  min-width: 120px;
  text-align: right;
  flex-shrink: 0;
}

.no-data {
  margin-top: 20px;
  color: #aaa;
  text-align: center;
  padding: 20px;
  background-color: #2d2d2d;
  border-radius: 8px;
  border: 1px solid #444;
}

@media (min-width: 1920px) {
  .content-container {
    max-width: 1400px;
  }
  
  .leaderboard-container {
    max-width: 900px;
  }
}
</style>