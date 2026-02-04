'use client';

import { useRequests } from '@/hooks/useRequests';
import { useAuthStore } from '@/store/authStore';
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
import { Input } from '@/components/ui/input';
import {
    Search,
    Filter,
    Truck,
    ExternalLink,
    Clock
} from 'lucide-react';
import Link from 'next/link';
import { Skeleton } from '@/components/ui/skeleton';
import { useState } from 'react';

interface FulfillmentRequest {
    requestId: number;
    requestNo: string;
    warehouseId: number;
    requestedAt: string;
    requestStatusId: number;
}

export default function FulfillmentQueuePage() {
    const { user } = useAuthStore();
    const { data: requestsRes, isLoading } = useRequests();
    const [search, setSearch] = useState('');

    const requests = (requestsRes?.data as FulfillmentRequest[]) || [];

    const filteredRequests = requests.filter((req) => {
        const matchesSearch = req.requestNo.toLowerCase().includes(search.toLowerCase());
        const isFulfillable = req.requestStatusId !== 1;
        return matchesSearch && isFulfillable;
    });

    if (user && !user.roles.includes('STOREKEEPER') && !user.roles.includes('ADMIN')) {
        return (
            <div className="flex h-[450px] flex-col items-center justify-center rounded-lg border border-dashed text-center">
                <Truck className="h-10 w-10 text-muted-foreground mb-4" />
                <h3 className="text-lg font-semibold">Access Restricted</h3>
                <p className="max-w-xs text-sm text-muted-foreground mt-2">
                    Only Storekeepers and Administrators can access the fulfillment queue.
                </p>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight text-foreground">Fulfillment Queue</h1>
                    <p className="text-sm text-muted-foreground">Manage picking, packing and dispatch operations.</p>
                </div>
                <div className="flex items-center gap-2">
                    <Button variant="outline" size="sm">
                        <Truck className="mr-2 h-4 w-4" /> Shipments
                    </Button>
                </div>
            </div>

            <div className="flex items-center justify-between gap-4">
                <div className="relative flex-1 max-w-sm">
                    <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                    <Input
                        placeholder="Search by Request #..."
                        className="pl-8"
                        value={search}
                        onChange={(e) => setSearch(e.target.value)}
                    />
                </div>
                <div className="flex items-center gap-2">
                    <Button variant="outline" size="sm">
                        <Filter className="mr-2 h-4 w-4" /> Filter
                    </Button>
                </div>
            </div>

            <div className="rounded-md border bg-card">
                <Table>
                    <TableHeader>
                        <TableRow>
                            <TableHead>Request #</TableHead>
                            <TableHead>Type</TableHead>
                            <TableHead>Warehouse</TableHead>
                            <TableHead>Requested At</TableHead>
                            <TableHead>Status</TableHead>
                            <TableHead className="text-right">Action</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {isLoading ? (
                            Array.from({ length: 5 }).map((_, i) => (
                                <TableRow key={i}>
                                    <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-20" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-20" /></TableCell>
                                    <TableCell className="text-right"><Skeleton className="h-8 w-16 ml-auto" /></TableCell>
                                </TableRow>
                            ))
                        ) : filteredRequests.length === 0 ? (
                            <TableRow>
                                <TableCell colSpan={6} className="h-32 text-center text-muted-foreground">
                                    No requests in queue.
                                </TableCell>
                            </TableRow>
                        ) : (
                            filteredRequests.map((req) => (
                                <TableRow key={req.requestId} className="group transition-colors hover:bg-muted/50">
                                    <TableCell className="font-medium text-foreground">
                                        {req.requestNo}
                                    </TableCell>
                                    <TableCell className="text-muted-foreground text-xs uppercase tracking-wider font-semibold">
                                        Standard Request
                                    </TableCell>
                                    <TableCell>
                                        <div className="flex items-center gap-2 text-sm">
                                            <Badge variant="outline" className="text-[10px] font-mono">WH-{req.warehouseId}</Badge>
                                        </div>
                                    </TableCell>
                                    <TableCell className="text-sm text-muted-foreground">
                                        {new Date(req.requestedAt).toLocaleDateString()}
                                    </TableCell>
                                    <TableCell>
                                        <Badge variant="secondary" className="gap-1.5 px-2 py-0.5 text-[10px] uppercase font-bold">
                                            <Clock className="h-3 w-3" /> FULFILLING
                                        </Badge>
                                    </TableCell>
                                    <TableCell className="text-right">
                                        <div className="flex justify-end gap-2">
                                            <Button variant="ghost" size="sm" asChild>
                                                <Link href={`/requests/${req.requestId}`}>
                                                    <ExternalLink className="h-4 w-4" />
                                                </Link>
                                            </Button>
                                            <Button size="sm" asChild>
                                                <Link href={`/requests/${req.requestId}`}>
                                                    Pick & Issue
                                                </Link>
                                            </Button>
                                        </div>
                                    </TableCell>
                                </TableRow>
                            ))
                        )}
                    </TableBody>
                </Table>
            </div>
        </div>
    );
}
