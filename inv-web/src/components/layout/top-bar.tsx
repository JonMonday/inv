'use client';

import { useTheme } from 'next-themes';
import {
    Sun,
    Moon,
    User,
    LogOut,
    LayoutList,
    Kanban,
    Calendar,
    BarChart2,
    Bell
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuLabel,
    DropdownMenuSeparator,
    DropdownMenuTrigger
} from '@/components/ui/dropdown-menu';
import { useAuthStore } from '@/store/authStore';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import Link from 'next/link';

export function TopBar({ title }: { title: string }) {
    const { setTheme, theme } = useTheme();
    const { user, clearAuth } = useAuthStore();

    const getInitials = (name: string) => {
        return name.split(' ').map(n => n[0]).join('').toUpperCase();
    };

    return (
        <header className="flex h-14 items-center justify-between border-b bg-background px-6">
            <div className="flex items-center gap-4">
                <h1 className="text-sm font-semibold">{title}</h1>
                <div className="h-4 w-px bg-border" />
                <Tabs defaultValue="list" className="h-8">
                    <TabsList className="bg-transparent h-8 p-0 gap-1">
                        <TabsTrigger value="list" className="h-7 px-2 text-[11px] data-[state=active]:bg-accent">
                            <LayoutList className="mr-1.5 h-3 w-3" /> List
                        </TabsTrigger>
                        <TabsTrigger value="board" className="h-7 px-2 text-[11px] data-[state=active]:bg-accent">
                            <Kanban className="mr-1.5 h-3 w-3" /> Board
                        </TabsTrigger>
                        <TabsTrigger value="calendar" className="h-7 px-2 text-[11px] data-[state=active]:bg-accent">
                            <Calendar className="mr-1.5 h-3 w-3" /> Calendar
                        </TabsTrigger>
                        <TabsTrigger value="gantt" className="h-7 px-2 text-[11px] data-[state=active]:bg-accent">
                            <BarChart2 className="mr-1.5 h-3 w-3" /> Gantt
                        </TabsTrigger>
                    </TabsList>
                </Tabs>
            </div>

            <div className="flex items-center gap-2">
                <Button variant="ghost" size="icon" className="h-8 w-8">
                    <Bell className="h-4 w-4 text-muted-foreground" />
                </Button>
                <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8"
                    onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
                >
                    <Sun className="h-4 w-4 rotate-0 scale-100 transition-all dark:-rotate-90 dark:scale-0" />
                    <Moon className="absolute h-4 w-4 rotate-90 scale-0 transition-all dark:rotate-0 dark:scale-100" />
                    <span className="sr-only">Toggle theme</span>
                </Button>

                <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                        <Button variant="ghost" className="h-8 gap-2 px-1">
                            <Avatar className="h-7 w-7">
                                <AvatarFallback className="text-[10px] bg-primary text-primary-foreground font-bold">
                                    {user ? getInitials(user.displayName) : 'U'}
                                </AvatarFallback>
                            </Avatar>
                            <div className="hidden flex-col items-start md:flex">
                                <span className="text-xs font-medium leading-none">{user?.displayName}</span>
                                <span className="text-[10px] text-muted-foreground leading-none mt-1">{user?.roles[0]}</span>
                            </div>
                        </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end" className="w-56">
                        <DropdownMenuLabel>My Account</DropdownMenuLabel>
                        <DropdownMenuSeparator />
                        <DropdownMenuItem asChild>
                            <Link href="/profile" className="flex w-full cursor-pointer items-center">
                                <User className="mr-2 h-4 w-4" />
                                <span>Profile</span>
                            </Link>
                        </DropdownMenuItem>
                        <DropdownMenuSeparator />
                        <DropdownMenuItem onClick={() => clearAuth()} className="cursor-pointer text-destructive focus:text-destructive">
                            <LogOut className="mr-2 h-4 w-4" />
                            <span>Log out</span>
                        </DropdownMenuItem>
                    </DropdownMenuContent>
                </DropdownMenu>
            </div>
        </header>
    );
}
