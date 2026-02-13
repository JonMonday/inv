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

    // Map pathname to breadcrumbs
    const getBreadcrumbs = () => {
        const segments: { label: string, href: string }[] = [];

        // Workflows
        if (pathname === '/tasks') return [{ label: 'Workflows', href: '#' }, { label: 'Inbox', href: '/tasks' }];
        if (pathname === '/requests/drafts') return [{ label: 'Workflows', href: '#' }, { label: 'Drafts', href: '/requests/drafts' }];
        if (pathname === '/requests/participated') return [{ label: 'Workflows', href: '#' }, { label: 'Participated', href: '/requests/participated' }];
        if (pathname === '/requests') return [{ label: 'Workflows', href: '#' }, { label: 'Requests', href: '/requests' }];
        if (pathname.startsWith('/requests/new')) return [{ label: 'Workflows', href: '#' }, { label: 'Requests', href: '/requests' }, { label: 'New Request', href: '#' }];
        if (pathname.startsWith('/requests/')) return [{ label: 'Workflows', href: '#' }, { label: 'Requests', href: '/requests' }, { label: 'Request Details', href: '#' }];

        // Operations
        if (pathname === '/') return [{ label: 'Operations', href: '#' }, { label: 'Dashboard', href: '/' }];
        if (pathname === '/fulfillment') return [{ label: 'Operations', href: '#' }, { label: 'Fulfillment', href: '/fulfillment' }];
        if (pathname === '/notifications') return [{ label: 'Operations', href: '#' }, { label: 'Notifications', href: '/notifications' }];
        if (pathname === '/profile') return [{ label: 'Operations', href: '#' }, { label: 'Profile', href: '/profile' }];

        // Inventory
        if (pathname === '/inventory/levels') return [{ label: 'Inventory', href: '#' }, { label: 'Stock Levels', href: '/inventory/levels' }];
        if (pathname === '/inventory/levels/movement') return [{ label: 'Inventory', href: '#' }, { label: 'Stock Levels', href: '/inventory/levels' }, { label: 'Manual Movement', href: '#' }];
        if (pathname === '/inventory/movements') return [{ label: 'Inventory', href: '#' }, { label: 'Movements', href: '/inventory/movements' }];

        if (pathname === '/reference/products') return [{ label: 'Inventory', href: '#' }, { label: 'Reference', href: '#' }, { label: 'Products', href: '/reference/products' }];
        if (pathname === '/reference/products/add') return [{ label: 'Inventory', href: '#' }, { label: 'Reference', href: '#' }, { label: 'Products', href: '/reference/products' }, { label: 'Add Product', href: '#' }];

        if (pathname === '/reference/categories') return [{ label: 'Inventory', href: '#' }, { label: 'Reference', href: '#' }, { label: 'Categories', href: '/reference/categories' }];
        if (pathname === '/reference/categories/add') return [{ label: 'Inventory', href: '#' }, { label: 'Reference', href: '#' }, { label: 'Categories', href: '/reference/categories' }, { label: 'Add Category', href: '#' }];

        if (pathname === '/reference/warehouses') return [{ label: 'Inventory', href: '#' }, { label: 'Reference', href: '#' }, { label: 'Warehouses', href: '/reference/warehouses' }];
        if (pathname === '/reference/warehouses/add') return [{ label: 'Inventory', href: '#' }, { label: 'Reference', href: '#' }, { label: 'Warehouses', href: '/reference/warehouses' }, { label: 'Add Warehouse', href: '#' }];

        // Admin
        if (pathname === '/admin/workflows') return [{ label: 'Admin', href: '#' }, { label: 'Workflow Templates', href: '/admin/workflows' }];
        if (pathname.startsWith('/admin/users')) return [{ label: 'Admin', href: '#' }, { label: 'User Management', href: '/admin/users' }];
        if (pathname.startsWith('/admin/roles')) return [{ label: 'Admin', href: '#' }, { label: 'Role Management', href: '/admin/roles' }];
        if (pathname === '/admin/permissions') return [{ label: 'Admin', href: '#' }, { label: 'Permissions', href: '/admin/permissions' }];
        if (pathname === '/admin/departments') return [{ label: 'Admin', href: '#' }, { label: 'Departments', href: '/admin/departments' }];

        return [{ label: 'Dashboard', href: '/' }];
    };

    return (
        <div className="flex h-screen overflow-hidden bg-background">
            <Sidebar />
            <div className="flex flex-1 flex-col overflow-hidden">
                <TopBar breadcrumbs={getBreadcrumbs()} />
                <main className="flex-1 overflow-y-auto p-6">
                    {children}
                </main>
            </div>
        </div>
    );
}
