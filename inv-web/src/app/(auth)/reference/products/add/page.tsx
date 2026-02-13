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
import { useCreateProduct, useCategories, useUnitsOfMeasure } from '@/hooks/useInventory';
import { useToast } from '@/hooks/use-toast';
import { ChevronLeft, Box, Loader2 } from 'lucide-react';
import Link from 'next/link';

const productSchema = z.object({
    sku: z.string().min(3, 'SKU must be at least 3 characters').max(50),
    name: z.string().min(2, 'Name must be at least 2 characters').max(200),
    categoryId: z.string().min(1, 'Category is required'),
    unitOfMeasureId: z.string().min(1, 'Unit of Measure is required'),
    reorderLevel: z.string().min(1, 'Reorder level is required'),
});

type ProductFormValues = z.infer<typeof productSchema>;

export default function ProductAddPage() {
    const router = useRouter();
    const { toast } = useToast();
    const createProduct = useCreateProduct();
    const { data: categoriesResponse, isLoading: loadingCategories } = useCategories();
    const categories = Array.isArray(categoriesResponse?.data) ? categoriesResponse.data : [];

    const { data: uomResponse, isLoading: loadingUoms } = useUnitsOfMeasure();
    const unitsOfMeasure = Array.isArray(uomResponse?.data) ? uomResponse.data : [];

    const { register, handleSubmit, setValue, formState: { errors, isSubmitting } } = useForm<ProductFormValues>({
        resolver: zodResolver(productSchema),
        defaultValues: {
            sku: '',
            name: '',
            categoryId: '',
            unitOfMeasureId: '',
            reorderLevel: '0'
        }
    });

    const onSubmit = async (values: ProductFormValues) => {
        try {
            await createProduct.mutateAsync({
                sku: values.sku,
                name: values.name,
                categoryId: Number(values.categoryId),
                unitOfMeasureId: Number(values.unitOfMeasureId),
                reorderLevel: Number(values.reorderLevel)
            });
            toast({
                title: "Product created",
                description: "The product has been successfully created."
            });
            router.push('/reference/products');
        } catch (error: any) {
            toast({
                title: "Error",
                description: error.response?.data?.message || 'Failed to create product',
                variant: "destructive"
            });
        }
    };

    return (
        <div className="max-w-2xl mx-auto space-y-6 py-6">
            <div className="flex items-center gap-4">
                <Button variant="ghost" size="icon" asChild>
                    <Link href="/reference/products">
                        <ChevronLeft className="h-4 w-4" />
                    </Link>
                </Button>
                <div>
                    <h1 className="text-2xl font-bold tracking-tight">Add New Product</h1>
                    <p className="text-sm text-muted-foreground">Define a new item in your inventory catalog</p>
                </div>
            </div>

            <Card>
                <CardHeader>
                    <CardTitle className="text-lg">Product Information</CardTitle>
                    <CardDescription>Enter the specifications for the new inventory item.</CardDescription>
                </CardHeader>
                <CardContent>
                    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
                        <div className="grid grid-cols-2 gap-4">
                            <div className="space-y-2">
                                <Label htmlFor="sku">SKU / Item Code</Label>
                                <Input
                                    id="sku"
                                    placeholder="e.g. STN-001"
                                    {...register('sku')}
                                    className={errors.sku ? 'border-destructive' : ''}
                                />
                                {errors.sku && <p className="text-xs text-destructive">{errors.sku.message}</p>}
                            </div>

                            <div className="space-y-2">
                                <Label htmlFor="unitOfMeasureId">Unit of Measure</Label>
                                <Select onValueChange={(val) => setValue('unitOfMeasureId', val)}>
                                    <SelectTrigger id="unitOfMeasureId" className={errors.unitOfMeasureId ? 'border-destructive' : ''}>
                                        <SelectValue placeholder="Select unit" />
                                    </SelectTrigger>
                                    <SelectContent>
                                        {unitsOfMeasure.map((uom: any) => (
                                            <SelectItem key={uom.id} value={uom.id.toString()}>
                                                {uom.name} ({uom.code})
                                            </SelectItem>
                                        ))}
                                    </SelectContent>
                                </Select>
                                {errors.unitOfMeasureId && <p className="text-xs text-destructive">{errors.unitOfMeasureId.message}</p>}
                            </div>
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="name">Product Name</Label>
                            <Input
                                id="name"
                                placeholder="e.g. A4 Paper Ream (80gsm)"
                                {...register('name')}
                                className={errors.name ? 'border-destructive' : ''}
                            />
                            {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="categoryId">Category</Label>
                            <Select onValueChange={(val) => setValue('categoryId', val)}>
                                <SelectTrigger id="categoryId" className={errors.categoryId ? 'border-destructive' : ''}>
                                    <SelectValue placeholder="Select a category" />
                                </SelectTrigger>
                                <SelectContent>
                                    {categories.map((cat: any) => (
                                        <SelectItem key={cat.id} value={cat.id.toString()}>
                                            {cat.name}
                                        </SelectItem>
                                    ))}
                                </SelectContent>
                            </Select>
                            {errors.categoryId && <p className="text-xs text-destructive">{errors.categoryId.message}</p>}
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="reorderLevel">Low Stock Threshold (Reorder Level)</Label>
                            <Input
                                id="reorderLevel"
                                type="number"
                                step="0.01"
                                placeholder="0"
                                {...register('reorderLevel')}
                                className={errors.reorderLevel ? 'border-destructive' : ''}
                            />
                            <p className="text-xs text-muted-foreground">Receive alerts when stock falls below this quantity.</p>
                            {errors.reorderLevel && <p className="text-xs text-destructive">{errors.reorderLevel.message}</p>}
                        </div>

                        <div className="flex justify-end gap-3 pt-4 border-t">
                            <Button variant="outline" type="button" onClick={() => router.back()} disabled={isSubmitting}>
                                Cancel
                            </Button>
                            <Button type="submit" disabled={isSubmitting} className="gap-2">
                                {isSubmitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Box className="h-4 w-4" />}
                                Create Product
                            </Button>
                        </div>
                    </form>
                </CardContent>
            </Card>
        </div>
    );
}
