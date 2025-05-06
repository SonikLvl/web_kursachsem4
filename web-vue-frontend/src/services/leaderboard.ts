import axios from 'axios';
import type ILeader from '@/types/leaders';

export default class LeaderboardService {
    // PRIVATE_API_URL більше не потрібен, оскільки використовуємо відносний шлях та проксі Vite
    // private readonly API_URL: string;

    constructor() {
        // Навіть якщо API_URL була б потрібна, для Vite використовується import.meta.env.VITE_API_URL
        // Цей рядок можна видалити, якщо API_URL не використовується
        // this.API_URL = import.meta.env.VITE_API_URL;
    }

    public async getLeaderboard(): Promise<ILeader[]> {
        try {
            // Змінено шлях з '/api/leaders' на '/api/leaderboard', щоб відповідати бекенду
            const result = await axios.get('/api/leaderboard');
            console.log(result.data);
            // Перевірка, чи result.data є масивом, якщо ILeader[]
            if (Array.isArray(result.data)) {
                 return result.data;
            } else {
                 // Якщо дані не масив, можливо, бекенд повернув інший формат або помилку
                 console.error('Backend returned data in unexpected format:', result.data);
                 // Можна повернути порожній масив або кинути помилку, залежно від бажаної поведінки
                 return []; // Повертаємо порожній масив, якщо дані не є масивом
                 // throw new Error('Invalid data format received from API'); // Або кинути помилку
            }

        } catch (error) {
            console.error('Error fetching leaderboard:', error);
            // Кидаємо помилку далі, щоб компонент міг її обробити
            throw error;
        }
    }
}