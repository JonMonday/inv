'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import {
    LayoutDashboard,
    Bell,
    UserCircle,
    ShoppingCart,
    Truck,
    Box,
    Package,
    Warehouse,
    History,
    FileText,
    BarChart3,
    ShieldCheck,
    Settings,
    HelpCircle,
    PlusCircle,
    Inbox,
    FilePlus2,
    ClipboardList,
    Users
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { Badge } from '@/components/ui/badge';
import { LucideIcon } from 'lucide-react';

interface NavItem {
    name: string;
    href: string;
    icon: LucideIcon;
    badge?: number;
}

interface NavGroup {
    title: string;
    items: NavItem[];
}

const navGroups: NavGroup[] = [
    {
        title: 'Workflows',
        items: [
            { name: 'Inbox', href: '/tasks', icon: Inbox, badge: 3 },
            { name: 'Drafts', href: '/requests/drafts', icon: FileText },
            { name: 'New Request', href: '/requests/new', icon: FilePlus2 },
            { name: 'Participated', href: '/requests/participated', icon: History },
            { name: 'Requests', href: '/requests', icon: ShoppingCart },
        ]
    },
    {
        title: 'Operations',
        items: [
            { name: 'Dashboard', href: '/', icon: LayoutDashboard },
            { name: 'Fulfillment', href: '/fulfillment', icon: Truck },
            { name: 'Notifications', href: '/notifications', icon: Bell },
            { name: 'Profile', href: '/profile', icon: UserCircle },
        ]
    },
    {
        title: 'Inventory',
        items: [
            { name: 'Stock Levels', href: '/inventory/levels', icon: Box },
            { name: 'Products', href: '/reference/products', icon: Package },
            { name: 'Categories', href: '/reference/categories', icon: Box },
            { name: 'Warehouses', href: '/reference/warehouses', icon: Warehouse },
            { name: 'Movements', href: '/inventory/movements', icon: History },
        ]
    },
    {
        title: 'Workflow',
        items: [
            { name: 'Workflow Templates', href: '/admin/workflows', icon: FileText },
        ]
    },
    {
        title: 'Admin',
        items: [
            { name: 'Users', href: '/admin/users', icon: Users },
            { name: 'Roles & Perms', href: '/admin/roles', icon: ShieldCheck },
            { name: 'Permissions', href: '/admin/permissions', icon: ShieldCheck },
            { name: 'Departments', href: '/admin/departments', icon: Box },
        ]
    },
    {
        title: 'Reference Data',
        items: [
            // Inventory
            { name: 'Request Status', href: '/admin/reference/inventory-request-status', icon: FileText },
            { name: 'Request Types', href: '/admin/reference/inventory-request-type', icon: FileText },
            { name: 'Movement Types', href: '/admin/reference/inventory-movement-type', icon: History },
            { name: 'Movement Status', href: '/admin/reference/inventory-movement-status', icon: History },
            { name: 'Reason Codes', href: '/admin/reference/inventory-reason-code', icon: FileText },
            { name: 'Res. Status', href: '/admin/reference/reservation-status', icon: FileText },
            // Workflow
            { name: 'WF Step Types', href: '/admin/reference/workflow-step-type', icon: ClipboardList },
            { name: 'WF Action Types', href: '/admin/reference/workflow-action-type', icon: ClipboardList },
            { name: 'WF Assignment', href: '/admin/reference/workflow-assignment-mode', icon: Users },
            { name: 'WF Conditions', href: '/admin/reference/workflow-condition-operator', icon: Settings },
            { name: 'WF Inst. Status', href: '/admin/reference/workflow-instance-status', icon: History },
            { name: 'WF Task Status', href: '/admin/reference/workflow-task-status', icon: ClipboardList },
            { name: 'WF Assign. Sts', href: '/admin/reference/workflow-task-assignee-status', icon: Users },
            // System
            { name: 'Access Scopes', href: '/admin/reference/access-scope-type', icon: ShieldCheck },
            { name: 'Sec. Events', href: '/admin/reference/security-event-type', icon: ShieldCheck },
        ]
    },
    {
        title: 'Reporting',
        items: [
            { name: 'Reports Center', href: '/reports', icon: BarChart3 },
            { name: 'Audit Logs', href: '/audit', icon: History },
        ]
    },
    {
        title: 'Support',
        items: [
            { name: 'Settings', href: '/settings', icon: Settings },
            { name: 'Help & FAQ', href: '/help', icon: HelpCircle },
        ]
    }
];

export function Sidebar() {
    const pathname = usePathname();

    return (
        <div className="flex h-full w-64 flex-col border-r bg-card text-card-foreground">
            <div className="flex h-14 items-center border-b px-6">
                <span className="text-lg font-bold tracking-tight">InvServer</span>
            </div>
            <div className="flex-1 overflow-y-auto py-4">
                {navGroups.map((group) => (
                    <div key={group.title} className="px-3 py-2">
                        <h2 className="mb-2 px-4 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                            {group.title}
                        </h2>
                        <div className="space-y-1">
                            {group.items.map((item) => (
                                <Link
                                    key={item.href}
                                    href={item.href}
                                    className={cn(
                                        "group flex items-center justify-between rounded-md px-4 py-2 text-sm font-medium transition-colors hover:bg-accent hover:text-accent-foreground",
                                        pathname === item.href ? "bg-accent text-accent-foreground" : "text-muted-foreground"
                                    )}
                                >
                                    <div className="flex items-center">
                                        <item.icon className="mr-3 h-4 w-4" />
                                        {item.name}
                                    </div>
                                    {item.badge && (
                                        <Badge variant="secondary" className="ml-auto h-5 px-1.5 text-[10px] font-bold">
                                            {item.badge}
                                        </Badge>
                                    )}
                                </Link>
                            ))}
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
}
