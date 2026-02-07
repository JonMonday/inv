import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '@/lib/api/client';
import { PagedRequest, PagedResponse } from '@/lib/api/types';

export interface Task {
    id: number;
    title: string;
    description: string;
    status: 'PENDING' | 'CLAIMED' | 'COMPLETED' | 'CANCELLED';
    priority: string;
    createdAt: string;
    claimedByUserId?: number;
    claimedByUserName?: string;
    requestId: number;
    initiatorUserId: number;
    stepName: string;
    stepKey: string;
}

export function useTasks(request: PagedRequest = { pageNumber: 1, pageSize: 20 }) {
    const queryClient = useQueryClient();

    const tasksQuery = useQuery<PagedResponse<Task[]>>({
        queryKey: ['tasks', 'my', request],
        queryFn: async () => {
            const response = await apiClient.get('/api/workflow/tasks/my', { params: request });
            return response.data;
        },
    });

    const claimMutation = useMutation({
        mutationFn: async (taskId: number) => {
            const response = await apiClient.post(`/api/workflow/tasks/${taskId}/claim`);
            return response.data;
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['tasks'] });
        },
    });

    const actionMutation = useMutation({
        mutationFn: async ({ taskId, action, notes, payloadJson }: {
            taskId: number;
            action: 'APPROVE' | 'REJECT' | 'CANCEL' | 'COMPLETE';
            notes?: string;
            payloadJson?: string;
        }) => {
            const response = await apiClient.post(`/api/workflow/tasks/${taskId}/action`, {
                actionCode: action,
                notes,
                payloadJson
            });
            return response.data;
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['tasks'] });
        },
    });

    return {
        tasksQuery,
        claimMutation,
        actionMutation,
    };
}

export function useEligibleAssignees(taskId: number | null) {
    return useQuery({
        queryKey: ['tasks', taskId, 'eligible-assignees'],
        queryFn: async () => {
            if (!taskId) return [];
            const response = await apiClient.get(`/api/workflow/tasks/${taskId}/eligible-assignees`);
            return response.data.data;
        },
        enabled: !!taskId,
    });
}
