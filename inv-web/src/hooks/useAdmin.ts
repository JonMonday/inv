import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '@/lib/api/client';
import { PagedRequest, PagedResponse } from '@/lib/api/types';

export interface User {
    userId: number;
    username: string;
    email: string;
    displayName: string;
    isActive: boolean;
    roles: string[];
}

export interface Role {
    roleId: number;
    name: string;
    code: string;
    description?: string;
    userCount: number;
    isActive: boolean;
}

export interface Department {
    departmentId: number;
    name: string;
    code: string;
}

export interface AuditLog {
    auditLogId: number;
    action: string;
    timestamp: string;
    performedBy: string;
    oldValue: string;
    newValue: string;
}

export interface Permission {
    permissionId: number;
    code: string;
    name: string;
    description: string;
    isActive: boolean;
}

export interface WorkflowTemplate {
    id: number;
    code: string;
    name: string;
    definitionJson?: string;
    latestVersion?: number;
    steps?: any[];
    transitions?: any[];
}

export function useUsers(request: PagedRequest = { pageNumber: 1, pageSize: 10 }) {
    return useQuery<PagedResponse<User[]>>({
        queryKey: ['admin', 'users', request],
        queryFn: async () => {
            const response = await apiClient.get('/api/admin/users', { params: request });
            return response.data;
        },
    });
}

export function useUser(userId: number | null) {
    return useQuery<User & { roleIds: number[] }>({
        queryKey: ['admin', 'users', userId],
        queryFn: async () => {
            const response = await apiClient.get(`/api/admin/users/${userId}`);
            return response.data.data;
        },
        enabled: !!userId,
    });
}

export function useRoles(request: PagedRequest = { pageNumber: 1, pageSize: 10 }) {
    return useQuery<PagedResponse<Role[]>>({
        queryKey: ['admin', 'roles', request],
        queryFn: async () => {
            const response = await apiClient.get('/api/admin/roles', { params: request });
            return response.data;
        },
    });
}

export function usePermissions(request: PagedRequest = { pageNumber: 1, pageSize: 50 }) {
    return useQuery<PagedResponse<Permission[]>>({
        queryKey: ['admin', 'permissions', request],
        queryFn: async () => {
            const response = await apiClient.get('/api/admin/permissions', { params: request });
            return response.data;
        },
    });
}

export function useDepartments() {
    return useQuery<Department[]>({
        queryKey: ['admin', 'departments'],
        queryFn: async () => {
            const response = await apiClient.get('/api/admin/departments');
            return response.data.data;
        },
    });
}

export function useCreateUser() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (data: Partial<User> & { roleIds?: number[] }) => {
            const response = await apiClient.post('/api/admin/users', data);
            return response.data;
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['admin', 'users'] });
        },
    });
}

export function useUpdateUser() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async ({ userId, data }: { userId: number, data: Partial<User> & { roleIds?: number[] } }) => {
            const response = await apiClient.put(`/api/admin/users/${userId}`, data);
            return response.data;
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['admin', 'users'] });
        },
    });
}

export function useAuditLogs(request: PagedRequest = { pageNumber: 1, pageSize: 20 }) {
    return useQuery<PagedResponse<AuditLog[]>>({
        queryKey: ['admin', 'audit-logs', request],
        queryFn: async () => {
            const response = await apiClient.get('/api/admin/audit-logs', { params: request });
            return response.data;
        },
    });
}

export function usePublishWorkflow() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (data: { workflowCode: string, definitionJson: string }) => {
            const response = await apiClient.post('/api/admin/workflows/publish', data);
            return response.data;
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['workflow', 'templates'] });
        },
    });
}

export function useWorkflowTemplate(code: string) {
    return useQuery<WorkflowTemplate>({
        queryKey: ['admin', 'workflow', code],
        queryFn: async () => {
            const response = await apiClient.get(`/api/admin/workflows/${code}`);
            return response.data.data;
        },
        enabled: !!code,
    });
}
export interface WorkflowStep {
    workflowStepId: number;
    stepKey: string;
    name: string;
    sequenceNo: number;
    allowManualAssignment: boolean;
}

export function useWorkflowTemplateSteps(id: number | null) {
    return useQuery<WorkflowStep[]>({
        queryKey: ['admin', 'workflow', 'steps', id],
        queryFn: async () => {
            const response = await apiClient.get(`/api/workflow/templates/${id}/steps`);
            return response.data.data;
        },
        enabled: !!id,
    });
}

export interface AssignmentOption {
    workflowStepId: number;
    name: string;
    modeCode: string;
    assignmentMode: string;
    roleName?: string;
    departmentName?: string;
    isManual: boolean;
    eligibleUsers: {
        userId: number;
        displayName: string;
        description: string;
    }[];
}

export function useAssignmentOptions(versionId: number | null) {
    return useQuery<AssignmentOption[]>({
        queryKey: ['admin', 'workflow', 'assignment-options', versionId],
        queryFn: async () => {
            const response = await apiClient.get(`/api/workflow/templates/versions/${versionId}/assignment-options`);
            return response.data.data;
        },
        enabled: !!versionId,
    });
}

export interface ReferenceItem {
    id: number;
    code: string;
    name: string;
    description?: string;
    isActiveOrTerminal: boolean;
}

export function useReferenceData(type: string) {
    return useQuery<PagedResponse<ReferenceItem[]>>({
        queryKey: ['reference', type],
        queryFn: async () => {
            const response = await apiClient.get(`/api/reference/${type}`, {
                params: { pageNumber: 1, pageSize: 100 }
            });
            return response.data;
        },
        enabled: !!type,
        staleTime: 5 * 60 * 1000, // Cache for 5 minutes
    });
}
