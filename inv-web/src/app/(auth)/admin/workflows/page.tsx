'use client';

import { useState, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '@/lib/api/client';
import { PagedResponse } from '@/lib/api/types';
import { PaginationControls } from '@/components/ui/pagination-controls';
import { Input } from '@/components/ui/input';
import { Search, Share2, Plus, Settings2, History, CheckCircle2, Clock, Eye, Copy, MoreHorizontal } from 'lucide-react';
import { debounce } from 'lodash';
import { useWorkflowTemplate } from '@/hooks/useAdmin';
import { WorkflowFlowView } from '@/components/workflow/WorkflowFlowView';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow
} from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import Link from 'next/link';
import {
    Dialog,
    DialogContent,
    DialogHeader,
    DialogTitle,
    DialogFooter,
    DialogDescription
} from '@/components/ui/dialog';
import { useToast } from '@/hooks/use-toast';
import { Label } from '@/components/ui/label';

interface WorkflowTemplate {
    workflowTemplateId: number;
    name: string;
    code: string;
    isActive: boolean;
    createdAt: string;
    status: string;
    publishedAt?: string;
    sourceTemplateId?: number;
}

function useCreateWorkflowTemplate() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (data: { name: string, code: string }) => {
            const response = await apiClient.post('/api/workflow/templates', data);
            return response.data;
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['workflow', 'templates'] });
        },
    });
}

function useCloneWorkflowTemplate() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async ({ id, data }: { id: number, data: { newCode: string, newName: string } }) => {
            const response = await apiClient.post(`/api/workflow/templates/${id}/clone`, data);
            return response.data;
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['workflow', 'templates'] });
        },
    });
}

