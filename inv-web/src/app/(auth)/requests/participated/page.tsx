'use client';

import { useState } from 'react';
import { useRequests } from '@/hooks/useRequests';
import { PaginationControls } from '@/components/ui/pagination-controls';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow
} from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { History, Search, Filter } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Skeleton } from '@/components/ui/skeleton';
import { Button } from '@/components/ui/button';

export default function ParticipatedRequestsPage() {
    const [page, setPage] = useState(1);
    const { data, isLoading } = useRequests({ pageNumber: page, pageSize: 20 });

    // In a real scenario, we'd have a specific filter for "Participated"
    const requests = data?.data || [];
    const totalPages = data?.totalPages || 0;
    const totalRecords = data?.totalRecords || 0;

    return (
        <div className="space-y-6">
            <div className="flex flex-col gap-2">
                <div className="flex items-center gap-2">
                    <History className="h-6 w-6 text-primary" />
                    <h1 className="text-2xl font-black tracking-tight">Participated Requests</h1>
                </div>
                <p className="text-sm text-muted-foreground">Requests where you have been an assignee, reviewer, or collaborator.</p>
            </div>

            <div className="flex items-center justify-between gap-4">
                <div className="relative flex-1 max-w-md">
                    <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                    <Input
                        placeholder="Search your history..."
                        className="pl-9 h-9"
                    />
                </div>
                <Button variant="outline" size="sm" className="h-9">
                    <Filter className="mr-2 h-4 w-4" /> Filter
                </Button>
            </div>

            <div className="rounded-xl border bg-card overflow-hidden">
                <Table>
                    <TableHeader className="bg-muted/50">
                        <TableRow>
                            <TableHead className="w-[100px] text-xs font-black uppercase tracking-widest">ID</TableHead>
                            <TableHead className="text-xs font-black uppercase tracking-widest">Warehouse</TableHead>
                            <TableHead className="text-xs font-black uppercase tracking-widest">Status</TableHead>
                            <TableHead className="text-xs font-black uppercase tracking-widest">Last Activity</TableHead>
                            <TableHead className="text-right text-xs font-black uppercase tracking-widest">Action</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {isLoading ? (
                            Array.from({ length: 5 }).map((_, i) => (
                                <TableRow key={i}>
                                    <TableCell><Skeleton className="h-4 w-12" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-20" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                                    <TableCell className="text-right"><Skeleton className="h-8 w-16 ml-auto" /></TableCell>
                                </TableRow>
                            ))
                        ) : requests.length === 0 ? (
                            <TableRow>
                                <TableCell colSpan={5} className="h-32 text-center text-muted-foreground">
                                    <div className="flex flex-col items-center gap-2 opacity-30">
                                        <History className="h-8 w-8" />
                                        <p className="text-xs font-bold uppercase tracking-widest">No participation history found</p>
                                    </div>
                                </TableCell>
                            </TableRow>
                        ) : (
                            requests.map((req: any) => (
                                <TableRow key={req.requestId} className="group">
                                    <TableCell className="font-mono text-xs font-bold">REQ-{req.requestId}</TableCell>
                                    <TableCell className="text-sm font-medium">{req.warehouseName}</TableCell>
                                    <TableCell>
                                        <Badge variant="secondary" className="text-[10px] font-bold uppercase">
                                            {req.statusLabel}
                                        </Badge>
                                    </TableCell>
                                    <TableCell className="text-xs text-muted-foreground">
                                        {new Date(req.requestedAt).toLocaleDateString()}
                                    </TableCell>
                                    <TableCell className="text-right">
                                        <Button variant="ghost" size="sm" className="text-xs font-bold h-7">View</Button>
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
