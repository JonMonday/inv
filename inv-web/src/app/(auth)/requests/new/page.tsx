'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useForm, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import apiClient from '@/lib/api/client';
import { useTemplates, useCreateRequest, useSubmitRequest } from '@/hooks/useRequests';
import { useAssignmentOptions, AssignmentOption } from '@/hooks/useAdmin';
import { useAuthStore } from '@/store/authStore';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
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
import { Trash2, Search, Loader2 } from 'lucide-react';
import { Badge } from '@/components/ui/badge';

interface Warehouse { id: number; name: string; }
interface Product { id: number; name: string; }
interface Department { id: number; name: string; }
interface RequestType { id: number; name: string; }
interface WorkflowTemplate { id: number; name: string; code: string; }

const lineSchema = z.object({
    productId: z.number().min(1, 'Product is required'),
    productName: z.string(),
    quantity: z.number().min(0.01, 'Qty must be > 0'),
});

const requestSchema = z.object({
    warehouseId: z.number().min(1, 'Warehouse is required'),
    departmentId: z.number().min(1, 'Department is required'),
    requestTypeId: z.number().min(1, 'Request Type is required'),
    workflowVersionId: z.number().min(1, 'Workflow Template is required'),
    notes: z.string().optional(),
    lines: z.array(lineSchema).min(1, 'At least one item is required'),
});

type RequestFormValues = z.infer<typeof requestSchema>;

