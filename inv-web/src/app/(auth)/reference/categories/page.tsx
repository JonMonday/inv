'use client';

import { useCategories } from '@/hooks/useInventory';
import { Button } from '@/components/ui/button';
import Link from 'next/link';
import {
    Plus,
    ExternalLink,
    Layers
} from 'lucide-react';
import { Skeleton } from '@/components/ui/skeleton';

interface Category {
    id: number;
    name: string;
}

export default function CategoriesPage() {
    const { data: categories, isLoading } = useCategories();

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight text-foreground">Categories</h1>
                    <p className="text-sm text-muted-foreground">Manage product classification and groupings.</p>
                </div>
                <Button size="sm" asChild>
                    <Link href="/reference/categories/add">
                        <Plus className="mr-2 h-4 w-4" /> Add Category
                    </Link>
                </Button>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {isLoading ? (
                    Array.from({ length: 6 }).map((_, i) => (
                        <div key={i} className="h-24 rounded-lg border bg-card p-4 animate-pulse">
                            <div className="flex justify-between items-start">
                                <Skeleton className="h-5 w-32" />
                                <Skeleton className="h-7 w-7 rounded-sm" />
                            </div>
                            <Skeleton className="h-4 w-20 mt-4" />
                        </div>
                    ))
                ) : (categories?.data?.length === 0 ? (
                    <div className="col-span-full h-32 flex items-center justify-center text-muted-foreground border border-dashed rounded-lg">
                        No categories found.
                    </div>
                ) : (
                    (categories?.data as Category[] | undefined)?.map((cat) => (
                        <div key={cat.id} className="group relative overflow-hidden rounded-lg border bg-card p-4 transition-all hover:shadow-md hover:border-primary/20">
                            <div className="flex items-start justify-between">
                                <div className="space-y-1">
                                    <h3 className="font-semibold text-foreground flex items-center gap-2">
                                        <Layers className="h-4 w-4 text-primary" />
                                        {cat.name}
                                    </h3>
                                    <p className="text-xs text-muted-foreground uppercase tracking-wider font-bold">
                                        ID: {cat.id}
                                    </p>
                                </div>
                                <Button variant="ghost" size="icon" className="h-8 w-8 scale-90 opacity-0 group-hover:opacity-100 transition-opacity">
                                    <ExternalLink className="h-4 w-4" />
                                </Button>
                            </div>
                        </div>
                    ))
                ))}
            </div>
        </div>
    );
}
