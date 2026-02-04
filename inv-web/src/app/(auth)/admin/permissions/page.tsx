'use client';

import { Lock } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { usePermissions } from '@/hooks/useAdmin';
import { useState, useCallback } from 'react';
import { PaginationControls } from '@/components/ui/pagination-controls';
import { Input } from '@/components/ui/input';
import { Search } from 'lucide-react';
import { debounce } from 'lodash';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table';

export default function PermissionsPage() {
    const [page, setPage] = useState(1);
    const [searchTerm, setSearchTerm] = useState('');
    const { data: permissionsData, isLoading } = usePermissions({ pageNumber: page, pageSize: 50, searchTerm });

    const permissions = permissionsData?.data || [];
    const totalPages = permissionsData?.totalPages || 0;
    const totalRecords = permissionsData?.totalRecords || 0;

    const debouncedSearch = useCallback(
        debounce((term: string) => {
            setSearchTerm(term);
            setPage(1);
        }, 500),
        []
    );

    if (isLoading && !permissionsData) {
        return <div className="p-8 text-center text-muted-foreground italic">Loading permissions...</div>;
    }

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight">Permissions</h1>
                    <p className="text-sm text-muted-foreground">View all available system permissions</p>
                </div>
            </div>

            <div className="relative">
                <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                <Input
                    type="search"
                    placeholder="Search permissions..."
                    className="pl-8 max-w-sm"
                    onChange={(e) => debouncedSearch(e.target.value)}
                />
            </div>

            <Card>
                <CardHeader>
                    <CardTitle>System Permissions</CardTitle>
                    <CardDescription>List of all granular permissions available in the system</CardDescription>
                </CardHeader>
                <CardContent>
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead>Code</TableHead>
                                <TableHead>Name</TableHead>
                                <TableHead>Description</TableHead>
                                <TableHead>Status</TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {permissions.map((perm) => (
                                <TableRow key={perm.permissionId}>
                                    <TableCell className="font-mono text-xs">
                                        <Badge variant="outline" className="font-normal">
                                            {perm.code}
                                        </Badge>
                                    </TableCell>
                                    <TableCell className="font-medium">
                                        <div className="flex items-center gap-2">
                                            <Lock className="h-3 w-3 text-muted-foreground" />
                                            {perm.name}
                                        </div>
                                    </TableCell>
                                    <TableCell className="text-sm text-muted-foreground">
                                        {perm.description || '-'}
                                    </TableCell>
                                    <TableCell>
                                        <Badge variant={perm.isActive ? "default" : "secondary"} className="text-xs">
                                            {perm.isActive ? 'Active' : 'Inactive'}
                                        </Badge>
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </CardContent>
            </Card>

            <PaginationControls
                pageNumber={page}
                totalPages={totalPages}
                totalRecords={totalRecords}
                onPageChange={setPage}
                pageSize={50}
            />
        </div>
    );
}
