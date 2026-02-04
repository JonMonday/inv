'use client';

import { Bell, Filter } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';

import { Button } from '@/components/ui/button';

export default function NotificationsPage() {
    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight">Notifications</h1>
                    <p className="text-sm text-muted-foreground">Stay updated with system activities and alerts</p>
                </div>
                <Button variant="outline" size="sm">
                    <Filter className="mr-2 h-4 w-4" /> Filter
                </Button>
            </div>

            <Card>
                <CardHeader>
                    <CardTitle>Recent Notifications</CardTitle>
                    <CardDescription>You have no new notifications at this time</CardDescription>
                </CardHeader>
                <CardContent>
                    <div className="flex flex-col items-center justify-center py-12 text-center">
                        <Bell className="h-12 w-12 text-muted-foreground opacity-20 mb-4" />
                        <p className="text-sm text-muted-foreground">All caught up!</p>
                        <p className="text-xs text-muted-foreground mt-1">You&apos;ll see notifications here when there&apos;s activity</p>
                    </div>
                </CardContent>
            </Card>
        </div>
    );
}
