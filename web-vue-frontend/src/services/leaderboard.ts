import axios from 'axios'
import type ILeader from '@/types/leaders'

export default class LeaderboardService {
  public async getLeaderboard(): Promise<ILeader[]> {
    //console.log(axios.isCancel('something'))
    /*API_URL = process.env.VUE_APP_API_URL
    let result = await axios.get(`${this.API_URL}`)*/
  }
}
