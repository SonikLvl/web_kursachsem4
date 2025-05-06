// vite.config.js
import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import path from 'path';

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    proxy: {
      // Проксіювати запити, що починаються з '/api'
      '/api': {
        // Адреса вашого бекенд-сервера, ВРАХОВУЮЧИ HTTPS та ПОРТ
        target: 'https://localhost:7167', // <--- ЗМІНЕНО НА HTTPS!
        // Дозволити проксіювання з http на https
        changeOrigin: true,
        // Оскільки ваш бекенд вже очікує '/api' у шляху, НЕ ПОТРІБНО його видаляти!
        // Видаліть або закоментуйте правило rewrite
        // rewrite: (path) => path.replace(/^\/api/, ''), // <-- ЦЕЙ РЯДОК ПОТРІБНО ВИДАЛИТИ АБО ЗАКОМЕНТУВАТИ

        // Оскільки це localhost на HTTPS, часто потрібно відключити перевірку SSL-сертифіката
        secure: false, // <--- ДОДАНО ЦЮ НАСТРОЙКУ ДЛЯ HTTPS НА LOCALHOST
      },
    },
  },
});