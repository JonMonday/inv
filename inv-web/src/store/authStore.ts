import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface User {
    userId: number;
    username: string;
    displayName: string;
    email: string;
    roles: string[];
    departments: string[];
    primaryDepartmentId?: number;
}

interface AuthState {
    user: User | null;
    accessToken: string | null;
    isAuthenticated: boolean;
    _hasHydrated: boolean;
    setAuth: (user: User, accessToken: string) => void;
    clearAuth: () => void;
    updateAccessToken: (accessToken: string) => void;
    setHasHydrated: (state: boolean) => void;
}

export const useAuthStore = create<AuthState>()(
    persist(
        (set) => ({
            user: null,
            accessToken: null,
            isAuthenticated: false,
            _hasHydrated: false,
            setAuth: (user, accessToken) => set({ user, accessToken, isAuthenticated: true }),
            clearAuth: () => set({ user: null, accessToken: null, isAuthenticated: false }),
            updateAccessToken: (accessToken) => set({ accessToken }),
            setHasHydrated: (state) => set({ _hasHydrated: state }),
        }),
        {
            name: 'auth-storage',
            //@ts-expect-error - persist middleware type mismatch
            getStorage: () => localStorage,
            onRehydrateStorage: () => (state) => {
                state?.setHasHydrated(true);
            },
        }
    )
);
