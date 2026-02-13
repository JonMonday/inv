'use client';

import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useForm, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import apiClient from '@/lib/api/client';
import { useWarehouses, useMovementTypes, usePostMovement } from '@/hooks/useInventory';
import { useAuthStore } from '@/store/authStore';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue
} from '@/components/ui/select';
import { useToast } from '@/hooks/use-toast';
import {
    Trash2,
    Search,
    Loader2,
    Plus,
    ArrowLeft,
    Package,
    ChevronRight,
    Save,
    History
} from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';

const lineSchema = z.object({
    productId: z.number().min(1, 'Product is required'),
    productName: z.string(),
    qtyDeltaOnHand: z.number().refine(val => val !== 0, 'Quantity cannot be zero'),
    qtyDeltaReserved: z.number().default(0),
    unitCost: z.number().min(0).optional(),
    lineNotes: z.string().optional(),
});

const movementSchema = z.object({
    warehouseId: z.number().min(1, 'Warehouse is required'),
    movementTypeCode: z.string().min(1, 'Movement type is required'),
    notes: z.string().min(1, 'Notes are required for manual movements'),
    lines: z.array(lineSchema).min(1, 'At least one item is required'),
});

type MovementFormValues = {
    warehouseId: number;
    movementTypeCode: string;
    notes: string;
    lines: {
        productId: number;
        productName: string;
        qtyDeltaOnHand: number;
        qtyDeltaReserved: number;
        unitCost?: number;
        lineNotes?: string;
    }[];
};

