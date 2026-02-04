'use client';

import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import {
    BarChart3,
    FileText,
    Download,
    TrendingUp,
    AlertCircle,
    Clock,
    ArrowRight
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import Link from 'next/link';

export default function ReportsCenterPage() {
    const reportTypes = [
        {
            title: "Inventory Valuation",
            description: "Summary of stock on hand and total monetary value by warehouse.",
            icon: <TrendingUp className="h-5 w-5 text-emerald-500" />,
            href: "/reports/valuation"
        },
        {
            title: "Request Lead Time",
            description: "Analyze the time taken from submission to final fulfillment.",
            icon: <Clock className="h-5 w-5 text-blue-500" />,
            href: "/reports/performance"
        },
        {
            title: "Stock Movement Audit",
            description: "Detailed breakdown of all issues, receipts and transfers.",
            icon: <FileText className="h-5 w-5 text-purple-500" />,
            href: "/inventory/movements"
        },
        {
            title: "Low Stock Alerts",
            description: "Identify items approaching or below reorder points.",
            icon: <AlertCircle className="h-5 w-5 text-amber-500" />,
            href: "/reports/low-stock"
        }
    ];

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight text-foreground text-center sm:text-left">Reports Center</h1>
                    <p className="text-sm text-muted-foreground text-center sm:text-left">Generate and export system data for business intelligence.</p>
                </div>
                <Button size="sm">
                    <Download className="mr-2 h-4 w-4" /> Export Center
                </Button>
            </div>

            <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
                <Card className="bg-primary/5 border-primary/20">
                    <CardHeader className="pb-2">
                        <CardDescription className="text-xs uppercase font-bold text-primary">Total Inventory Value</CardDescription>
                        <CardTitle className="text-2xl">$1,245,000</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="text-[10px] text-muted-foreground">+2.5% from last month</div>
                    </CardContent>
                </Card>
                <Card>
                    <CardHeader className="pb-2">
                        <CardDescription className="text-xs uppercase font-bold">Active Requests</CardDescription>
                        <CardTitle className="text-2xl">142</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="text-[10px] text-muted-foreground">Across all departments</div>
                    </CardContent>
                </Card>
                <Card>
                    <CardHeader className="pb-2">
                        <CardDescription className="text-xs uppercase font-bold">Pending Fulfillment</CardDescription>
                        <CardTitle className="text-2xl">18</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="text-[10px] text-destructive font-semibold">5 Escalated</div>
                    </CardContent>
                </Card>
                <Card>
                    <CardHeader className="pb-2">
                        <CardDescription className="text-xs uppercase font-bold">Stock Precision</CardDescription>
                        <CardTitle className="text-2xl">99.2%</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="text-[10px] text-muted-foreground">Based on last audit</div>
                    </CardContent>
                </Card>
            </div>

            <div className="grid gap-6 md:grid-cols-2">
                <Card>
                    <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                            <BarChart3 className="h-5 w-5 text-primary" /> Available Reports
                        </CardTitle>
                        <CardDescription>Select a report template to view or download data.</CardDescription>
                    </CardHeader>
                    <CardContent className="grid gap-4">
                        {reportTypes.map((report) => (
                            <Link
                                key={report.title}
                                href={report.href}
                                className="flex items-center justify-between p-4 rounded-lg border bg-card hover:bg-muted/50 transition-colors group"
                            >
                                <div className="flex items-center gap-4">
                                    <div className="p-2 rounded-full bg-background border">
                                        {report.icon}
                                    </div>
                                    <div>
                                        <h4 className="text-sm font-semibold">{report.title}</h4>
                                        <p className="text-xs text-muted-foreground line-clamp-1">{report.description}</p>
                                    </div>
                                </div>
                                <ArrowRight className="h-4 w-4 text-muted-foreground group-hover:text-primary transition-colors" />
                            </Link>
                        ))}
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader>
                        <CardTitle>Recent Exports</CardTitle>
                        <CardDescription>List of recently generated data files.</CardDescription>
                    </CardHeader>
                    <CardContent>
                        <div className="space-y-4">
                            <div className="flex items-center justify-between py-2 border-b last:border-0">
                                <div className="flex items-center gap-3">
                                    <FileText className="h-4 w-4 text-muted-foreground" />
                                    <div>
                                        <p className="text-sm font-medium">Inventory_Stock_Levels_Jan30.csv</p>
                                        <p className="text-[10px] text-muted-foreground">Generated by John Doe • 2MB</p>
                                    </div>
                                </div>
                                <Button variant="ghost" size="sm">Download</Button>
                            </div>
                            <div className="flex items-center justify-between py-2 border-b last:border-0">
                                <div className="flex items-center gap-3">
                                    <FileText className="h-4 w-4 text-muted-foreground" />
                                    <div>
                                        <p className="text-sm font-medium">Fulfillment_Latency_Q4.xlsx</p>
                                        <p className="text-[10px] text-muted-foreground">Generated by Admin • 5.2MB</p>
                                    </div>
                                </div>
                                <Button variant="ghost" size="sm">Download</Button>
                            </div>
                        </div>
                    </CardContent>
                </Card>
            </div>
        </div>
    );
}
