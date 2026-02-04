'use client';

import { useDepartments } from '@/hooks/useAdmin';
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
    Building2,
    Plus,
    MoreVertical,
    Users as UsersIcon,
    ShieldAlert
} from 'lucide-react';
import { Skeleton } from '@/components/ui/skeleton';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

interface Department {
    departmentId: number;
    name: string;
    code?: string;
    headUserId?: number;
}

export default function DepartmentsPage() {
    const { data: departments, isLoading } = useDepartments();

    const depts = (departments as Department[]) || [];

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight text-foreground">Departments</h1>
                    <p className="text-sm text-muted-foreground">Manage organization units and cost centers.</p>
                </div>
                <Button size="sm">
                    <Plus className="mr-2 h-4 w-4" /> New Department
                </Button>
            </div>

            <div className="rounded-md border bg-card overflow-hidden">
                <Table>
                    <TableHeader className="bg-muted/50">
                        <TableRow>
                            <TableHead>Department Name</TableHead>
                            <TableHead>Code</TableHead>
                            <TableHead>Head of Dept</TableHead>
                            <TableHead className="text-right">Actions</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {isLoading ? (
                            Array.from({ length: 4 }).map((_, i) => (
                                <TableRow key={i}>
                                    <TableCell><Skeleton className="h-4 w-48" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-16" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                                    <TableCell className="text-right"><Skeleton className="h-8 w-8 ml-auto" /></TableCell>
                                </TableRow>
                            ))
                        ) : depts.length === 0 ? (
                            <TableRow>
                                <TableCell colSpan={4} className="h-48 text-center text-muted-foreground">
                                    <div className="flex flex-col items-center gap-2">
                                        <Building2 className="h-8 w-8 opacity-20" />
                                        <p>No departments configured in the system.</p>
                                    </div>
                                </TableCell>
                            </TableRow>
                        ) : (
                            depts.map((dept) => (
                                <TableRow key={dept.departmentId} className="group hover:bg-muted/30 transition-colors">
                                    <TableCell className="font-semibold text-foreground">
                                        <div className="flex items-center gap-2">
                                            <div className="h-8 w-8 rounded-lg bg-primary/5 flex items-center justify-center">
                                                <Building2 className="h-4 w-4 text-primary" />
                                            </div>
                                            {dept.name}
                                        </div>
                                    </TableCell>
                                    <TableCell>
                                        <Badge variant="secondary" className="font-mono text-[10px] tracking-wider px-2 py-0.5">
                                            {dept.code || `DEPT-${dept.departmentId}`}
                                        </Badge>
                                    </TableCell>
                                    <TableCell className="text-sm text-muted-foreground">
                                        {dept.headUserId ? `User ID: ${dept.headUserId}` : 'Unassigned'}
                                    </TableCell>
                                    <TableCell className="text-right">
                                        <DropdownMenu>
                                            <DropdownMenuTrigger asChild>
                                                <Button variant="ghost" size="icon" className="h-8 w-8 opacity-0 group-hover:opacity-100 transition-opacity">
                                                    <MoreVertical className="h-4 w-4" />
                                                </Button>
                                            </DropdownMenuTrigger>
                                            <DropdownMenuContent align="end" className="w-48">
                                                <DropdownMenuItem className="gap-2">
                                                    <Building2 className="h-4 w-4" /> Edit Details
                                                </DropdownMenuItem>
                                                <DropdownMenuItem className="gap-2">
                                                    <UsersIcon className="h-4 w-4" /> Manage Members
                                                </DropdownMenuItem>
                                                <DropdownMenuItem className="gap-2 text-destructive focus:text-destructive">
                                                    <ShieldAlert className="h-4 w-4" /> Delete Department
                                                </DropdownMenuItem>
                                            </DropdownMenuContent>
                                        </DropdownMenu>
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
