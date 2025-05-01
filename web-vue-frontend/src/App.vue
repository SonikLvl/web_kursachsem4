<script setup lang="ts">
// import { RouterLink, RouterView } from 'vue-router' // Закоментовано, як у вашому прикладі
// import HelloWorld from './components/HelloWorld.vue' // Закоментовано
import TopMenu from './components/TopMenu.vue' // Імпортуємо ваше меню
</script>

<template>
  <TopMenu />

  <div class="page-content-wrapper">
    <div class="centered-content">
      <h1>Ласкаво просимо!</h1>
      <p>Це головний вміст сторінки.</p>
    </div>
  </div>
</template>

<style scoped>
:global(body) {
  margin: 0;
  font-family: sans-serif;
}

.page-content-wrapper {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: stretch;
  min-height: 90lvh;
  width: 100%;
  padding-top: 65px;
}

.centered-content {
  padding: 20px;
  text-align: center;
}
</style>