export default function ManualMovementPage() {
    const router = useRouter();
    const { toast } = useToast();
    const { user } = useAuthStore();
    const { data: warehouses } = useWarehouses();
    const { data: movementTypes } = useMovementTypes();
    const postMovement = usePostMovement();

    const [productSearch, setProductSearch] = useState('');
    const [searchResults, setSearchResults] = useState<any[]>([]);
    const [isSearching, setIsSearching] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);

    const form = useForm<MovementFormValues>({
        resolver: zodResolver(movementSchema) as any,
        defaultValues: {
            warehouseId: 0,
            movementTypeCode: '',
            notes: '',
            lines: [],
        },
    });

    const { fields, append, remove } = useFieldArray({
        control: form.control,
        name: 'lines',
    });

    const selectedMovementType = form.watch('movementTypeCode');
    const isReceipt = selectedMovementType === 'RECEIPT';

    // Auto-set first warehouse and movement type when they load
    useEffect(() => {
        if (warehouses?.data && (warehouses.data as any[]).length && form.getValues('warehouseId') === 0) {
            form.setValue('warehouseId', Number((warehouses.data as any[])[0].id));
        }
    }, [warehouses, form]);

    useEffect(() => {
        if (productSearch.length < 2) {
            setSearchResults([]);
            return;
        }
        const delayDebounceFn = setTimeout(async () => {
            setIsSearching(true);
            try {
                const response = await apiClient.get(`/api/reference/products?searchTerm=${productSearch}`);
                setSearchResults(response.data.data);
            } finally {
                setIsSearching(false);
            }
        }, 300);

        return () => clearTimeout(delayDebounceFn);
    }, [productSearch]);

    const addProduct = (product: any) => {
        if (fields.some(f => f.productId === product.id)) {
            toast({ variant: 'destructive', title: 'Product already added' });
            return;
        }
        append({
            productId: product.id,
            productName: product.name,
            qtyDeltaOnHand: 1,
            qtyDeltaReserved: 0,
            unitCost: 0,
            lineNotes: '',
        });
        setProductSearch('');
        setSearchResults([]);
    };

    const onSubmit = async (values: MovementFormValues) => {
        setIsSubmitting(true);
        try {
            const payload = {
                ...values,
                userId: user?.userId,
                lines: values.lines.map(l => {
                    let onHand = l.qtyDeltaOnHand;
                    if (values.movementTypeCode.endsWith('_OUT') || values.movementTypeCode === 'ISSUE') {
                        onHand = -Math.abs(onHand);
                    } else if (values.movementTypeCode.endsWith('_IN') || values.movementTypeCode === 'RECEIPT') {
                        onHand = Math.abs(onHand);
                    }
                    return { ...l, qtyDeltaOnHand: onHand };
                })
            };

            await postMovement.mutateAsync(payload);
            toast({ title: 'Success', description: 'Stock movement posted successfully' });
            router.push('/inventory/movements');
        } catch (error: any) {
            toast({
                variant: 'destructive',
                title: 'Error',
                description: error.response?.data?.message || 'Failed to post movement'
            });
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <div className="max-w-5xl mx-auto space-y-6 pb-20 animate-in fade-in duration-500">
            <div className="flex items-center justify-between">
                <div className="flex items-center gap-4">
                    <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 rounded-md bg-muted"
                        onClick={() => router.back()}
                    >
                        <ArrowLeft className="h-4 w-4" />
                    </Button>
                    <div>
                        <h1 className="text-xl font-bold tracking-tight">Manual Stock Adjustment</h1>
                        <p className="text-xs text-muted-foreground">Directly modify stock levels for receipts or corrections.</p>
                    </div>
                </div>
                <div className="flex items-center gap-2">
                    <History className="h-4 w-4 text-muted-foreground" />
                    <span className="text-xs text-muted-foreground font-medium">Audit Trail will be created</span>
                </div>
            </div>

            <Form {...form}>
                <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                        <Card className="md:col-span-2">
                            <CardHeader className="py-4 border-b">
                                <CardTitle className="text-sm font-bold uppercase tracking-tight">Transaction Header</CardTitle>
                            </CardHeader>
                            <CardContent className="pt-6 space-y-6">
                                <div className="grid md:grid-cols-2 gap-4">
                                    <FormField
                                        control={form.control}
                                        name="warehouseId"
                                        render={({ field }) => (
                                            <FormItem>
                                                <FormLabel className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground">Warehouse</FormLabel>
                                                <Select
                                                    onValueChange={(v) => field.onChange(Number(v))}
                                                    value={field.value?.toString()}
                                                >
                                                    <FormControl>
                                                        <SelectTrigger>
                                                            <SelectValue placeholder="Select warehouse..." />
                                                        </SelectTrigger>
                                                    </FormControl>
                                                    <SelectContent>
                                                        {(warehouses?.data as any[])?.map((w: any) => (
                                                            <SelectItem key={w.id} value={w.id.toString()}>{w.name}</SelectItem>
                                                        ))}
                                                    </SelectContent>
                                                </Select>
                                                <FormMessage />
                                            </FormItem>
                                        )}
                                    />

                                    <FormField
                                        control={form.control}
                                        name="movementTypeCode"
                                        render={({ field }) => (
                                            <FormItem>
                                                <FormLabel className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground">Movement Type</FormLabel>
                                                <Select onValueChange={field.onChange} value={field.value}>
                                                    <FormControl>
                                                        <SelectTrigger>
                                                            <SelectValue placeholder="Select type..." />
                                                        </SelectTrigger>
                                                    </FormControl>
                                                    <SelectContent>
                                                        {(movementTypes?.data as any[])?.filter((mt: any) => !['RESERVE', 'RELEASE', 'CONSUME_RESERVE'].includes(mt.code)).map((mt: any) => (
                                                            <SelectItem key={mt.code} value={mt.code}>{mt.name}</SelectItem>
                                                        ))}
                                                    </SelectContent>
                                                </Select>
                                                <FormMessage />
                                            </FormItem>
                                        )}
                                    />
                                </div>

                                <FormField
                                    control={form.control}
                                    name="notes"
                                    render={({ field }) => (
                                        <FormItem>
                                            <FormLabel className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground">Reference / Notes</FormLabel>
                                            <FormControl>
                                                <Textarea
                                                    placeholder="Reason for adjustment, PO number, or reference..."
                                                    className="min-h-[80px] resize-none"
                                                    {...field}
                                                />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    )}
                                />
                            </CardContent>
                        </Card>

                        <Card>
                            <CardHeader className="py-4 border-b">
                                <CardTitle className="text-sm font-bold uppercase tracking-tight">Summary</CardTitle>
                            </CardHeader>
                            <CardContent className="pt-6 space-y-4">
                                <div className="space-y-2">
                                    <div className="flex justify-between text-xs">
                                        <span className="text-muted-foreground">Operator</span>
                                        <span className="font-semibold">{user?.displayName}</span>
                                    </div>
                                    <div className="flex justify-between text-xs">
                                        <span className="text-muted-foreground">Date</span>
                                        <span className="font-semibold">{new Date().toLocaleDateString()}</span>
                                    </div>
                                    <div className="flex justify-between text-xs border-t pt-2">
                                        <span className="text-muted-foreground">Total Lines</span>
                                        <span className="font-bold underline">{fields.length}</span>
                                    </div>
                                </div>
                                <Button
                                    type="submit"
                                    className="w-full font-bold h-11"
                                    disabled={isSubmitting || fields.length === 0}
                                >
                                    {isSubmitting ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Save className="mr-2 h-4 w-4" />}
                                    Post Transaction
                                </Button>
                            </CardContent>
                        </Card>
                    </div>

                    <Card>
                        <CardHeader className="py-4 border-b flex flex-row items-center justify-between space-y-0">
                            <CardTitle className="text-sm font-bold uppercase tracking-tight">Movement Lines</CardTitle>
                            <div className="relative w-72">
                                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                                <Input
                                    placeholder="Add product..."
                                    className="pl-9 h-9 text-xs"
                                    value={productSearch}
                                    onChange={(e) => setProductSearch(e.target.value)}
                                />
                                {productSearch && (
                                    <div className="absolute z-50 w-64 mt-2 bg-popover border rounded-md shadow-lg overflow-hidden">
                                        {isSearching ? (
                                            <div className="p-4 text-center text-xs text-muted-foreground flex items-center justify-center gap-2">
                                                <Loader2 className="h-3 w-3 animate-spin" />
                                                Searching...
                                            </div>
                                        ) : searchResults.length > 0 ? (
                                            <div className="max-h-60 overflow-y-auto divide-y">
                                                {searchResults.map(p => (
                                                    <button
                                                        key={p.id}
                                                        type="button"
                                                        className="w-full px-4 py-2 text-left text-xs hover:bg-accent transition-colors flex flex-col"
                                                        onClick={() => addProduct(p)}
                                                    >
                                                        <span className="font-bold">{p.name}</span>
                                                        <span className="text-[10px] text-muted-foreground font-mono">{p.code}</span>
                                                    </button>
                                                ))}
                                            </div>
                                        ) : (
                                            <div className="p-4 text-center text-xs text-muted-foreground">No products found</div>
                                        )}
                                    </div>
                                )}
                            </div>
                        </CardHeader>
                        <CardContent className="p-0">
                            {fields.length > 0 ? (
                                <table className="w-full">
                                    <thead>
                                        <tr className="bg-muted/30 border-b">
                                            <th className="px-4 py-3 text-left text-[10px] font-bold uppercase tracking-widest text-muted-foreground">Product</th>
                                            <th className="px-4 py-3 text-center text-[10px] font-bold uppercase tracking-widest text-muted-foreground w-32">Quantity</th>
                                            {isReceipt && <th className="px-4 py-3 text-center text-[10px] font-bold uppercase tracking-widest text-muted-foreground w-32">Unit Cost</th>}
                                            <th className="px-4 py-3 text-left text-[10px] font-bold uppercase tracking-widest text-muted-foreground">Notes</th>
                                            <th className="px-4 py-3 w-12"></th>
                                        </tr>
                                    </thead>
                                    <tbody className="divide-y">
                                        {fields.map((field, index) => (
                                            <tr key={field.id} className="h-16 hover:bg-accent/5 transition-colors">
                                                <td className="px-4">
                                                    <div className="flex items-center gap-3">
                                                        <div className="p-2 rounded bg-muted">
                                                            <Package className="h-4 w-4 text-muted-foreground" />
                                                        </div>
                                                        <span className="text-sm font-semibold">{field.productName}</span>
                                                    </div>
                                                </td>
                                                <td className="px-4">
                                                    <Input
                                                        type="number"
                                                        step="0.01"
                                                        {...form.register(`lines.${index}.qtyDeltaOnHand`, { valueAsNumber: true })}
                                                        className="text-center font-bold h-9"
                                                    />
                                                </td>
                                                {isReceipt && (
                                                    <td className="px-4">
                                                        <Input
                                                            type="number"
                                                            step="0.01"
                                                            {...form.register(`lines.${index}.unitCost`, { valueAsNumber: true })}
                                                            className="text-center font-mono h-9"
                                                        />
                                                    </td>
                                                )}
                                                <td className="px-4">
                                                    <Input
                                                        placeholder="Line specific note..."
                                                        {...form.register(`lines.${index}.lineNotes`)}
                                                        className="h-9 text-xs"
                                                    />
                                                </td>
                                                <td className="px-4">
                                                    <Button
                                                        type="button"
                                                        variant="ghost"
                                                        size="icon"
                                                        className="h-8 w-8 text-destructive hover:bg-destructive/10"
                                                        onClick={() => remove(index)}
                                                    >
                                                        <Trash2 className="h-4 w-4" />
                                                    </Button>
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            ) : (
                                <div className="py-20 text-center space-y-3">
                                    <div className="mx-auto w-12 h-12 rounded-full bg-muted flex items-center justify-center">
                                        <Package className="h-6 w-6 text-muted-foreground" />
                                    </div>
                                    <div className="space-y-1">
                                        <p className="text-sm font-medium">No items added to this transaction</p>
                                        <p className="text-xs text-muted-foreground">Start typing in the search box above to add products.</p>
                                    </div>
                                </div>
                            )}
                        </CardContent>
                    </Card>
                </form>
            </Form>
        </div>
    );
}
