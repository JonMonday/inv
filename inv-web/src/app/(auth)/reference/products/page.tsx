'use client';

import { useQuery } from '@tanstack/react-query';
import apiClient from '@/lib/api/client';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow
} from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Package, Search } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { useState, useCallback } from 'react';
import { debounce } from 'lodash';
import { PaginationControls } from '@/components/ui/pagination-controls';
import { PagedResponse } from '@/lib/api/types';

interface Product {
    id: number;
    name: string;
}

export default function ProductsPage() {
    const [page, setPage] = useState(1);
    const [searchTerm, setSearchTerm] = useState('');

    const { data: pagedData, isLoading } = useQuery<PagedResponse<Product[]>>({
        queryKey: ['reference', 'products', { page: page, search: searchTerm }],
        queryFn: async () => {
            const response = await apiClient.get('/api/reference/products', {
                params: { pageNumber: page, pageSize: 20, searchTerm }
            });
            return response.data;
        }
    });

    const products = pagedData?.data || [];
    const totalPages = pagedData?.totalPages || 0;
    const totalRecords = pagedData?.totalRecords || 0;

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
                <div>
                    <h1 className="text-xl font-bold">Products</h1>
                    <p className="text-sm text-muted-foreground">Catalog of all available products and assets</p>
                </div>
                <div className="relative w-64">
                    <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                    <Input
                        placeholder="Search catalog..."
                        className="pl-9 h-9"
                        onChange={(e) => debouncedSearch(e.target.value)}
                    />
                </div>
            </div>

            <div className="rounded-md border bg-card">
                <Table>
                    <TableHeader>
                        <TableRow>
                            <TableHead>Product</TableHead>
                            <TableHead>Category</TableHead>
                            <TableHead>Unit</TableHead>
                            <TableHead className="text-right">Unit Cost</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {isLoading ? (
                            Array.from({ length: 10 }).map((_, i) => (
                                <TableRow key={i}>
                                    <TableCell><Skeleton className="h-4 w-48" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-12" /></TableCell>
                                    <TableCell className="text-right"><Skeleton className="h-4 w-16 ml-auto" /></TableCell>
                                </TableRow>
                            ))
                        ) : products.length === 0 ? (
                            <TableRow>
                                <TableCell colSpan={4} className="h-32 text-center text-muted-foreground">
                                    No products found
                                </TableCell>
                            </TableRow>
                        ) : (
                            products.map((product) => (
                                <TableRow key={product.id}>
                                    <TableCell>
                                        <div className="flex items-center gap-3">
                                            <div className="h-8 w-8 rounded bg-muted flex items-center justify-center">
                                                <Package className="h-4 w-4 text-muted-foreground" />
                                            </div>
                                            <div className="flex flex-col">
                                                <span className="text-sm font-medium">{product.name}</span>
                                                <span className="text-[10px] text-muted-foreground font-mono">ID: {product.id}</span>
                                            </div>
                                        </div>
                                    </TableCell>
                                    <TableCell>
                                        <Badge variant="outline" className="text-[10px]">
                                            General
                                        </Badge>
                                    </TableCell>
                                    <TableCell className="text-sm">EA</TableCell>
                                    <TableCell className="text-right font-mono text-xs">
                                        $0.00
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
