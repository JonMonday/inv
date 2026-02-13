import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '@/lib/api/client';
import { PagedRequest, PagedResponse } from '@/lib/api/types';

export function useRequests(request: PagedRequest = { pageNumber: 1, pageSize: 20 }) {
    return useQuery<PagedResponse<unknown[]>>({
        queryKey: ['requests', request],
        queryFn: async () => {
            const response = await apiClient.get('/api/inventory/requests', {
                params: request
            });
            return response.data;
        },
    });
}

export function useRequest(requestId: number | null) {
    return useQuery({
        queryKey: ['requests', requestId],
        queryFn: async () => {
            if (!requestId) return null;
            const response = await apiClient.get(`/api/inventory/requests/${requestId}`);
            return response.data.data;
        },
        enabled: !!requestId,
    });
}

export function useCreateRequest() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (data: unknown) => {
            const response = await apiClient.post('/api/inventory/requests', data);
            return response.data;
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['requests'] });
        },
    });
}

export interface ManualAssignment {
    workflowStepId: number;
    userIds: number[];
}

export function useSubmitRequest() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async ({ requestId, assignments }: { requestId: number, assignments?: ManualAssignment[] }) => {
            const response = await apiClient.post(`/api/inventory/requests/${requestId}/submit`, {
                manualAssignments: assignments
            });
            return response.data;
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['requests'] });
        },
    });
}

export function useTemplates(request: PagedRequest = { pageNumber: 1, pageSize: 50 }) {
    return useQuery<PagedResponse<unknown[]>>({
        queryKey: ['workflow', 'templates', request],
        queryFn: async () => {
            const response = await apiClient.get('/api/workflow/templates', { params: request });
            return response.data;
        },
    });
}

export function useFulfillment() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async ({ requestId, action, data }: { requestId: number, action: 'reserve' | 'release' | 'issue', data: unknown }) => {
            const response = await apiClient.post(`/api/inventory/requests/${requestId}/fulfillment/${action}`, data);
            return response.data;
        },
        onSuccess: (_, variables) => {
            queryClient.invalidateQueries({ queryKey: ['requests', variables.requestId] });
        },
    });
}
export function useFulfillmentDetails(requestId: number | null) {
    return useQuery({
        queryKey: ['requests', requestId, 'fulfillment-details'],
        queryFn: async () => {
            if (!requestId) return null;
            const response = await apiClient.get(`/api/inventory/requests/${requestId}/fulfillment-details`);
            return response.data.data;
        },
        enabled: !!requestId,
    });
}
export function useRequestHistory(requestId: number | null) {
    return useQuery({
        queryKey: ['requests', requestId, 'history'],
        queryFn: async () => {
            if (!requestId) return null;
            const response = await apiClient.get(`/api/inventory/requests/${requestId}/history`);
            return response.data.data;
        },
        enabled: !!requestId,
    });
}
export function useUpdateFulfillmentWarehouse() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async ({ requestId, warehouseId }: { requestId: number, warehouseId: number }) => {
            const response = await apiClient.put(`/api/inventory/requests/${requestId}/fulfillment/warehouse`, { warehouseId });
            return response.data;
        },
        onSuccess: (_, variables) => {
            queryClient.invalidateQueries({ queryKey: ['requests', variables.requestId] });
        },
    });
}

export function useUpdateRequest() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async ({ requestId, data }: { requestId: number, data: any }) => {
            const response = await apiClient.put(`/api/inventory/requests/${requestId}`, data);
            return response.data;
        },
        onSuccess: (_, variables) => {
            queryClient.invalidateQueries({ queryKey: ['requests', variables.requestId] });
        },
    });
}
export function useUpdateFulfillmentQuantities() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async ({ requestId, quantities }: { requestId: number, quantities: { productId: number, quantity: number }[] }) => {
            const response = await apiClient.put(`/api/inventory/requests/${requestId}/fulfillment/quantities`, { quantities });
            return response.data;
        },
        onSuccess: (_, variables) => {
            queryClient.invalidateQueries({ queryKey: ['requests', variables.requestId] });
        },
    });
}
