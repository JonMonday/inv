import { useQuery } from '@tanstack/react-query';
import apiClient from '@/lib/api/client';

export interface ReferenceItem {
    id: number;
    code: string;
    name: string;
    description: string;
    isActiveOrTerminal: boolean;
}

export function useReferenceList(type: string) {
    return useQuery<ReferenceItem[]>({
        queryKey: ['reference', type],
        queryFn: async () => {
            const response = await apiClient.get(`/api/reference/${type}`);
            return response.data.data;
        },
        enabled: !!type,
    });
}
