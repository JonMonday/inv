'use client';

import React from 'react';
import { useRouter } from 'next/navigation';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useCreateCategory, useCategories } from '@/hooks/useInventory';
import { useToast } from '@/hooks/use-toast';
import { ChevronLeft, FolderPlus, Loader2 } from 'lucide-react';
import Link from 'next/link';

const categorySchema = z.object({
    name: z.string().min(2, 'Name must be at least 2 characters'),
    parentCategoryId: z.string().optional().nullable(),
});

type CategoryFormValues = z.infer<typeof categorySchema>;

export default function CategoryAddPage() {
    const router = useRouter();
    const { toast } = useToast();
    const createCategory = useCreateCategory();
    const { data: categoriesResponse, isLoading: loadingCategories } = useCategories();
    const categories = Array.isArray(categoriesResponse?.data) ? categoriesResponse.data : [];

    const { register, handleSubmit, setValue, formState: { errors, isSubmitting } } = useForm<CategoryFormValues>({
        resolver: zodResolver(categorySchema),
        defaultValues: {
            name: '',
            parentCategoryId: null
        }
    });

    const onSubmit = async (values: CategoryFormValues) => {
        try {
            await createCategory.mutateAsync({
                name: values.name,
                parentCategoryId: values.parentCategoryId ? Number(values.parentCategoryId) : null
            });
            toast({
                title: "Category created",
                description: "The category has been successfully created."
            });
            router.push('/reference/categories');
        } catch (error: any) {
            toast({
                title: "Error",
                description: error.response?.data?.message || 'Failed to create category',
                variant: "destructive"
            });
        }
    };

    return (
        <div className="max-w-2xl mx-auto space-y-6 py-6">
            <div className="flex items-center gap-4">
                <Button variant="ghost" size="icon" asChild>
                    <Link href="/reference/categories">
                        <ChevronLeft className="h-4 w-4" />
                    </Link>
                </Button>
                <div>
                    <h1 className="text-2xl font-bold tracking-tight">Add New Category</h1>
                    <p className="text-sm text-muted-foreground">Create a new product category to organize your inventory</p>
                </div>
            </div>

            <Card>
                <CardHeader>
                    <CardTitle className="text-lg">Category Details</CardTitle>
                    <CardDescription>Enter the basic information for the new category.</CardDescription>
                </CardHeader>
                <CardContent>
                    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
                        <div className="space-y-2">
                            <Label htmlFor="name">Category Name</Label>
                            <Input
                                id="name"
                                placeholder="e.g. Office Supplies, Hardware..."
                                {...register('name')}
                                className={errors.name ? 'border-destructive' : ''}
                            />
                            {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="parentCategoryId">Parent Category (Optional)</Label>
                            <Select onValueChange={(val) => setValue('parentCategoryId', val === 'none' ? null : val)}>
                                <SelectTrigger>
                                    <SelectValue placeholder="Select a parent category" />
                                </SelectTrigger>
                                <SelectContent>
                                    <SelectItem value="none">None (Root Category)</SelectItem>
                                    {categories.map((cat: any) => (
                                        <SelectItem key={cat.id} value={cat.id.toString()}>
                                            {cat.name}
                                        </SelectItem>
                                    ))}
                                </SelectContent>
                            </Select>
                        </div>

                        <div className="flex justify-end gap-3 pt-4 border-t">
                            <Button variant="outline" type="button" onClick={() => router.back()} disabled={isSubmitting}>
                                Cancel
                            </Button>
                            <Button type="submit" disabled={isSubmitting} className="gap-2">
                                {isSubmitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <FolderPlus className="h-4 w-4" />}
                                Create Category
                            </Button>
                        </div>
                    </form>
                </CardContent>
            </Card>
        </div>
    );
}