export default function NewRequestPage() {
    const router = useRouter();
    const { toast } = useToast();
    const { data: templates } = useTemplates();
    const createMutation = useCreateRequest();
    const submitMutation = useSubmitRequest();

    const [productSearch, setProductSearch] = useState('');
    const [searchResults, setSearchResults] = useState<Product[]>([]);
    const [isSearching, setIsSearching] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [manualAssignments, setManualAssignments] = useState<Record<number, number[]>>({});

    const form = useForm<RequestFormValues>({
        resolver: zodResolver(requestSchema),
        defaultValues: {
            warehouseId: 1,
            departmentId: 1,
            requestTypeId: 1,
            notes: '',
            lines: [],
        },
    });

    const { user } = useAuthStore();
    const workflowVersionId = form.watch('workflowVersionId');
    const { data: assignmentOptions } = useAssignmentOptions(workflowVersionId);

    const { fields, append, remove } = useFieldArray({
        control: form.control,
        name: 'lines',
    });

    useEffect(() => {
        if (assignmentOptions && user) {
            const newAssignments = { ...manualAssignments };
            let changed = false;
            assignmentOptions.forEach(opt => {
                if (opt.modeCode === 'REQ' && !newAssignments[opt.workflowStepId]) {
                    newAssignments[opt.workflowStepId] = [user.userId];
                    changed = true;
                }
            });
            if (changed) setManualAssignments(newAssignments);
        }
    }, [assignmentOptions, user, manualAssignments]);

    useEffect(() => {
        // Fetch reference data (if needed in the future)
    }, []);

    useEffect(() => {
        if (productSearch.length < 2) {
            setSearchResults([]);
            return;
        }
        const delayDebounceFn = setTimeout(async () => {
            setIsSearching(true);
            try {
                const response = await apiClient.get(`/api/reference/products?search=${productSearch}`);
                setSearchResults(response.data.data);
            } finally {
                setIsSearching(false);
            }
        }, 300);

        return () => clearTimeout(delayDebounceFn);
    }, [productSearch]);

    const addProduct = (product: Product) => {
        if (fields.some(f => f.productId === product.id)) {
            toast({ variant: 'destructive', title: 'Product already added' });
            return;
        }
        append({
            productId: product.id,
            productName: product.name,
            quantity: 1,
        });
        setProductSearch('');
        setSearchResults([]);
    };

    const onSubmit = async (values: RequestFormValues, shouldSubmit: boolean = false) => {
        setIsSubmitting(true);
        try {
            const response = await createMutation.mutateAsync(values);
            const requestId = response.data;

            if (shouldSubmit) {
                const assignments = Object.entries(manualAssignments)
                    .filter(([, uids]) => uids.length > 0)
                    .map(([stepId, uids]) => ({
                        workflowStepId: Number(stepId),
                        userIds: uids
                    }));

                await submitMutation.mutateAsync({ requestId, assignments });
                toast({ title: 'Request submitted successfully' });
            } else {
                toast({ title: 'Draft created successfully' });
            }

            router.push(`/requests/${requestId}`);
        } catch (error: unknown) {
            const message = error instanceof Error ? error.message : 'Unknown error';
            toast({
                variant: 'destructive',
                title: 'Failed to create request',
                description: message
            });
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <div className="max-w-4xl mx-auto space-y-6">
            <div className="flex items-center gap-4">
                <Button variant="ghost" size="sm" onClick={() => router.back()}>Back</Button>
            </div>

            <Form {...form}>
                <form onSubmit={form.handleSubmit((v) => onSubmit(v, false))} className="space-y-6">
                    <Card>
                        <CardHeader>
                            <CardTitle>Request Header</CardTitle>
                            <CardDescription>Basic information about your inventory request</CardDescription>
                        </CardHeader>
                        <CardContent className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <FormField
                                control={form.control}
                                name="workflowVersionId"
                                render={({ field }) => (
                                    <FormItem className="md:col-span-2">
                                        <FormLabel>Workflow Template</FormLabel>
                                        <Select
                                            onValueChange={(v) => {
                                                form.setValue('workflowVersionId', Number(v));
                                                setManualAssignments({});
                                            }}
                                            value={workflowVersionId ? workflowVersionId.toString() : ""}
                                        >
                                            <FormControl>
                                                <SelectTrigger>
                                                    <SelectValue placeholder="Select process flow..." />
                                                </SelectTrigger>
                                            </FormControl>
                                            <SelectContent>
                                                {templates?.data?.map((t: any, idx: number) => (
                                                    <SelectItem key={t.versionId || idx} value={(t.versionId || '').toString()}>
                                                        {t.name} {t.isActive ? "(Active)" : ""}
                                                    </SelectItem>
                                                ))}
                                            </SelectContent>
                                        </Select>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                            <FormField
                                control={form.control}
                                name="notes"
                                render={({ field }) => (
                                    <FormItem className="md:col-span-2">
                                        <FormLabel>Notes</FormLabel>
                                        <FormControl>
                                            <Input placeholder="Internal notes or justification..." {...field} />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                        </CardContent>
                    </Card>

                    {assignmentOptions && assignmentOptions.length > 0 && (
                        <Card>
                            <CardHeader>
                                <CardTitle>Workflow Assignments</CardTitle>
                                <CardDescription>Select the approver/processor for each required step</CardDescription>
                            </CardHeader>
                            <CardContent className="space-y-4">
                                {assignmentOptions.map((opt: AssignmentOption) => (
                                    <div key={opt.workflowStepId} className="flex flex-col gap-2 p-3 border rounded-md">
                                        <div className="flex justify-between items-center">
                                            <div>
                                                <span className="text-sm font-semibold">{opt.name}</span>
                                                <div className="text-[10px] text-muted-foreground flex gap-2">
                                                    <span>{opt.assignmentMode}</span>
                                                    {(opt.roleName || opt.departmentName) && (
                                                        <span>• {opt.roleName} {opt.departmentName ? `/ ${opt.departmentName}` : ''}</span>
                                                    )}
                                                </div>
                                            </div>
                                            {opt.modeCode === 'REQ' ? (
                                                <div className="flex items-center gap-2 px-3 py-1.5 border rounded-md bg-primary/5 text-primary text-xs font-medium">
                                                    <span className="w-2 h-2 rounded-full bg-primary animate-pulse" />
                                                    {user?.displayName || 'Current User'} (Auto)
                                                </div>
                                            ) : opt.isManual ? (
                                                <Select
                                                    value={manualAssignments[opt.workflowStepId]?.[0]?.toString() || ""}
                                                    onValueChange={(v) => {
                                                        const uid = Number(v);
                                                        setManualAssignments({
                                                            ...manualAssignments,
                                                            [opt.workflowStepId]: [uid]
                                                        });
                                                    }}
                                                >
                                                    <SelectTrigger className="w-64 h-9">
                                                        <SelectValue placeholder="Select Assignee" />
                                                    </SelectTrigger>
                                                    <SelectContent>
                                                        {opt.eligibleUsers.map((u: { userId: number; description: string }) => (
                                                            <SelectItem key={u.userId} value={u.userId.toString()}>
                                                                {u.description}
                                                            </SelectItem>
                                                        ))}
                                                    </SelectContent>
                                                </Select>
                                            ) : (
                                                <div className="text-[10px] text-muted-foreground italic px-3 py-1.5 border rounded-md bg-muted/30">
                                                    System Auto-Assign
                                                </div>
                                            )}
                                        </div>
                                    </div>
                                ))}
                            </CardContent>
                        </Card>
                    )}


                    <Card>
                        <CardHeader className="flex flex-row items-center justify-between">
                            <div>
                                <CardTitle>Items</CardTitle>
                                <CardDescription>Search and add products to your request</CardDescription>
                            </div>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="relative">
                                <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                                <Input
                                    placeholder="Search products by name..."
                                    className="pl-9"
                                    value={productSearch}
                                    onChange={(e) => setProductSearch(e.target.value)}
                                />
                                {searchResults.length > 0 && (
                                    <div className="absolute z-10 w-full mt-1 border rounded-md bg-popover shadow-lg overflow-hidden">
                                        {searchResults.map(p => (
                                            <button
                                                key={p.id}
                                                type="button"
                                                className="w-full px-4 py-2 text-left text-sm hover:bg-accent transition-colors flex justify-between items-center"
                                                onClick={() => addProduct(p)}
                                            >
                                                <span>{p.name}</span>
                                                <Badge variant="outline" className="text-[10px]">Add</Badge>
                                            </button>
                                        ))}
                                    </div>
                                )}
                                {isSearching && (
                                    <div className="absolute right-3 top-2.5">
                                        <Loader2 className="h-4 w-4 animate-spin text-muted-foreground" />
                                    </div>
                                )}
                            </div>

                            <div className="rounded-md border">
                                <table className="w-full text-sm">
                                    <thead>
                                        <tr className="border-b bg-muted/50">
                                            <th className="p-3 text-left font-medium">Product</th>
                                            <th className="p-3 text-left font-medium w-32">Qty</th>
                                            <th className="p-3 text-right font-medium w-16"></th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {fields.length === 0 ? (
                                            <tr>
                                                <td colSpan={3} className="p-8 text-center text-muted-foreground">
                                                    No items added yet
                                                </td>
                                            </tr>
                                        ) : (
                                            fields.map((field, index) => (
                                                <tr key={field.id} className="border-b last:border-0">
                                                    <td className="p-3">
                                                        <span className="font-medium">{field.productName}</span>
                                                    </td>
                                                    <td className="p-3">
                                                        <Input
                                                            type="number"
                                                            {...form.register(`lines.${index}.quantity` as const, { valueAsNumber: true })}
                                                            className="w-20 text-center"
                                                        />
                                                    </td>
                                                    <td className="p-3 text-right">
                                                        <Button
                                                            type="button"
                                                            variant="ghost"
                                                            size="icon"
                                                            className="h-8 w-8 text-destructive"
                                                            onClick={() => remove(index)}
                                                        >
                                                            <Trash2 className="h-4 w-4" />
                                                        </Button>
                                                    </td>
                                                </tr>
                                            ))
                                        )}
                                    </tbody>
                                </table>
                            </div>
                            {form.formState.errors.lines && (
                                <p className="text-sm font-medium text-destructive">{form.formState.errors.lines.message}</p>
                            )}
                        </CardContent>
                    </Card>

                    <div className="flex justify-end gap-3">
                        <Button type="button" variant="outline" onClick={() => router.back()} disabled={isSubmitting}>Cancel</Button>
                        <Button
                            type="button"
                            variant="secondary"
                            disabled={isSubmitting}
                            onClick={form.handleSubmit((v) => onSubmit(v, false))}
                        >
                            {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                            Save Draft
                        </Button>
                        <Button
                            type="button"
                            disabled={isSubmitting}
                            onClick={form.handleSubmit((v) => onSubmit(v, true))}
                        >
                            {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                            Create & Submit
                        </Button>
                    </div>
                </form>
            </Form>
        </div>
    );
}
