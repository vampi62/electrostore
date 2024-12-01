import { defineStore } from 'pinia';

export const useSessionTokenStore = defineStore({
    id: 'sessionToken',
    state: () => ({
      token: localStorage.getItem('token') || null
    }),
    actions: {
      setToken(token) {
        this.token = token
        localStorage.setItem('token', token)
      },
      clearToken() {
        this.token = null
        localStorage.removeItem('token')
      }
    }
});
