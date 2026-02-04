'use client';

import { useWarehouses } from '@/hooks/useInventory';
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
import {
    Warehouse as WarehouseIcon,
    MapPin,
    Plus,
    ExternalLink
} from 'lucide-react';
import { Skeleton } from '@/components/ui/skeleton';

interface Warehouse {
    id: number;
    name: string;
    location?: string;
}

export default function WarehousesPage() {
    const { data: warehouses, isLoading } = useWarehouses();

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight text-foreground">Warehouses</h1>
                    <p className="text-sm text-muted-foreground">Manage storage locations and inventory hubs.</p>
                </div>
                <Button size="sm">
                    <Plus className="mr-2 h-4 w-4" /> Add Warehouse
                </Button>
            </div>

            <div className="rounded-md border bg-card">
                <Table>
                    <TableHeader>
                        <TableRow>
                            <TableHead>Location Name</TableHead>
                            <TableHead>Code</TableHead>
                            <TableHead>Address</TableHead>
                            <TableHead className="text-right">Actions</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {isLoading ? (
                            Array.from({ length: 3 }).map((_, i) => (
                                <TableRow key={i}>
                                    <TableCell><Skeleton className="h-4 w-40" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-12" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-60" /></TableCell>
                                    <TableCell className="text-right"><Skeleton className="h-8 w-8 ml-auto" /></TableCell>
                                </TableRow>
                            ))
                        ) : (warehouses?.data?.length === 0 ? (
                            <TableRow>
                                <TableCell colSpan={4} className="h-32 text-center text-muted-foreground">
                                    No warehouses configured.
                                </TableCell>
                            </TableRow>
                        ) : (
                            (warehouses?.data as Warehouse[] | undefined)?.map((wh) => (
                                <TableRow key={wh.id} className="group hover:bg-muted/30 transition-colors">
                                    <TableCell className="font-semibold text-foreground">
                                        <div className="flex items-center gap-2">
                                            <WarehouseIcon className="h-4 w-4 text-primary" />
                                            {wh.name}
                                        </div>
                                    </TableCell>
                                    <TableCell>
                                        <Badge variant="outline" className="font-mono text-[10px]">
                                            WH-{wh.id}
                                        </Badge>
                                    </TableCell>
                                    <TableCell className="text-sm text-muted-foreground">
                                        <div className="flex items-center gap-1">
                                            <MapPin className="h-3 w-3" />
                                            {wh.location || 'No address specified'}
                                        </div>
                                    </TableCell>
                                    <TableCell className="text-right">
                                        <Button variant="ghost" size="sm">
                                            <ExternalLink className="h-4 w-4" />
                                        </Button>
                                    </TableCell>
                                </TableRow>
                            ))
                        ))}
                    </TableBody>
                </Table>
            </div>
        </div>
    );
}
