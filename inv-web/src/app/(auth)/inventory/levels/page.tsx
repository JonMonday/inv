'use client';

import Link from 'next/link';
import { useStockLevels, useWarehouses } from '@/hooks/useInventory';
import { Button } from '@/components/ui/button';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow
} from '@/components/ui/table';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuLabel,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Box, Search, Warehouse as WarehouseIcon, Plus, ChevronDown, FolderPlus, LayoutGrid } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useState } from 'react';

interface StockLevel {
    stockLevelId: number;
    warehouseId: number;
    warehouseName: string;
    productId: number;
    productName: string;
    productSku: string;
    onHandQty: number;
    reservedQty: number;
    availableQty: number;
    productReorderLevel: number;
    updatedAt: string;
}

interface Warehouse {
    id: number;
    name: string;
}

export default function StockLevelsPage() {
    const [warehouseId, setWarehouseId] = useState<number | undefined>(undefined);
    const [searchTerm, setSearchTerm] = useState('');

    const { data: stockLevels, isLoading } = useStockLevels(warehouseId);
    const { data: warehousesData } = useWarehouses();
    const warehouses = (warehousesData?.data as Warehouse[]) || [];

    const filteredStock = stockLevels?.filter((item: StockLevel) =>
        item.productName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        item.productSku.toLowerCase().includes(searchTerm.toLowerCase())
    ) || [];

    return (
        <div className="space-y-6">
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight">Stock Levels</h1>
                    <p className="text-sm text-muted-foreground">Real-time inventory levels across all warehouses</p>
                </div>
                <div className="flex items-center gap-3">
                    <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                            <Button variant="outline" className="gap-2">
                                <Plus className="h-4 w-4" /> Add New <ChevronDown className="h-4 w-4" />
                            </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end" className="w-48">
                            <DropdownMenuLabel>Catalog Management</DropdownMenuLabel>
                            <DropdownMenuSeparator />
                            <DropdownMenuItem asChild>
                                <Link href="/reference/categories/add" className="flex items-center gap-2 cursor-pointer">
                                    <FolderPlus className="h-4 w-4 text-muted-foreground" /> Add Category
                                </Link>
                            </DropdownMenuItem>
                            <DropdownMenuItem asChild>
                                <Link href="/reference/products/add" className="flex items-center gap-2 cursor-pointer">
                                    <Box className="h-4 w-4 text-muted-foreground" /> Add Product
                                </Link>
                            </DropdownMenuItem>
                            <DropdownMenuItem asChild>
                                <Link href="/reference/warehouses/add" className="flex items-center gap-2 cursor-pointer">
                                    <LayoutGrid className="h-4 w-4 text-muted-foreground" /> Add Warehouse
                                </Link>
                            </DropdownMenuItem>
                        </DropdownMenuContent>
                    </DropdownMenu>

                    <Link href="/inventory/levels/movement">
                        <Button className="gap-2">
                            <Plus className="h-4 w-4" /> New Manual Movement
                        </Button>
                    </Link>
                </div>
            </div>

            <Card>
                <CardHeader className="pb-3">
                    <CardTitle className="text-sm font-medium">Filters</CardTitle>
                </CardHeader>
                <CardContent className="flex flex-col md:flex-row gap-4">
                    <div className="flex-1 relative">
                        <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                        <Input
                            placeholder="Search by product name or SKU..."
                            className="pl-9"
                            value={searchTerm}
                            onChange={(e) => setSearchTerm(e.target.value)}
                        />
                    </div>
                    <div className="w-full md:w-64">
                        <Select
                            onValueChange={(val) => setWarehouseId(val === 'all' ? undefined : Number(val))}
                            defaultValue="all"
                        >
                            <SelectTrigger>
                                <div className="flex items-center gap-2">
                                    <WarehouseIcon className="h-4 w-4 text-muted-foreground" />
                                    <SelectValue placeholder="All Warehouses" />
                                </div>
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value="all">All Warehouses</SelectItem>
                                {warehouses.filter(w => w.id).map(w => (
                                    <SelectItem key={w.id} value={w.id.toString()}>{w.name}</SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                    </div>
                </CardContent>
            </Card>

            <div className="rounded-md border bg-card">
                <Table>
                    <TableHeader>
                        <TableRow>
                            <TableHead>Product</TableHead>
                            <TableHead>Warehouse</TableHead>
                            <TableHead className="text-right">In Stock</TableHead>
                            <TableHead className="text-right">Reserved</TableHead>
                            <TableHead className="text-right font-bold">Available</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {isLoading ? (
                            Array.from({ length: 5 }).map((_, i) => (
                                <TableRow key={i}>
                                    <TableCell><Skeleton className="h-4 w-48" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-16 ml-auto" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-16 ml-auto" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-16 ml-auto" /></TableCell>
                                </TableRow>
                            ))
                        ) : filteredStock.length === 0 ? (
                            <TableRow>
                                <TableCell colSpan={5} className="h-32 text-center text-muted-foreground">
                                    No stock records found matching your criteria.
                                </TableCell>
                            </TableRow>
                        ) : (
                            filteredStock.map((item: StockLevel) => (
                                <TableRow key={item.stockLevelId} className="hover:bg-muted/50 transition-colors">
                                    <TableCell>
                                        <div className="flex items-center gap-3">
                                            <div className="h-8 w-8 rounded bg-primary/10 flex items-center justify-center">
                                                <Box className="h-4 w-4 text-primary" />
                                            </div>
                                            <div className="flex flex-col">
                                                <span className="font-medium">{item.productName}</span>
                                                <span className="text-[10px] text-muted-foreground font-mono">{item.productSku}</span>
                                            </div>
                                        </div>
                                    </TableCell>
                                    <TableCell>
                                        <div className="flex items-center gap-2">
                                            <span className="text-sm">{item.warehouseName}</span>
                                        </div>
                                    </TableCell>
                                    <TableCell className="text-right font-mono">{item.onHandQty.toLocaleString()}</TableCell>
                                    <TableCell className="text-right text-muted-foreground font-mono">{item.reservedQty.toLocaleString()}</TableCell>
                                    <TableCell className="text-right font-bold font-mono">
                                        <Badge
                                            variant={item.availableQty <= item.productReorderLevel ? 'destructive' : 'secondary'}
                                            className="font-mono"
                                        >
                                            {item.availableQty.toLocaleString()}
                                            {item.availableQty <= item.productReorderLevel && (
                                                <span className="ml-1 text-[8px] uppercase tracking-tighter opacity-80">Low</span>
                                            )}
                                        </Badge>
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
