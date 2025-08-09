import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore } from '@/stores/auth.store'

// Mock localStorage
const localStorageMock = (() => {
  let store = {}
  return {
    getItem: vi.fn((key) => store[key] || null),
    setItem: vi.fn((key, value) => {
      store[key] = value.toString()
    }),
    removeItem: vi.fn((key) => {
      delete store[key]
    }),
    clear: vi.fn(() => {
      store = {}
    })
  }
})()

Object.defineProperty(window, 'localStorage', {
  value: localStorageMock
})

// Mock fetchWrapper
vi.mock('@/helpers', () => ({
  fetchWrapper: {
    post: vi.fn()
  },
  router: {
    push: vi.fn()
  }
}))

describe('Auth Store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    localStorageMock.clear()
  })

  it('initializes with empty state when localStorage is empty', () => {
    const store = useAuthStore()
    expect(store.user).toBeNull()
    expect(store.accessToken).toBeNull()
    expect(store.refreshToken).toBeNull()
  })

  it('sets token and user information correctly', () => {
    const store = useAuthStore()
    const user = { id: 1, name: 'Test User' }
    const accessToken = { token: 'access-token', date_expire: '2023-12-31' }
    const refreshToken = { token: 'refresh-token', date_expire: '2023-12-31' }

    store.setToken(user, accessToken, refreshToken)

    expect(store.user).toEqual(user)
    expect(store.accessToken).toEqual(accessToken)
    expect(store.refreshToken).toEqual(refreshToken)
    expect(localStorageMock.setItem).toHaveBeenCalledWith('user', JSON.stringify(user))
    expect(localStorageMock.setItem).toHaveBeenCalledWith('accessToken', JSON.stringify(accessToken))
    expect(localStorageMock.setItem).toHaveBeenCalledWith('refreshToken', JSON.stringify(refreshToken))
  })

  it('clears token and user information correctly', () => {
    const store = useAuthStore()
    store.user = { id: 1, name: 'Test User' }
    store.accessToken = { token: 'access-token', date_expire: '2023-12-31' }
    store.refreshToken = { token: 'refresh-token', date_expire: '2023-12-31' }

    store.clearToken()

    expect(store.user).toBeNull()
    expect(store.accessToken).toBeNull()
    expect(store.refreshToken).toBeNull()
    expect(localStorageMock.removeItem).toHaveBeenCalledWith('user')
    expect(localStorageMock.removeItem).toHaveBeenCalledWith('accessToken')
    expect(localStorageMock.removeItem).toHaveBeenCalledWith('refreshToken')
  })
})