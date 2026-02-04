'use client';

import { useAuthStore } from '@/store/authStore';
import { Card, CardContent, CardHeader, CardTitle, CardDescription, CardFooter } from '@/components/ui/card';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Shield, User, Building, Key, Save } from 'lucide-react';
import { useToast } from '@/hooks/use-toast';
import { useState } from 'react';

export default function ProfilePage() {
    const { user } = useAuthStore();
    const { toast } = useToast();
    const [isChangingPassword, setIsChangingPassword] = useState(false);

    if (!user) return null;

    const handlePasswordChange = (e: React.FormEvent) => {
        e.preventDefault();
        toast({ title: 'Password change requested', description: 'This feature is currently being wired up.' });
        setIsChangingPassword(false);
    };

    return (
        <div className="max-w-4xl mx-auto space-y-6">
            <div className="flex items-center gap-6">
                <Avatar className="h-24 w-24 border-4 border-muted">
                    <AvatarFallback className="text-3xl font-bold bg-primary/10 text-primary">
                        {user.displayName?.[0] || 'U'}
                    </AvatarFallback>
                </Avatar>
                <div className="space-y-1">
                    <h1 className="text-3xl font-bold tracking-tight">{user.displayName}</h1>
                    <p className="text-muted-foreground">@{user.username}</p>
                    <div className="flex gap-2 pt-2">
                        {user.roles.map(role => (
                            <Badge key={role} variant="secondary" className="px-2 py-0.5 text-[10px] font-bold">
                                {role}
                            </Badge>
                        ))}
                    </div>
                </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                <div className="md:col-span-2 space-y-6">
                    <Card>
                        <CardHeader>
                            <CardTitle className="text-sm font-semibold flex items-center gap-2">
                                <User className="h-4 w-4" /> Personal Information
                            </CardTitle>
                            <CardDescription>Manage your basic account details</CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div className="space-y-2">
                                    <Label htmlFor="displayName">Display Name</Label>
                                    <Input id="displayName" defaultValue={user.displayName} />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="email">Email Address</Label>
                                    <Input id="email" type="email" defaultValue={user.email} />
                                </div>
                            </div>
                        </CardContent>
                        <CardFooter className="border-t bg-muted/50 px-6 py-3">
                            <Button size="sm" className="ml-auto">
                                <Save className="mr-2 h-4 w-4" /> Save Changes
                            </Button>
                        </CardFooter>
                    </Card>

                    <Card>
                        <CardHeader>
                            <CardTitle className="text-sm font-semibold flex items-center gap-2">
                                <Key className="h-4 w-4" /> Security
                            </CardTitle>
                            <CardDescription>Change your password</CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            {!isChangingPassword ? (
                                <Button variant="outline" size="sm" onClick={() => setIsChangingPassword(true)}>
                                    Change Password
                                </Button>
                            ) : (
                                <form onSubmit={handlePasswordChange} className="space-y-4 max-w-sm">
                                    <div className="space-y-2">
                                        <Label htmlFor="current">Current Password</Label>
                                        <Input id="current" type="password" />
                                    </div>
                                    <div className="space-y-2">
                                        <Label htmlFor="new">New Password</Label>
                                        <Input id="new" type="password" />
                                    </div>
                                    <div className="space-y-2">
                                        <Label htmlFor="confirm">Confirm New Password</Label>
                                        <Input id="confirm" type="password" />
                                    </div>
                                    <div className="flex gap-2">
                                        <Button type="submit" size="sm">Update Password</Button>
                                        <Button type="button" variant="ghost" size="sm" onClick={() => setIsChangingPassword(false)}>Cancel</Button>
                                    </div>
                                </form>
                            )}
                        </CardContent>
                    </Card>
                </div>

                <div className="space-y-6">
                    <Card>
                        <CardHeader>
                            <CardTitle className="text-sm font-semibold flex items-center gap-2">
                                <Building className="h-4 w-4" /> Organization
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="space-y-1">
                                <p className="text-[10px] uppercase font-bold text-muted-foreground">Departments</p>
                                <div className="flex flex-wrap gap-1">
                                    {user.departments && user.departments.length > 0 ? user.departments.map(dept => (
                                        <Badge key={dept} variant="outline" className="text-[10px]">{dept}</Badge>
                                    )) : (
                                        <span className="text-xs text-muted-foreground">No departments assigned</span>
                                    )}
                                </div>
                            </div>
                            <div className="pt-2 border-t">
                                <p className="text-[10px] uppercase font-bold text-muted-foreground">Access Level</p>
                                <div className="flex items-center gap-2 mt-1">
                                    <Shield className="h-4 w-4 text-primary" />
                                    <span className="text-sm font-medium">Standard User</span>
                                </div>
                            </div>
                        </CardContent>
                    </Card>
                </div>
            </div>
        </div>
    );
}
