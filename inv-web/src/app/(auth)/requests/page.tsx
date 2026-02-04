'use client';

import { useState, useCallback } from 'react';
import Link from 'next/link';
import { useRequests } from '@/hooks/useRequests';
import { PaginationControls } from '@/components/ui/pagination-controls';
import { debounce } from 'lodash';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow
} from '@/components/ui/table';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Plus, Search, Filter, MoreHorizontal } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Skeleton } from '@/components/ui/skeleton';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger
} from '@/components/ui/dropdown-menu';

interface InventoryRequest {
    requestId: number;
    warehouseId: number;
    warehouseName: string;
    departmentId: number;
    departmentName: string;
    statusCode: string;
    statusLabel: string;
    requestedAt: string;
    currentAssignee?: string;
}

const statusMap: Record<string, { label: string, variant: "default" | "secondary" | "destructive" | "outline" }> = {
    'DRAFT': { label: 'Draft', variant: 'outline' },
    'IN_WORKFLOW': { label: 'In Workflow', variant: 'secondary' },
    'FULFILLMENT': { label: 'Fulfillment', variant: 'default' },
    'COMPLETED': { label: 'Completed', variant: 'default' },
    'CANCELLED': { label: 'Cancelled', variant: 'destructive' },
};

export default function RequestsPage() {
    const [page, setPage] = useState(1);
    const [searchTerm, setSearchTerm] = useState('');
    const { data, isLoading } = useRequests({ pageNumber: page, pageSize: 20, searchTerm });

    const requests = data?.data || [];
    const totalPages = data?.totalPages || 0;
    const totalRecords = data?.totalRecords || 0;

    const debouncedSearch = useCallback(
        debounce((term: string) => {
            setSearchTerm(term);
            setPage(1);
        }, 500),
        []
    );

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div className="flex items-center gap-4 flex-1 max-md:max-w-full max-w-md">
                    <div className="relative flex-1">
                        <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                        <Input
                            placeholder="Search requests..."
                            className="pl-9 h-9"
                            onChange={(e) => debouncedSearch(e.target.value)}
                        />
                    </div>
                    <Button variant="outline" size="sm" className="h-9">
                        <Filter className="mr-2 h-4 w-4" /> Filter
                    </Button>
                </div>
                <Link href="/requests/new">
                    <Button size="sm" className="h-9">
                        <Plus className="mr-2 h-4 w-4" /> New Request
                    </Button>
                </Link>
            </div>

            <div className="rounded-md border bg-card">
                <Table>
                    <TableHeader>
                        <TableRow className="hover:bg-transparent">
                            <TableHead className="w-[100px]">ID</TableHead>
                            <TableHead>Warehouse</TableHead>
                            <TableHead>Department</TableHead>
                            <TableHead>Assignee</TableHead>
                            <TableHead>Status</TableHead>
                            <TableHead>Created At</TableHead>
                            <TableHead className="text-right">Actions</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {isLoading ? (
                            Array.from({ length: 5 }).map((_, i) => (
                                <TableRow key={i}>
                                    <TableCell><Skeleton className="h-4 w-12" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-20" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                                    <TableCell className="text-right"><Skeleton className="h-8 w-8 ml-auto" /></TableCell>
                                </TableRow>
                            ))
                        ) : requests.length === 0 ? (
                            <TableRow key="empty">
                                <TableCell colSpan={7} className="h-32 text-center text-muted-foreground">
                                    No requests found
                                </TableCell>
                            </TableRow>
                        ) : (
                            (requests as InventoryRequest[]).map((req) => (
                                <TableRow key={req.requestId} className="group cursor-pointer" onClick={() => (window.location.href = `/requests/${req.requestId}`)}>
                                    <TableCell className="font-mono text-xs font-bold">REQ-{req.requestId}</TableCell>
                                    <TableCell className="text-sm">{req.warehouseName}</TableCell>
                                    <TableCell className="text-sm">{req.departmentName}</TableCell>
                                    <TableCell className="text-sm">
                                        {req.currentAssignee || (
                                            <span className="text-muted-foreground italic text-xs">Unassigned</span>
                                        )}
                                    </TableCell>
                                    <TableCell>
                                        <Badge variant={statusMap[req.statusCode]?.variant || 'outline'} className="text-[10px] font-bold uppercase">
                                            {req.statusLabel || req.statusCode}
                                        </Badge>
                                    </TableCell>
                                    <TableCell className="text-xs text-muted-foreground">
                                        {new Date(req.requestedAt).toLocaleDateString()}
                                    </TableCell>
                                    <TableCell className="text-right">
                                        <DropdownMenu>
                                            <DropdownMenuTrigger asChild onClick={(e) => e.stopPropagation()}>
                                                <Button variant="ghost" size="icon" className="h-8 w-8">
                                                    <MoreHorizontal className="h-4 w-4" />
                                                </Button>
                                            </DropdownMenuTrigger>
                                            <DropdownMenuContent align="end">
                                                <DropdownMenuItem asChild>
                                                    <Link href={`/requests/${req.requestId}`}>View Details</Link>
                                                </DropdownMenuItem>
                                                {req.statusCode === 'DRAFT' && (
                                                    <DropdownMenuItem>Edit Request</DropdownMenuItem>
                                                )}
                                            </DropdownMenuContent>
                                        </DropdownMenu>
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
                pageSize={20}
            />
        </div>
    );
}