export default function WorkflowTemplatesPage() {
    const [page, setPage] = useState(1);
    const [searchTerm, setSearchTerm] = useState('');
    const { toast } = useToast();

    const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
    const [newTemplate, setNewTemplate] = useState({ name: '', code: '' });

    const [isActionModalOpen, setIsActionModalOpen] = useState(false);
    const [selectedTemplate, setSelectedTemplate] = useState<WorkflowTemplate | null>(null);

    const [isCloneDialogOpen, setIsCloneDialogOpen] = useState(false);
    const [isFlowModalOpen, setIsFlowModalOpen] = useState(false);
    const [cloneData, setCloneData] = useState({ newName: '', newCode: '' });

    const createMutation = useCreateWorkflowTemplate();
    const cloneMutation = useCloneWorkflowTemplate();

    const { data: pagedData, isLoading } = useQuery<PagedResponse<WorkflowTemplate[]>>({
        queryKey: ['workflow', 'templates', { pageNumber: page, pageSize: 10, searchTerm }],
        queryFn: async () => {
            const response = await apiClient.get('/api/workflow/templates', {
                params: { pageNumber: page, pageSize: 10, searchTerm }
            });
            return response.data;
        },
    });

    const templates = pagedData?.data || [];
    const totalPages = pagedData?.totalPages || 0;
    const totalRecords = pagedData?.totalRecords || 0;

    const debouncedSearch = useCallback(
        debounce((term: string) => {
            setSearchTerm(term);
            setPage(1);
        }, 500),
        []
    );

    const handleCreateTemplate = async () => {
        if (!newTemplate.name || !newTemplate.code) return;
        try {
            await createMutation.mutateAsync(newTemplate);
            toast({ title: 'Success', description: 'Template created successfully.' });
            setIsCreateDialogOpen(false);
            setNewTemplate({ name: '', code: '' });
        } catch (error: unknown) {
            const err = error as { response?: { data?: { message?: string } } };
            toast({
                title: 'Error',
                description: err.response?.data?.message || (error as Error).message || 'Failed to create template',
                variant: 'destructive'
            });
        }
    };

    const handleCloneTemplate = async () => {
        if (!selectedTemplate || !cloneData.newName || !cloneData.newCode) return;
        try {
            await cloneMutation.mutateAsync({
                id: selectedTemplate.workflowTemplateId,
                data: cloneData
            });
            toast({ title: 'Success', description: 'Template cloned successfully.' });
            setIsCloneDialogOpen(false);
            setCloneData({ newName: '', newCode: '' });
            setSelectedTemplate(null);
        } catch (error: unknown) {
            const err = error as { response?: { data?: { message?: string } } };
            toast({
                title: 'Error',
                description: err.response?.data?.message || (error as Error).message || 'Failed to clone template',
                variant: 'destructive'
            });
        }
    };

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight text-foreground">Workflow Templates</h1>
                    <p className="text-sm text-muted-foreground">Define and manage business logic for approvals and fulfillment.</p>
                </div>
                <Button size="sm" onClick={() => setIsCreateDialogOpen(true)}>
                    <Plus className="mr-2 h-4 w-4" /> New Template
                </Button>
            </div>

            <div className="relative">
                <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                <Input
                    type="search"
                    placeholder="Search templates..."
                    className="pl-8 max-w-sm"
                    onChange={(e) => debouncedSearch(e.target.value)}
                />
            </div>

            <div className="rounded-md border bg-card overflow-hidden">
                <Table>
                    <TableHeader className="bg-muted/50">
                        <TableRow>
                            <TableHead>Template Name</TableHead>
                            <TableHead>Code</TableHead>
                            <TableHead>Status</TableHead>
                            <TableHead>Created At</TableHead>
                            <TableHead className="text-right">Actions</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {isLoading ? (
                            Array.from({ length: 3 }).map((_, i) => (
                                <TableRow key={i}>
                                    <TableCell><Skeleton className="h-4 w-48" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-12" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-16" /></TableCell>
                                    <TableCell className="text-right"><Skeleton className="h-8 w-24 ml-auto" /></TableCell>
                                </TableRow>
                            ))
                        ) : templates.length === 0 ? (
                            <TableRow>
                                <TableCell colSpan={5} className="h-48 text-center text-muted-foreground">
                                    <div className="flex flex-col items-center gap-2">
                                        <Share2 className="h-8 w-8 opacity-20" />
                                        <p>No workflow templates found.</p>
                                    </div>
                                </TableCell>
                            </TableRow>
                        ) : (
                            templates.map((row) => (
                                <TableRow key={row.workflowTemplateId} className="group hover:bg-muted/30 transition-colors">
                                    <TableCell className="font-semibold text-foreground">
                                        <div className="flex items-center gap-2">
                                            <div className="h-8 w-8 rounded-lg bg-primary/5 flex items-center justify-center">
                                                <Share2 className="h-4 w-4 text-primary" />
                                            </div>
                                            {row.name}
                                        </div>
                                    </TableCell>
                                    <TableCell>
                                        <Badge variant="outline" className="font-mono text-[10px] tracking-wider px-2 py-0.5">
                                            {row.code}
                                        </Badge>
                                    </TableCell>
                                    <TableCell>
                                        <Badge variant={row.isActive ? "default" : "secondary"} className="text-[10px] gap-1 px-2 py-0.5">
                                            {row.isActive ? (
                                                <CheckCircle2 className="h-3 w-3" />
                                            ) : (
                                                <Clock className="h-3 w-3" />
                                            )}
                                            {row.status}
                                        </Badge>
                                    </TableCell>
                                    <TableCell className="text-xs text-muted-foreground">
                                        {new Date(row.createdAt).toLocaleDateString()}
                                    </TableCell>
                                    <TableCell className="text-right">
                                        <Button
                                            variant="outline"
                                            size="sm"
                                            onClick={() => {
                                                setSelectedTemplate(row);
                                                setIsActionModalOpen(true);
                                            }}
                                        >
                                            Actions
                                        </Button>
                                    </TableCell>
                                </TableRow>
                            ))
                        )}
                    </TableBody>
                </Table>
            </div>

            <PaginationControls
                pageNumber={page}
                totalPages={totalPages}
                totalRecords={totalRecords}
                onPageChange={setPage}
            />

            <Dialog open={isActionModalOpen} onOpenChange={setIsActionModalOpen}>
                <DialogContent className="sm:max-w-[425px]">
                    <DialogHeader>
                        <DialogTitle>Template Actions</DialogTitle>
                        <DialogDescription>
                            Manage the logic for {selectedTemplate?.name}
                        </DialogDescription>
                    </DialogHeader>
                    <div className="grid gap-3 py-4">
                        <Button
                            variant="outline"
                            className="w-full justify-start h-12"
                            onClick={() => {
                                setIsActionModalOpen(false);
                                setIsFlowModalOpen(true);
                            }}
                        >
                            <Share2 className="mr-3 h-5 w-5 text-primary" />
                            <div className="flex flex-col items-start">
                                <span className="font-medium">View Flow Diagram</span>
                                <span className="text-[11px] text-muted-foreground font-normal">Visual overview of transitions</span>
                            </div>
                        </Button>
                        <Button variant="outline" className="w-full justify-start h-12" asChild onClick={() => setIsActionModalOpen(false)}>
                            <Link href={`/admin/workflows/${selectedTemplate?.workflowTemplateId}/builder`}>
                                <Eye className="mr-3 h-5 w-5 text-primary" />
                                <div className="flex flex-col items-start">
                                    <span className="font-medium">View Template</span>
                                    <span className="text-[11px] text-muted-foreground font-normal">Open in workflow builder</span>
                                </div>
                            </Link>
                        </Button>
                        <Button
                            variant="outline"
                            className="w-full justify-start h-12"
                            onClick={() => {
                                setIsActionModalOpen(false);
                                setCloneData({
                                    newName: `${selectedTemplate?.name} (Clone)`,
                                    newCode: `${selectedTemplate?.code}_CLONE`
                                });
                                setIsCloneDialogOpen(true);
                            }}
                        >
                            <Copy className="mr-3 h-5 w-5 text-primary" />
                            <div className="flex flex-col items-start">
                                <span className="font-medium">Clone Template</span>
                                <span className="text-[11px] text-muted-foreground font-normal">Create a draft from this template</span>
                            </div>
                        </Button>
                    </div>
                    <DialogFooter>
                        <Button variant="ghost" onClick={() => setIsActionModalOpen(false)}>Close</Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            <FlowOverviewModal
                open={isFlowModalOpen}
                onOpenChange={setIsFlowModalOpen}
                templateId={selectedTemplate?.workflowTemplateId || null}
            />

            <Dialog open={isCloneDialogOpen} onOpenChange={setIsCloneDialogOpen}>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Clone Workflow Template</DialogTitle>
                        <DialogDescription>
                            Enter details for the new cloned template.
                        </DialogDescription>
                    </DialogHeader>
                    <div className="space-y-4 py-4">
                        <div className="space-y-2">
                            <Label>New Template Name</Label>
                            <Input
                                placeholder="e.g. Finance Approval Flow - Copy"
                                value={cloneData.newName}
                                onChange={e => setCloneData({ ...cloneData, newName: e.target.value })}
                            />
                        </div>
                        <div className="space-y-2">
                            <Label>New Code (Unique)</Label>
                            <Input
                                placeholder="e.g. FIN_FLOW_V2"
                                value={cloneData.newCode}
                                onChange={e => setCloneData({ ...cloneData, newCode: e.target.value.toUpperCase().replace(/\s/g, '_') })}
                            />
                        </div>
                    </div>
                    <DialogFooter>
                        <Button variant="outline" onClick={() => setIsCloneDialogOpen(false)}>Cancel</Button>
                        <Button onClick={handleCloneTemplate} disabled={cloneMutation.isPending}>
                            {cloneMutation.isPending ? 'Cloning...' : 'Clone Template'}
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            <Dialog open={isCreateDialogOpen} onOpenChange={setIsCreateDialogOpen}>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>New Workflow Template</DialogTitle>
                        <DialogDescription>
                            Create a new workflow definition. You can build the steps after creation.
                        </DialogDescription>
                    </DialogHeader>
                    <div className="space-y-4 py-4">
                        <div className="space-y-2">
                            <Label>Template Name</Label>
                            <Input
                                placeholder="e.g. Finance Approval Flow"
                                value={newTemplate.name}
                                onChange={e => setNewTemplate({ ...newTemplate, name: e.target.value })}
                            />
                        </div>
                        <div className="space-y-2">
                            <Label>Code (Unique)</Label>
                            <Input
                                placeholder="e.g. FIN_FLOW"
                                value={newTemplate.code}
                                onChange={e => setNewTemplate({ ...newTemplate, code: e.target.value.toUpperCase().replace(/\s/g, '_') })}
                            />
                        </div>
                    </div>
                    <DialogFooter>
                        <Button variant="outline" onClick={() => setIsCreateDialogOpen(false)}>Cancel</Button>
                        <Button onClick={handleCreateTemplate} disabled={createMutation.isPending}>
                            {createMutation.isPending ? 'Creating...' : 'Create Template'}
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </div>
    );
}

