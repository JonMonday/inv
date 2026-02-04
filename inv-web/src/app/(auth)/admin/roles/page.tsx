'use client';


import { ShieldCheck, Plus, MoreVertical, Search } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { useRoles } from '@/hooks/useAdmin';
import { useState, useCallback } from 'react';
import { PaginationControls } from '@/components/ui/pagination-controls';
import { Input } from '@/components/ui/input';
import { debounce } from 'lodash';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow
} from '@/components/ui/table';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';

export default function RolesPage() {
    const [page, setPage] = useState(1);
    const [searchTerm, setSearchTerm] = useState('');
    const { data: rolesData, isLoading } = useRoles({ pageNumber: page, pageSize: 10, searchTerm });

    const roles = rolesData?.data || [];
    const totalPages = rolesData?.totalPages || 0;
    const totalRecords = rolesData?.totalRecords || 0;

    const debouncedSearch = useCallback(
        debounce((term: string) => {
            setSearchTerm(term);
            setPage(1);
        }, 500),
        []
    );

    if (isLoading && !rolesData) {
        return <div className="p-8 text-center text-muted-foreground italic">Loading roles...</div>;
    }

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight">Roles & Permissions</h1>
                    <p className="text-sm text-muted-foreground">Manage system roles and their permissions</p>
                </div>
                <Button size="sm">
                    <Plus className="mr-2 h-4 w-4" /> New Role
                </Button>
            </div>

            <div className="relative">
                <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                <Input
                    type="search"
                    placeholder="Search roles..."
                    className="pl-8 max-w-sm"
                    onChange={(e) => debouncedSearch(e.target.value)}
                />
            </div>

            <Card>
                <CardHeader>
                    <CardTitle>System Roles</CardTitle>
                    <CardDescription>Configure roles and assign permissions</CardDescription>
                </CardHeader>
                <CardContent>
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead>Role Name</TableHead>
                                <TableHead>Description</TableHead>
                                <TableHead>Users</TableHead>
                                <TableHead className="text-right">Actions</TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {roles.map((role) => (
                                <TableRow key={role.roleId}>
                                    <TableCell className="font-semibold">
                                        <div className="flex items-center gap-2">
                                            <ShieldCheck className="h-4 w-4 text-primary" />
                                            {role.name}
                                        </div>
                                    </TableCell>
                                    <TableCell className="text-sm text-muted-foreground">
                                        {role.description || '-'}
                                    </TableCell>
                                    <TableCell>
                                        <Badge variant="secondary" className="text-xs">
                                            {role.userCount} users
                                        </Badge>
                                    </TableCell>
                                    <TableCell className="text-right">
                                        <DropdownMenu>
                                            <DropdownMenuTrigger asChild>
                                                <Button variant="ghost" size="icon" className="h-8 w-8">
                                                    <MoreVertical className="h-4 w-4" />
                                                </Button>
                                            </DropdownMenuTrigger>
                                            <DropdownMenuContent align="end">
                                                <DropdownMenuItem>Edit Role</DropdownMenuItem>
                                                <DropdownMenuItem>Manage Permissions</DropdownMenuItem>
                                                <DropdownMenuItem className="text-destructive">Delete Role</DropdownMenuItem>
                                            </DropdownMenuContent>
                                        </DropdownMenu>
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
            />
        </div>
    );
}
