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
import { useCreateWarehouse } from '@/hooks/useInventory';
import { useToast } from '@/hooks/use-toast';
import { ChevronLeft, Warehouse, Loader2 } from 'lucide-react';
import Link from 'next/link';

const warehouseSchema = z.object({
    name: z.string().min(2, 'Name must be at least 2 characters').max(100),
    location: z.string().max(200).optional().nullable(),
});

type WarehouseFormValues = z.infer<typeof warehouseSchema>;

export default function WarehouseAddPage() {
    const router = useRouter();
    const { toast } = useToast();
    const createWarehouse = useCreateWarehouse();

    const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<WarehouseFormValues>({
        resolver: zodResolver(warehouseSchema),
        defaultValues: {
            name: '',
            location: ''
        }
    });

    const onSubmit = async (values: WarehouseFormValues) => {
        try {
            await createWarehouse.mutateAsync({
                name: values.name,
                location: values.location
            });
            toast({
                title: "Warehouse created",
                description: "The warehouse has been successfully created."
            });
            router.push('/reference/warehouses');
        } catch (error: any) {
            toast({
                title: "Error",
                description: error.response?.data?.message || 'Failed to create warehouse',
                variant: "destructive"
            });
        }
    };

    return (
        <div className="max-w-2xl mx-auto space-y-6 py-6">
            <div className="flex items-center gap-4">
                <Button variant="ghost" size="icon" asChild>
                    <Link href="/reference/warehouses">
                        <ChevronLeft className="h-4 w-4" />
                    </Link>
                </Button>
                <div>
                    <h1 className="text-2xl font-bold tracking-tight">Add New Warehouse</h1>
                    <p className="text-sm text-muted-foreground">Register a new physical storage location</p>
                </div>
            </div>

            <Card>
                <CardHeader>
                    <CardTitle className="text-lg">Warehouse Details</CardTitle>
                    <CardDescription>Enter the name and location for the new warehouse site.</CardDescription>
                </CardHeader>
                <CardContent>
                    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
                        <div className="space-y-2">
                            <Label htmlFor="name">Warehouse Name</Label>
                            <Input
                                id="name"
                                placeholder="e.g. East Wing Store, Southern Depot..."
                                {...register('name')}
                                className={errors.name ? 'border-destructive' : ''}
                            />
                            {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="location">Physical Location / Address</Label>
                            <Input
                                id="location"
                                placeholder="e.g. Tema Port, Block C-1..."
                                {...register('location')}
                            />
                        </div>

                        <div className="flex justify-end gap-3 pt-4 border-t">
                            <Button variant="outline" type="button" onClick={() => router.back()} disabled={isSubmitting}>
                                Cancel
                            </Button>
                            <Button type="submit" disabled={isSubmitting} className="gap-2">
                                {isSubmitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Warehouse className="h-4 w-4" />}
                                Create Warehouse
                            </Button>
                        </div>
                    </form>
                </CardContent>
            </Card>
        </div>
    );
}