function FlowOverviewModal({ open, onOpenChange, templateId }: { open: boolean, onOpenChange: (open: boolean) => void, templateId: number | null }) {
    const { data: template, isLoading } = useWorkflowTemplate(templateId);

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="max-w-6xl h-[85vh] flex flex-col p-0 overflow-hidden bg-black border-white/10">
                <DialogHeader className="p-6 border-b border-white/5 bg-[#050505]">
                    <div className="flex items-center gap-4">
                        <div className="h-10 w-10 rounded-xl bg-blue-500/10 flex items-center justify-center">
                            <Share2 className="h-5 w-5 text-blue-400" />
                        </div>
                        <div>
                            <DialogTitle className="text-xl text-white">Workflow Visualizer</DialogTitle>
                            <DialogDescription className="text-white/40">
                                {template?.name || 'Loading...'} • <span className="font-mono">{template?.code}</span>
                            </DialogDescription>
                        </div>
                    </div>
                </DialogHeader>

                <div className="flex-1 bg-black relative">
                    {isLoading ? (
                        <div className="absolute inset-0 flex flex-col items-center justify-center gap-4 text-white/20">
                            <Skeleton className="h-[400px] w-full max-w-4xl bg-white/5 rounded-2xl" />
                            <p className="animate-pulse">Fetching workflow definition...</p>
                        </div>
                    ) : template ? (
                        <WorkflowFlowView template={template} />
                    ) : (
                        <div className="absolute inset-0 flex items-center justify-center text-white/20">
                            Failed to load template data.
                        </div>
                    )}
                </div>

                <DialogFooter className="p-4 bg-[#050505] border-t border-white/5 justify-between sm:justify-between items-center">
                    <div className="flex items-center gap-6 text-[10px] uppercase tracking-widest text-white/30 font-bold ml-2">
                        <span className="flex items-center gap-2"><div className="h-1.5 w-1.5 rounded-full bg-emerald-500 shadow-sm shadow-emerald-500/50" /> Start Node</span>
                        <span className="flex items-center gap-2"><div className="h-1.5 w-1.5 rounded-full bg-blue-500 shadow-sm shadow-blue-500/50" /> Review Step</span>
                        <span className="flex items-center gap-2"><div className="h-1.5 w-1.5 rounded-full bg-rose-500 shadow-sm shadow-rose-500/50" /> Terminal</span>
                    </div>
                    <Button variant="ghost" className="text-white hover:bg-white/5" onClick={() => onOpenChange(false)}>
                        Dismiss
                    </Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}
