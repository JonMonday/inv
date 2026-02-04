'use client';

import { useState, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '@/lib/api/client';
import { PagedResponse } from '@/lib/api/types';
import { PaginationControls } from '@/components/ui/pagination-controls';
import { Input } from '@/components/ui/input';
import { Search, Share2, Plus, Settings2, History, CheckCircle2, Clock } from 'lucide-react';
import { debounce } from 'lodash';
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
    id: number;
    name: string;
    code: string;
    isActive: boolean;
    createdAt: string;
    latestVersion?: number;
    versions: number[];
}

function useCreateWorkflowTemplate() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async (data: { name: string, code: string }) => {
            const response = await apiClient.post('/api/admin/workflows', data);
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

    const createMutation = useCreateWorkflowTemplate();

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
                            <TableHead>Version</TableHead>
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
                                    <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                                    <TableCell className="text-right"><Skeleton className="h-8 w-24 ml-auto" /></TableCell>
                                </TableRow>
                            ))
                        ) : templates.length === 0 ? (
                            <TableRow>
                                <TableCell colSpan={6} className="h-48 text-center text-muted-foreground">
                                    <div className="flex flex-col items-center gap-2">
                                        <Share2 className="h-8 w-8 opacity-20" />
                                        <p>No workflow templates found.</p>
                                    </div>
                                </TableCell>
                            </TableRow>
                        ) : (
                            templates.flatMap((tpl): (WorkflowTemplate & { version: number | null })[] => {
                                // If no versions, show one row saying "No version"
                                if (!tpl.versions || tpl.versions.length === 0) {
                                    return [{ ...tpl, version: null }];
                                }
                                // Map each version to a row
                                return tpl.versions.map(v => ({ ...tpl, version: v }));
                            }).map((row, idx) => (
                                <TableRow key={`${row.id}-${row.version || 'none'}-${idx}`} className="group hover:bg-muted/30 transition-colors">
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
                                        {row.version ? (
                                            <Badge variant="secondary" className="font-mono text-[10px] px-1.5 py-0">v{row.version}</Badge>
                                        ) : (
                                            <span className="text-[10px] text-muted-foreground italic">No version</span>
                                        )}
                                    </TableCell>
                                    <TableCell>
                                        <Badge variant={row.isActive ? "default" : "secondary"} className="text-[10px] gap-1 px-2 py-0.5">
                                            {row.isActive ? (
                                                <CheckCircle2 className="h-3 w-3" />
                                            ) : (
                                                <Clock className="h-3 w-3" />
                                            )}
                                            {row.isActive ? 'Active' : 'Inactive'}
                                        </Badge>
                                    </TableCell>
                                    <TableCell className="text-xs text-muted-foreground">
                                        {new Date(row.createdAt).toLocaleDateString()}
                                    </TableCell>
                                    <TableCell className="text-right">
                                        <div className="flex justify-end gap-2">
                                            {row.version && (
                                                <Button variant="outline" size="sm" asChild>
                                                    <Link href={`/admin/workflows/${row.code}/builder?version=${row.version}`}>
                                                        <Settings2 className="mr-2 h-4 w-4" /> Builder
                                                    </Link>
                                                </Button>
                                            )}
                                            <Button variant="ghost" size="icon" className="h-8 w-8">
                                                <History className="h-4 w-4" />
                                            </Button>
                                        </div>
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
