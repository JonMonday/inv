'use client';

import { Save } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { Separator } from '@/components/ui/separator';

export default function SettingsPage() {
    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight">Settings</h1>
                    <p className="text-sm text-muted-foreground">Manage system configuration and preferences</p>
                </div>
            </div>

            <div className="grid gap-6">
                <Card>
                    <CardHeader>
                        <CardTitle>General Settings</CardTitle>
                        <CardDescription>Configure basic system settings</CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        <div className="space-y-2">
                            <Label htmlFor="company">Company Name</Label>
                            <Input id="company" placeholder="Your Company Name" defaultValue="InvServer Corp" />
                        </div>
                        <div className="space-y-2">
                            <Label htmlFor="email">System Email</Label>
                            <Input id="email" type="email" placeholder="admin@company.com" defaultValue="admin@invserver.com" />
                        </div>
                        <Separator />
                        <div className="flex items-center justify-between">
                            <div className="space-y-0.5">
                                <Label>Email Notifications</Label>
                                <p className="text-xs text-muted-foreground">Send email notifications for system events</p>
                            </div>
                            <Switch defaultChecked />
                        </div>
                        <div className="flex items-center justify-between">
                            <div className="space-y-0.5">
                                <Label>Auto-approve Low Value Requests</Label>
                                <p className="text-xs text-muted-foreground">Automatically approve requests under threshold</p>
                            </div>
                            <Switch />
                        </div>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader>
                        <CardTitle>Workflow Settings</CardTitle>
                        <CardDescription>Configure workflow behavior</CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        <div className="space-y-2">
                            <Label htmlFor="threshold">Auto-approval Threshold ($)</Label>
                            <Input id="threshold" type="number" placeholder="1000" defaultValue="500" />
                        </div>
                        <div className="flex items-center justify-between">
                            <div className="space-y-0.5">
                                <Label>Require Department Approval</Label>
                                <p className="text-xs text-muted-foreground">All requests must be approved by department head</p>
                            </div>
                            <Switch defaultChecked />
                        </div>
                    </CardContent>
                </Card>

                <div className="flex justify-end gap-2">
                    <Button variant="outline">Cancel</Button>
                    <Button>
                        <Save className="mr-2 h-4 w-4" /> Save Changes
                    </Button>
                </div>
            </div>
        </div>
    );
}
