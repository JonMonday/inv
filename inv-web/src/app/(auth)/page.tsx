'use client';

import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import {
    ClipboardList,
    ShoppingCart,
    Package,
    AlertTriangle,
    CheckCircle2,
    ArrowUpRight
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import Link from 'next/link';

interface StatCardProps {
    title: string;
    value: string;
    description: string;
    icon: React.ReactNode;
    trend: 'up' | 'down';
}

interface ActivityItemProps {
    title: string;
    time: string;
    user: string;
    status: string;
}

export default function DashboardPage() {
    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <h1 className="text-2xl font-bold tracking-tight">Dashboard Overview</h1>
                <div className="flex items-center gap-2">
                    <Button size="sm" variant="outline">Last 7 Days</Button>
                    <Link href="/requests/new">
                        <Button size="sm">New Request</Button>
                    </Link>
                </div>
            </div>

            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
                <StatCard
                    title="Pending Tasks"
                    value="12"
                    description="+2 since yesterday"
                    icon={<ClipboardList className="h-4 w-4 text-primary" />}
                    trend="up"
                />
                <StatCard
                    title="Active Requests"
                    value="45"
                    description="8 awaiting approval"
                    icon={<ShoppingCart className="h-4 w-4 text-primary" />}
                    trend="up"
                />
                <StatCard
                    title="Low Stock Items"
                    value="3"
                    description="Needs immediate attention"
                    icon={<AlertTriangle className="h-4 w-4 text-destructive" />}
                    trend="down"
                />
                <StatCard
                    title="Completed Today"
                    value="28"
                    description="Inventory issues"
                    icon={<CheckCircle2 className="h-4 w-4 text-emerald-500" />}
                    trend="up"
                />
            </div>

            <div className="grid gap-6 md:grid-cols-1 lg:grid-cols-7">
                <Card className="lg:col-span-4">
                    <CardHeader>
                        <CardTitle>Recent Activity</CardTitle>
                        <CardDescription>Latest tasks and request updates</CardDescription>
                    </CardHeader>
                    <CardContent>
                        <div className="space-y-4">
                            <ActivityItem
                                title="Task Claimed: REQ-1049"
                                time="2 hours ago"
                                user="John Doe"
                                status="IN_PROGRESS"
                            />
                            <ActivityItem
                                title="New Request: Stationery Office B"
                                time="5 hours ago"
                                user="Alice Smith"
                                status="DRAFT"
                            />
                            <ActivityItem
                                title="Stock Issued: Laptop L-4"
                                time="Yesterday"
                                user="Storekeeper Bob"
                                status="COMPLETED"
                            />
                        </div>
                    </CardContent>
                </Card>

                <Card className="lg:col-span-3">
                    <CardHeader>
                        <CardTitle>Quick Links</CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-2">
                        <Button variant="ghost" className="w-full justify-between" asChild>
                            <Link href="/tasks">
                                <span className="flex items-center gap-2"><ClipboardList className="h-4 w-4" /> Go to My Tasks</span>
                                <ArrowUpRight className="h-4 w-4 text-muted-foreground" />
                            </Link>
                        </Button>
                        <Button variant="ghost" className="w-full justify-between" asChild>
                            <Link href="/requests">
                                <span className="flex items-center gap-2"><ShoppingCart className="h-4 w-4" /> View All Requests</span>
                                <ArrowUpRight className="h-4 w-4 text-muted-foreground" />
                            </Link>
                        </Button>
                        <Button variant="ghost" className="w-full justify-between" asChild>
                            <Link href="/reference/products">
                                <span className="flex items-center gap-2"><Package className="h-4 w-4" /> Inventory Catalog</span>
                                <ArrowUpRight className="h-4 w-4 text-muted-foreground" />
                            </Link>
                        </Button>
                    </CardContent>
                </Card>
            </div>
        </div>
    );
}

function StatCard({ title, value, description, icon }: StatCardProps) {
    return (
        <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">{title}</CardTitle>
                {icon}
            </CardHeader>
            <CardContent>
                <div className="text-2xl font-bold">{value}</div>
                <p className="text-xs text-muted-foreground mt-1">
                    {description}
                </p>
            </CardContent>
        </Card>
    );
}

function ActivityItem({ title, time, user, status }: ActivityItemProps) {
    return (
        <div className="flex items-center gap-4 text-sm border-b last:border-0 pb-4 last:pb-0">
            <div className="flex-1 space-y-1">
                <p className="font-medium">{title}</p>
                <div className="flex items-center gap-2 text-xs text-muted-foreground">
                    <span>{user}</span>
                    <span>•</span>
                    <span>{time}</span>
                </div>
            </div>
            <div className="text-[10px] uppercase font-bold text-muted-foreground bg-muted px-2 py-1 rounded">
                {status}
            </div>
        </div>
    );
}
