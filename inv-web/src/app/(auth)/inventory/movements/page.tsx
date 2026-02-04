'use client';

import { useMovements } from '@/hooks/useInventory';
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
    ArrowRightLeft,
    User,
    Calendar,
    Warehouse as WarehouseIcon,
    Tag
} from 'lucide-react';
import { Skeleton } from '@/components/ui/skeleton';
import { useState, useCallback } from 'react';
import { PaginationControls } from '@/components/ui/pagination-controls';
import { debounce } from 'lodash';

interface StockMovement {
    stockMovementId: number;
    requestNo?: string;
    movementTypeCode: string;
    performedBy: string;
    createdAt: string;
    referenceNo?: string;
    warehouseId: number;
    linesCount: number;
}

export default function StockMovementsPage() {
    const [page, setPage] = useState(1);
    const [searchTerm, setSearchTerm] = useState('');
    const { data: movementsData, isLoading } = useMovements({ pageNumber: page, pageSize: 20, searchTerm });

    const movements = movementsData?.data || [];
    const totalPages = movementsData?.totalPages || 0;
    const totalRecords = movementsData?.totalRecords || 0;

    const debouncedSearch = useCallback(
        debounce((term: string) => {
            setSearchTerm(term);
            setPage(1);
        }, 500),
        []
    );

    const getMovementBadge = (type: string) => {
        const types: Record<string, { color: string, icon: React.ReactNode }> = {
            'RESERVE': { color: 'bg-blue-500/10 text-blue-500 border-blue-500/20', icon: <Tag className="h-3 w-3" /> },
            'ISSUE': { color: 'bg-emerald-500/10 text-emerald-500 border-emerald-500/20', icon: <ArrowRightLeft className="h-3 w-3" /> },
            'CONSUME_RESERVE': { color: 'bg-purple-500/10 text-purple-500 border-purple-500/20', icon: <ArrowRightLeft className="h-3 w-3" /> },
            'RELEASE': { color: 'bg-orange-500/10 text-orange-500 border-orange-500/20', icon: <ArrowRightLeft className="h-3 w-3" /> },
            'ADJUST': { color: 'bg-gray-500/10 text-gray-500 border-gray-500/20', icon: <Tag className="h-3 w-3" /> },
        };
        const config = types[type] || { color: 'bg-slate-500/10 text-slate-500 border-slate-500/20', icon: <Tag className="h-3 w-3" /> };
        return (
            <Badge variant="outline" className={`gap-1.5 px-2 py-0.5 text-[10px] font-bold uppercase ${config.color}`}>
                {config.icon} {type}
            </Badge>
        );
    };

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight text-foreground">Stock Movements Log</h1>
                    <p className="text-sm text-muted-foreground">Historical audit trail of all inventory transactions.</p>
                </div>
                <Button variant="outline" size="sm">
                    <Calendar className="mr-2 h-4 w-4" /> Export CSV
                </Button>
            </div>

            <div className="flex items-center gap-4">
                <div className="relative flex-1 max-w-sm">
                    <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                    <Input
                        placeholder="Search by Request, Type, or User..."
                        className="pl-8"
                        onChange={(e) => debouncedSearch(e.target.value)}
                    />
                </div>
            </div>

            <div className="rounded-md border bg-card overflow-hidden">
                <Table>
                    <TableHeader className="bg-muted/50">
                        <TableRow>
                            <TableHead className="w-[120px]">Date</TableHead>
                            <TableHead>Type</TableHead>
                            <TableHead>Reference</TableHead>
                            <TableHead>Warehouse</TableHead>
                            <TableHead>Performed By</TableHead>
                            <TableHead className="text-right">Lines</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {isLoading ? (
                            Array.from({ length: 10 }).map((_, i) => (
                                <TableRow key={i}>
                                    <TableCell><Skeleton className="h-4 w-20" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-20" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-12" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                                    <TableCell className="text-right"><Skeleton className="h-4 w-8 ml-auto" /></TableCell>
                                </TableRow>
                            ))
                        ) : movements.length === 0 ? (
                            <TableRow>
                                <TableCell colSpan={6} className="h-32 text-center text-muted-foreground">
                                    No movements found.
                                </TableCell>
                            </TableRow>
                        ) : (
                            (movements as StockMovement[]).map((m) => (
                                <TableRow key={m.stockMovementId} className="group hover:bg-muted/30 transition-colors">
                                    <TableCell className="text-xs text-muted-foreground font-medium">
                                        {new Date(m.createdAt).toLocaleDateString()}
                                        <span className="block text-[10px] opacity-70">
                                            {new Date(m.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                                        </span>
                                    </TableCell>
                                    <TableCell>
                                        {getMovementBadge(m.movementTypeCode)}
                                    </TableCell>
                                    <TableCell>
                                        <div className="flex flex-col">
                                            <span className="text-sm font-semibold text-foreground">{m.requestNo || 'Manual'}</span>
                                            {m.referenceNo && <span className="text-[10px] text-muted-foreground">Ref: {m.referenceNo}</span>}
                                        </div>
                                    </TableCell>
                                    <TableCell>
                                        <div className="flex items-center gap-2 text-xs">
                                            <WarehouseIcon className="h-3 w-3 text-muted-foreground" />
                                            <span className="font-semibold">{m.warehouseId}</span>
                                        </div>
                                    </TableCell>
                                    <TableCell>
                                        <div className="flex items-center gap-2 text-sm">
                                            <User className="h-3 w-3 text-muted-foreground" />
                                            <span>{m.performedBy}</span>
                                        </div>
                                    </TableCell>
                                    <TableCell className="text-right text-xs font-mono font-bold">
                                        {m.linesCount}
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
