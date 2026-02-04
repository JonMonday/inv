import { useQuery } from '@tanstack/react-query';
import apiClient from '@/lib/api/client';
import { PagedRequest, PagedResponse } from '@/lib/api/types';

export function useMovements(request: PagedRequest = { pageNumber: 1, pageSize: 20 }) {
    return useQuery<PagedResponse<unknown[]>>({
        queryKey: ['inventory', 'movements', request],
        queryFn: async () => {
            const response = await apiClient.get('/api/stock/movements', { params: request });
            return response.data;
        },
    });
}

export function useWarehouses(request: PagedRequest = { pageNumber: 1, pageSize: 100 }) {
    return useQuery<PagedResponse<unknown[]>>({
        queryKey: ['reference', 'warehouses', request],
        queryFn: async () => {
            const response = await apiClient.get('/api/reference/warehouses', { params: request });
            return response.data;
        },
    });
}

export function useCategories(request: PagedRequest = { pageNumber: 1, pageSize: 100 }) {
    return useQuery<PagedResponse<unknown[]>>({
        queryKey: ['reference', 'categories', request],
        queryFn: async () => {
            const response = await apiClient.get('/api/reference/categories', { params: request });
            return response.data;
        },
    });
}

export function useStockLevels(warehouseId?: number, productId?: number) {
    return useQuery({
        queryKey: ['inventory', 'levels', warehouseId, productId],
        queryFn: async () => {
            const response = await apiClient.get('/api/reference/stock/levels', {
                params: { warehouseId, productId }
            });
            return response.data.data;
        },
    });
}
