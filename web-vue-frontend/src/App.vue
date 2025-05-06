<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import LeaderboardService from '@/services/leaderboard';
import type { ILeader } from '@/types/leaders';
import TopMenu from '@/components/TopMenu.vue';

const leaderboard = ref<ILeader[]>([]);

onMounted(async () => {
  try {
    const service = new LeaderboardService();
    const data = await service.getLeaderboard();
    leaderboard.value = data;
  } catch (err) {
    console.error('Помилка завантаження даних:', err);
  }
});

const leaderboardCount = computed(() => leaderboard.value.length);
</script>

<template>
  <TopMenu />
  <div class="page-wrapper">
    <div class="page-content-wrapper">
      <div class="content-container">
        <div class="centered-content">
          <h1>Ласкаво просимо!</h1>
          <p>Це головний вміст сторінки.</p>
        </div>

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
          <div v-else class="no-data">Наразі немає лідерів</div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
:global(body) {
  margin: 0;
  font-family: sans-serif;
  background-color: #000;
  color: white;
  min-height: 100vh;
  overflow-x: hidden;
}

.page-wrapper {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

.page-content-wrapper {
  display: flex;
  flex: 1;
  width: 100%;
  padding: 20px;
  box-sizing: border-box;
  justify-content: center;
  align-items: center;
}

.content-container {
  width: 100%;
  max-width: 2000px; /* Обмеження максимальної ширини для широких екранів */
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 40px;
}

.centered-content {
  text-align: center;
  width: 100%;
}

.leaderboard-container {
  width: 100%;
  max-width: 700px;
  padding: 30px 20px;
  background-color: #111;
  border-radius: 12px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
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
  background-color: #1b1b1b;
  padding: 12px 20px;
  border-radius: 8px;
  border: 1px solid #333;
  transition: background-color 0.2s;
  font-size: 16px;
}

.leaderboard-item:hover {
  background-color: #222;
}

.leader-rank {
  font-weight: bold;
  color: #007bff;
  min-width: 50px;
}

.leader-name {
  flex-grow: 1;
  padding-left: 12px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  color: white;
}

.leader-score {
  font-weight: 600;
  color: #28a745;
  min-width: 120px;
  text-align: right;
}

.no-data {
  margin-top: 20px;
  color: #888;
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