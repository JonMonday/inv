import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '@/lib/api/client'; // Updated hook list
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

export function useUnitsOfMeasure(request: PagedRequest = { pageNumber: 1, pageSize: 100 }) {
    return useQuery<PagedResponse<any[]>>({
        queryKey: ['reference', 'units-of-measure', request],
        queryFn: async () => {
            const response = await apiClient.get('/api/reference/units-of-measure', { params: request });
            return response.data;
        },
    });
}

export function useMovementTypes(request: PagedRequest = { pageNumber: 1, pageSize: 100 }) {
    return useQuery<PagedResponse<unknown[]>>({
        queryKey: ['reference', 'movement-types', request],
        queryFn: async () => {
            const response = await apiClient.get('/api/reference/inventory-movement-type', { params: request });
            return response.data;
        },
    });
}

export function usePostMovement() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (data: any) => {
            return await apiClient.post('/api/stock/post', data);
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['inventory', 'levels'] });
            queryClient.invalidateQueries({ queryKey: ['inventory', 'movements'] });
        }
    });
}

export function useCreateCategory() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (data: { name: string, parentCategoryId?: number | null }) => {
            return await apiClient.post('/api/catalog/categories', data);
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['reference', 'categories'] });
        }
    });
}

export function useCreateProduct() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (data: { sku: string, name: string, categoryId?: number | null, unitOfMeasureId: number, reorderLevel: number }) => {
            return await apiClient.post('/api/catalog/products', data);
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['catalog', 'products'] });
            queryClient.invalidateQueries({ queryKey: ['inventory', 'levels'] });
        }
    });
}

export function useUpdateReorderLevel() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async ({ id, reorderLevel }: { id: number, reorderLevel: number }) => {
            return await apiClient.patch(`/api/catalog/products/${id}/reorder-level`, { reorderLevel });
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['catalog', 'products'] });
            queryClient.invalidateQueries({ queryKey: ['inventory', 'levels'] });
        }
    });
}

export function useCreateWarehouse() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (data: { name: string, location?: string | null }) => {
            return await apiClient.post('/api/catalog/warehouses', data);
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['reference', 'warehouses'] });
            queryClient.invalidateQueries({ queryKey: ['inventory', 'levels'] });
        }
    });
}
