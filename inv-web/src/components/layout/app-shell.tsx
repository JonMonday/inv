'use client';

import { useEffect } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { useAuthStore } from '@/store/authStore';
import { Sidebar } from './sidebar';
import { TopBar } from './top-bar';
import apiClient from '@/lib/api/client';

export function AppShell({ children }: { children: React.ReactNode }) {
    const { isAuthenticated, accessToken, user, _hasHydrated, setAuth, clearAuth } = useAuthStore();
    const router = useRouter();
    const pathname = usePathname();

    useEffect(() => {
        // Wait for hydration before checking auth
        if (!_hasHydrated) return;

        if (!isAuthenticated) {
            router.push('/login');
            return;
        }

        // Only fetch profile if not already loaded
        if (!user) {
            apiClient.get('/api/auth/me')
                .then((res) => {
                    setAuth(res.data.data, accessToken!);
                })
                .catch(() => {
                    clearAuth();
                });
        }
    }, [isAuthenticated, user, _hasHydrated, router, accessToken, setAuth, clearAuth]);

    // Show nothing while hydrating
    if (!_hasHydrated) return null;

    if (!isAuthenticated) return null;

    // Map pathname to title
    const getTitle = () => {
        if (pathname === '/tasks') return 'My Tasks';
        if (pathname === '/requests') return 'Inventory Requests';
        if (pathname.startsWith('/requests/new')) return 'New Request';
        if (pathname.startsWith('/requests/')) return 'Request Details';
        if (pathname.startsWith('/admin/users')) return 'User Management';
        if (pathname.startsWith('/admin/roles')) return 'Role Management';
        return 'Dashboard';
    };

    return (
        <div className="flex h-screen overflow-hidden bg-background">
            <Sidebar />
            <div className="flex flex-1 flex-col overflow-hidden">
                <TopBar title={getTitle()} />
                <main className="flex-1 overflow-y-auto p-6">
                    {children}
                </main>
            </div>
        </div>
    );
}
