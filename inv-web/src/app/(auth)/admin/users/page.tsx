'use client';

import { useUsers } from '@/hooks/useAdmin';
import apiClient from '@/lib/api/client';
import { useToast } from '@/hooks/use-toast';
import { useState, useCallback, useEffect } from 'react';
import { PaginationControls } from '@/components/ui/pagination-controls';
import { Search } from 'lucide-react';
import {
    Dialog,
    DialogContent,
    DialogHeader,
    DialogTitle,
    DialogFooter,
    DialogDescription
} from "@/components/ui/dialog";
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
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
import { Skeleton } from '@/components/ui/skeleton';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { MoreHorizontal, UserPlus, Key, Edit2, Check } from 'lucide-react';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger
} from '@/components/ui/dropdown-menu';
import { debounce } from 'lodash';
import { useCreateUser, useUpdateUser, useRoles, useUser } from '@/hooks/useAdmin';
import { Switch } from '@/components/ui/switch';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Card } from '@/components/ui/card';

interface AdminUser {
    userId: number;
    username: string;
    displayName: string;
    email: string;
    isActive: boolean;
    roles: string[];
}

export default function AdminUsersPage() {
    const [page, setPage] = useState(1);
    const [searchTerm, setSearchTerm] = useState('');
    const { data: usersData, isLoading } = useUsers({ pageNumber: page, pageSize: 10, searchTerm });
    const { data: rolesData } = useRoles({ pageNumber: 1, pageSize: 50 });
    const { toast } = useToast();

    const [resetUser, setResetUser] = useState<AdminUser | null>(null);
    const [newPassword, setNewPassword] = useState('');
    const [isResetting, setIsResetting] = useState(false);

    // User Edit/Add state
    const [isUserDialogOpen, setIsUserDialogOpen] = useState(false);
    const [editingUserId, setEditingUserId] = useState<number | null>(null);
    const { data: userDetail, isLoading: isLoadingDetail } = useUser(editingUserId);

    const createUser = useCreateUser();
    const updateUser = useUpdateUser();

    const [formData, setFormData] = useState({
        username: '',
        displayName: '',
        email: '',
        isActive: true,
        roleIds: [] as number[]
    });

    const users = usersData?.data || [];
    const totalPages = usersData?.totalPages || 0;
    const totalRecords = usersData?.totalRecords || 0;

    const debouncedSearch = useCallback(
        debounce((term: string) => {
            setSearchTerm(term);
            setPage(1);
        }, 500),
        []
    );

    const handleResetPassword = async () => {
        if (!newPassword || !resetUser) return;
        setIsResetting(true);
        try {
            await apiClient.post(`/api/admin/users/${resetUser.userId}/reset-password`, { newPassword });
            toast({ title: 'Success', description: 'Password reset successfully.' });
            setResetUser(null);
            setNewPassword('');
        } catch (error: unknown) {
            const message = error instanceof Error ? error.message : 'Unknown error';
            toast({ title: 'Error', description: message, variant: 'destructive' });
        } finally {
            setIsResetting(false);
        }
    };

    const openCreateDialog = () => {
        setEditingUserId(null);
        setFormData({
            username: '',
            displayName: '',
            email: '',
            isActive: true,
            roleIds: []
        });
        setIsUserDialogOpen(true);
    };

    const openEditDialog = (u: AdminUser) => {
        setEditingUserId(u.userId);
        setIsUserDialogOpen(true);
    };

    // Update form when detail is loaded
    useEffect(() => {
        if (userDetail && editingUserId) {
            setFormData({
                username: userDetail.username,
                displayName: userDetail.displayName,
                email: userDetail.email,
                isActive: userDetail.isActive,
                roleIds: userDetail.roleIds || []
            });
        }
    }, [userDetail, editingUserId]);

    const handleSaveUser = async () => {
        try {
            if (editingUserId) {
                await updateUser.mutateAsync({
                    userId: editingUserId,
                    data: {
                        displayName: formData.displayName,
                        email: formData.email,
                        isActive: formData.isActive,
                        roleIds: formData.roleIds
                    }
                });
                toast({ title: 'User Updated', description: 'Changes saved successfully.' });
            } else {
                await createUser.mutateAsync(formData);
                toast({ title: 'User Created', description: 'New user added successfully.' });
            }
            setIsUserDialogOpen(false);
        } catch (error: unknown) {
            const err = error as { response?: { data?: { message?: string } } };
            const message = err.response?.data?.message || (error as Error).message || 'Something went wrong.';
            toast({
                title: 'Operation Failed',
                description: message,
                variant: 'destructive'
            });
        }
    };

    const toggleRole = (roleId: number) => {
        setFormData(prev => ({
            ...prev,
            roleIds: prev.roleIds.includes(roleId)
                ? prev.roleIds.filter(id => id !== roleId)
                : [...prev.roleIds, roleId]
        }));
    };

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-xl font-bold">User Management</h1>
                    <p className="text-sm text-muted-foreground">Manage system users and their assigned roles</p>
                </div>
                <Button size="sm" onClick={openCreateDialog}>
                    <UserPlus className="mr-2 h-4 w-4" /> Add User
                </Button>
            </div>

            <div className="relative">
                <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                <Input
                    type="search"
                    placeholder="Search users..."
                    className="pl-8 max-w-sm"
                    onChange={(e) => debouncedSearch(e.target.value)}
                />
            </div>

            <div className="rounded-md border bg-card">
                <Table>
                    <TableHeader>
                        <TableRow>
                            <TableHead>User</TableHead>
                            <TableHead>Email</TableHead>
                            <TableHead>Roles</TableHead>
                            <TableHead>Status</TableHead>
                            <TableHead className="text-right">Actions</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {isLoading ? (
                            Array.from({ length: 5 }).map((_, i) => (
                                <TableRow key={i}>
                                    <TableCell>
                                        <div className="flex items-center gap-3">
                                            <Skeleton className="h-8 w-8 rounded-full" />
                                            <Skeleton className="h-4 w-24" />
                                        </div>
                                    </TableCell>
                                    <TableCell><Skeleton className="h-4 w-40" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-16" /></TableCell>
                                    <TableCell className="text-right"><Skeleton className="h-8 w-8 ml-auto" /></TableCell>
                                </TableRow>
                            ))
                        ) : (
                            users.map((u) => (
                                <TableRow key={u.userId}>
                                    <TableCell>
                                        <div className="flex items-center gap-3">
                                            <Avatar className="h-8 w-8">
                                                <AvatarFallback>{u.displayName[0]}</AvatarFallback>
                                            </Avatar>
                                            <div className="flex flex-col">
                                                <span className="text-sm font-medium">{u.displayName}</span>
                                                <span className="text-[10px] text-muted-foreground">@{u.username}</span>
                                            </div>
                                        </div>
                                    </TableCell>
                                    <TableCell className="text-sm">{u.email}</TableCell>
                                    <TableCell>
                                        <div className="flex gap-1 flex-wrap">
                                            {u.roles.map((role: string) => (
                                                <Badge key={role} variant="secondary" className="text-[9px] font-bold">
                                                    {role}
                                                </Badge>
                                            ))}
                                        </div>
                                    </TableCell>
                                    <TableCell>
                                        <Badge variant={u.isActive ? "default" : "outline"} className="text-[10px]">
                                            {u.isActive ? 'Active' : 'Inactive'}
                                        </Badge>
                                    </TableCell>
                                    <TableCell className="text-right">
                                        <DropdownMenu>
                                            <DropdownMenuTrigger asChild>
                                                <Button variant="ghost" size="icon" className="h-8 w-8">
                                                    <MoreHorizontal className="h-4 w-4" />
                                                </Button>
                                            </DropdownMenuTrigger>
                                            <DropdownMenuContent align="end">
                                                <DropdownMenuItem onClick={() => openEditDialog(u)}>
                                                    <Edit2 className="mr-2 h-4 w-4" /> Edit User
                                                </DropdownMenuItem>
                                                <DropdownMenuItem onClick={() => setResetUser(u)}>
                                                    <Key className="mr-2 h-4 w-4" /> Reset Password
                                                </DropdownMenuItem>
                                                <DropdownMenuItem className="text-destructive">Deactivate</DropdownMenuItem>
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
            />

            <Dialog open={!!resetUser} onOpenChange={() => setResetUser(null)}>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Reset Password</DialogTitle>
                        <DialogDescription>
                            Enter a new password for {resetUser?.displayName}. The user will be forced to refresh their session.
                        </DialogDescription>
                    </DialogHeader>
                    <div className="space-y-4 py-4">
                        <div className="space-y-2">
                            <Label htmlFor="password">New Password</Label>
                            <Input
                                id="password"
                                type="password"
                                value={newPassword}
                                onChange={(e) => setNewPassword(e.target.value)}
                                placeholder="Enter secure password..."
                            />
                        </div>
                    </div>
                    <DialogFooter>
                        <Button variant="outline" onClick={() => setResetUser(null)}>Cancel</Button>
                        <Button onClick={handleResetPassword} disabled={!newPassword || isResetting}>
                            {isResetting ? 'Resetting...' : 'Reset Password'}
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            <Dialog open={isUserDialogOpen} onOpenChange={setIsUserDialogOpen}>
                <DialogContent className="max-w-md">
                    <DialogHeader>
                        <DialogTitle>{editingUserId ? 'Edit User' : 'Add New User'}</DialogTitle>
                        <DialogDescription>
                            {editingUserId ? 'Update account information and roles.' : 'Create a new user account and assign roles.'}
                        </DialogDescription>
                    </DialogHeader>

                    {isLoadingDetail && editingUserId ? (
                        <div className="py-8 text-center text-muted-foreground italic">Fetching user details...</div>
                    ) : (
                        <div className="space-y-4 py-4">
                            <div className="grid grid-cols-2 gap-4">
                                <div className="space-y-2">
                                    <Label>Username</Label>
                                    <Input
                                        value={formData.username}
                                        onChange={(e) => setFormData({ ...formData, username: e.target.value })}
                                        disabled={!!editingUserId}
                                        placeholder="jdoe"
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label>Status</Label>
                                    <div className="flex items-center gap-2 h-10 px-1">
                                        <Switch
                                            checked={formData.isActive}
                                            onCheckedChange={(checked) => setFormData({ ...formData, isActive: checked })}
                                        />
                                        <span className="text-sm">{formData.isActive ? 'Active' : 'Inactive'}</span>
                                    </div>
                                </div>
                            </div>

                            <div className="space-y-2">
                                <Label>Display Name</Label>
                                <Input
                                    value={formData.displayName}
                                    onChange={(e) => setFormData({ ...formData, displayName: e.target.value })}
                                    placeholder="John Doe"
                                />
                            </div>

                            <div className="space-y-2">
                                <Label>Email Address</Label>
                                <Input
                                    type="email"
                                    value={formData.email}
                                    onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                                    placeholder="john.doe@example.com"
                                />
                            </div>

                            <div className="space-y-2">
                                <Label>Assigned Roles</Label>
                                <Card className="p-0 overflow-hidden">
                                    <ScrollArea className="h-40">
                                        <div className="p-2 space-y-1">
                                            {rolesData?.data.map((role) => (
                                                <div
                                                    key={role.roleId}
                                                    onClick={() => toggleRole(role.roleId)}
                                                    className={`
                                                        flex items-center justify-between px-3 py-2 rounded-md cursor-pointer text-sm transition-colors
                                                        ${formData.roleIds.includes(role.roleId)
                                                            ? 'bg-primary/10 text-primary font-medium'
                                                            : 'hover:bg-muted text-muted-foreground'}
                                                    `}
                                                >
                                                    <div className="flex flex-col">
                                                        <span>{role.name}</span>
                                                        <span className="text-[10px] opacity-70">{role.code}</span>
                                                    </div>
                                                    {formData.roleIds.includes(role.roleId) && (
                                                        <Check className="h-4 w-4" />
                                                    )}
                                                </div>
                                            ))}
                                        </div>
                                    </ScrollArea>
                                </Card>
                            </div>
                        </div>
                    )}

                    <DialogFooter>
                        <Button variant="outline" onClick={() => setIsUserDialogOpen(false)}>Cancel</Button>
                        <Button onClick={handleSaveUser} disabled={createUser.isPending || updateUser.isPending}>
                            {createUser.isPending || updateUser.isPending ? 'Saving...' : 'Save User'}
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </div >
    );
}
