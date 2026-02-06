'use client';

import { useState, useEffect, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import { useForm, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import apiClient from '@/lib/api/client';
import { useTemplates, useCreateRequest, useSubmitRequest } from '@/hooks/useRequests';
import { useAssignmentOptions, useDepartments } from '@/hooks/useAdmin';
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
import { Trash2, Search, Loader2, AlertCircle, FilePlus2, ChevronRight, CheckCircle2, ArrowLeft, ClipboardList, Package, FileText, Banknote, UserCheck, ShieldCheck, Workflow, Plus, Calendar } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { WorkflowAssignmentsCard } from '@/components/requests/WorkflowAssignmentsCard';
import { Separator } from '@/components/ui/separator';

type Phase = 'selection' | 'form' | 'success';

interface Product { id: number; name: string; code: string; }

const lineSchema = z.object({
    productId: z.number().min(1, 'Product is required'),
    productName: z.string(),
    quantity: z.number().min(0.01, 'Qty must be > 0'),
});

const requestSchema = z.object({
    warehouseId: z.number().min(1, 'Warehouse is required'),
    departmentId: z.number().min(1, 'Department is required'),
    requestTypeId: z.number().min(1, 'Request Type is required'),
    workflowTemplateId: z.number().min(1, 'Workflow Template is required'),
    notes: z.string().max(500, 'Justification cannot exceed 500 characters').optional(),
    lines: z.array(lineSchema).min(1, 'At least one item is required'),
});

type RequestFormValues = z.infer<typeof requestSchema>;

export default function NewRequestPage() {
    const router = useRouter();
    const { toast } = useToast();
    const { data: templates } = useTemplates();
    const createMutation = useCreateRequest();
    const submitMutation = useSubmitRequest();
    const { data: departments } = useDepartments();
    const { user } = useAuthStore();

    const [phase, setPhase] = useState<Phase>('selection');
    const [templateSearch, setTemplateSearch] = useState('');
    const [selectedTemplate, setSelectedTemplate] = useState<any>(null);
    const [submittedRequestId, setSubmittedRequestId] = useState<number | null>(null);

    const [productSearch, setProductSearch] = useState('');
    const [searchResults, setSearchResults] = useState<Product[]>([]);
    const [isSearching, setIsSearching] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);

    // Manual Assignments State: stepId -> userIds[]
    const [manualAssignments, setManualAssignments] = useState<Record<number, number[]>>({});

    const form = useForm<RequestFormValues>({
        resolver: zodResolver(requestSchema),
        defaultValues: {
            warehouseId: 1,
            departmentId: user?.primaryDepartmentId || 1,
            requestTypeId: 1,
            notes: '',
            lines: [],
        },
    });

    const workflowTemplateId = form.watch('workflowTemplateId');
    const { data: assignmentOptions, isLoading: isLoadingAssignments } = useAssignmentOptions(workflowTemplateId || null);

    const { fields, append, remove } = useFieldArray({
        control: form.control,
        name: 'lines',
    });

    // Auto-initialize assignments when options load
    useEffect(() => {
        if (assignmentOptions) {
            const initialAssignments: Record<number, number[]> = {};
            assignmentOptions.forEach(opt => {
                if (opt.modeCode === 'REQ' && user) {
                    initialAssignments[opt.workflowStepId] = [user.userId];
                }
            });
            setManualAssignments(initialAssignments);
        }
    }, [assignmentOptions, user]);

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

    const handleAssignmentChange = (stepId: number, userIds: number[]) => {
        setManualAssignments(prev => ({
            ...prev,
            [stepId]: userIds
        }));
    };

    const validateAssignments = () => {
        if (!assignmentOptions) return true;

        for (const opt of assignmentOptions) {
            if (opt.isManual && opt.modeCode !== 'REQ') {
                const selected = manualAssignments[opt.workflowStepId];
                if (!selected || selected.length === 0) {
                    toast({
                        variant: 'destructive',
                        title: 'Missing Assignment',
                        description: `Please select an assignee for step: ${opt.name}`
                    });
                    return false;
                }
            }
        }
        return true;
    };

    const onSubmit = async (values: RequestFormValues, shouldSubmit: boolean = false) => {
        if (shouldSubmit && !validateAssignments()) return;

        setIsSubmitting(true);
        try {
            // Ensure template ID is set if it was selected in Phase 1
            const finalValues = { ...values, workflowTemplateId: selectedTemplate.workflowTemplateId };

            const response = await createMutation.mutateAsync(finalValues);
            const requestId = response.data;

            if (shouldSubmit) {
                const assignmentsPayload = Object.entries(manualAssignments)
                    .filter(([stepId, uids]) => {
                        // Backend rejects manual assignment for 'Requestor' steps (AllowRequesterSelect=false)
                        // So we filter them out. The backend handles REQ assignment automatically.
                        const stepOption = assignmentOptions?.find(opt => opt.workflowStepId === Number(stepId));
                        return uids.length > 0 && stepOption?.modeCode !== 'REQ';
                    })
                    .map(([stepId, uids]) => ({
                        workflowStepId: Number(stepId),
                        userIds: uids
                    }));

                await submitMutation.mutateAsync({ requestId, assignments: assignmentsPayload });
                setSubmittedRequestId(requestId);
                setPhase('success');
                toast({ title: 'Request submitted successfully' });
            } else {
                toast({ title: 'Draft created successfully' });
                router.push('/requests/drafts');
            }
        } catch (error: any) {
            const message = error.response?.data?.message || error.message || 'Unknown error';
            toast({
                variant: 'destructive',
                title: 'Operation Failed',
                description: message
            });
        } finally {
            setIsSubmitting(false);
        }
    };

    const nextStep = useMemo(() => {
        if (!assignmentOptions || !manualAssignments) return null;
        // Find the first step that is not current user (REQ) or is manual
        const next = assignmentOptions.find(opt => opt.modeCode !== 'REQ');
        if (!next) return null;

        const assignedUserIds = manualAssignments[next.workflowStepId] || [];
        const assignedUserNames = next.eligibleUsers
            .filter(u => assignedUserIds.includes(u.userId))
            .map(u => u.displayName)
            .join(', ');

        return {
            name: next.name,
            assignees: assignedUserNames || '(system assigned)'
        };
    }, [assignmentOptions, manualAssignments]);

    if (phase === 'selection') {
        const filteredTemplates = templates?.data?.filter((t: any) =>
            t.name.toLowerCase().includes(templateSearch.toLowerCase())
        ) || [];

        const getIcon = (name: string) => {
            const n = name.toLowerCase();
            if (n.includes('admin')) return ClipboardList;
            if (n.includes('car') || n.includes('vehicle')) return Package;
            if (n.includes('cash') || n.includes('petty')) return Banknote;
            if (n.includes('leave')) return Calendar;
            if (n.includes('it') || n.includes('user')) return ShieldCheck;
            return Workflow;
        };

        return (
            <div className="max-w-5xl mx-auto space-y-6 animate-in fade-in duration-500">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight">New Request</h1>
                    <p className="text-sm text-muted-foreground">Select a process flow to initiate a new request.</p>
                </div>

                <div className="relative max-w-md">
                    <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                    <Input
                        placeholder="Find a process..."
                        className="pl-10"
                        value={templateSearch}
                        onChange={(e) => setTemplateSearch(e.target.value)}
                    />
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                    {filteredTemplates.map((t: any) => {
                        const Icon = getIcon(t.name);
                        return (
                            <Card
                                key={t.workflowTemplateId}
                                className="group relative cursor-pointer transition-all hover:bg-accent hover:text-accent-foreground"
                                onClick={() => {
                                    setSelectedTemplate(t);
                                    form.setValue('workflowTemplateId', t.workflowTemplateId);
                                    setPhase('form');
                                }}
                            >
                                <CardHeader className="flex flex-row items-center gap-4 space-y-0">
                                    <div className="p-2.5 rounded-lg bg-primary/10 text-primary group-hover:bg-primary group-hover:text-primary-foreground transition-colors">
                                        <Icon className="h-5 w-5" />
                                    </div>
                                    <div className="space-y-1">
                                        <CardTitle className="text-base font-bold tracking-tight">{t.name}</CardTitle>
                                        <CardDescription className="text-xs line-clamp-1">
                                            {t.description || "Start this workflow process..."}
                                        </CardDescription>
                                    </div>
                                    <ChevronRight className="ml-auto h-4 w-4 opacity-0 group-hover:opacity-100 transition-opacity" />
                                </CardHeader>
                            </Card>
                        );
                    })}
                </div>
            </div>
        );
    }

    if (phase === 'success') {
        return (
            <div className="max-w-xl mx-auto py-12 animate-in zoom-in-95 fade-in duration-500">
                <Card className="text-center">
                    <CardHeader className="pt-10">
                        <div className="mx-auto w-12 h-12 rounded-full bg-primary/10 flex items-center justify-center mb-4">
                            <CheckCircle2 className="h-6 w-6 text-primary" />
                        </div>
                        <CardTitle className="text-2xl font-bold">Request Submitted</CardTitle>
                        <CardDescription>
                            Case #REQ-{submittedRequestId} has been initiated successfully.
                        </CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-6">
                        {nextStep && (
                            <div className="p-4 rounded-lg bg-muted border text-left">
                                <p className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground mb-1">Next Action</p>
                                <p className="font-bold text-primary italic mb-1">{nextStep.name}</p>
                                <p className="text-xs text-muted-foreground">
                                    Assigned to <span className="font-semibold text-foreground">{nextStep.assignees}</span>
                                </p>
                            </div>
                        )}

                        <div className="grid grid-cols-1 gap-3">
                            <Button className="w-full" onClick={() => router.push(`/requests/${submittedRequestId}`)}>
                                View Request Details
                            </Button>
                            <Button variant="ghost" onClick={() => setPhase('selection')}>
                                Submit Another
                            </Button>
                            <Button variant="link" className="text-muted-foreground" onClick={() => router.push('/requests')}>
                                Go to All Requests
                            </Button>
                        </div>
                    </CardContent>
                </Card>
            </div>
        );
    }

    return (
        <div className="max-w-5xl mx-auto space-y-6 pb-20 animate-in fade-in duration-500">
            {/* Phase 2: Unified Form */}
            <div className="flex items-center justify-between">
                <div className="flex items-center gap-4">
                    <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 rounded-md bg-muted"
                        onClick={() => setPhase('selection')}
                    >
                        <ArrowLeft className="h-4 w-4" />
                    </Button>
                    <div>
                        <h1 className="text-xl font-bold tracking-tight">{selectedTemplate?.name}</h1>
                        <p className="text-xs text-muted-foreground flex items-center gap-1">
                            <Calendar className="h-3 w-3" />
                            {new Date().toLocaleDateString()}
                        </p>
                    </div>
                </div>
                {/* <Badge variant="outline" className="uppercase text-[10px] font-bold tracking-widest px-3 py-1">New Case</Badge> */}
            </div>

            {nextStep && (
                <div className="bg-primary/5 border border-primary/20 rounded-lg p-3 flex items-center justify-between animate-in slide-in-from-top-2">
                    <div className="flex items-center gap-3">
                        <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center">
                            <ArrowLeft className="h-4 w-4 text-primary rotate-180" />
                        </div>
                        <div>
                            <p className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground">Next Action Preview</p>
                            <p className="text-sm font-bold">{nextStep.name}</p>
                        </div>
                    </div>
                    <div className="text-right">
                        <p className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground">Assignee</p>
                        <p className="text-sm font-semibold">{nextStep.assignees}</p>
                    </div>
                </div>
            )}

            <Form {...form}>
                <form className="space-y-6">
                    <Card>
                        <CardHeader className="border-b py-4">
                            <CardTitle className="text-sm font-bold uppercase tracking-tight">Request Details</CardTitle>
                        </CardHeader>
                        <CardContent className="pt-6 grid md:grid-cols-2 gap-6">
                            <div className="space-y-2">
                                <label className="text-xs font-bold uppercase text-muted-foreground tracking-widest">Requesting User</label>
                                <div className="h-10 px-3 flex items-center bg-muted/30 border rounded-md font-medium text-sm">
                                    {user?.displayName || 'Unknown User'}
                                </div>
                                <p className="text-[10px] text-muted-foreground italic">Current process initiator</p>
                            </div>
                            <div className="space-y-2">
                                <label className="text-xs font-bold uppercase text-muted-foreground tracking-widest">Department</label>
                                <div className="h-10 px-3 flex items-center bg-muted/30 border rounded-md font-medium text-sm">
                                    {(() => {
                                        const dept = departments?.find((d: any) => d.departmentId === user?.primaryDepartmentId);
                                        return dept ? `${dept.name}` : user?.departments?.[0] || 'No Department Assigned';
                                    })()}
                                </div>
                                <p className="text-[10px] text-muted-foreground italic">Your assigned department</p>
                            </div>

                            <div className='md:col-span-2 '>
                                <FormField
                                    control={form.control}
                                    name="notes"
                                    render={({ field }) => (
                                        <FormItem>
                                            <div className="flex justify-between items-center">
                                                <FormLabel className="text-xs font-bold uppercase text-muted-foreground tracking-widest">Justification</FormLabel>
                                                <span className="text-[10px] text-muted-foreground tabular-nums">
                                                    {field.value?.length || 0}/500
                                                </span>
                                            </div>
                                            <FormControl>
                                                <Textarea
                                                    placeholder="Purpose of this request..."
                                                    className="min-h-[80px]"
                                                    {...field}
                                                    maxLength={500}
                                                />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    )}
                                />
                            </div>

                            <div className="md:col-span-2 space-y-4">
                                <div className="space-y-4">
                                    <div className="flex items-center justify-between border-b pb-2">
                                        <h3 className="text-xs font-bold uppercase tracking-widest text-primary">Items Selection</h3>
                                        <p className="text-[10px] text-muted-foreground italic">Add product items to your request</p>
                                    </div>

                                    <div className="relative">
                                        <div className="relative group">
                                            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground group-focus-within:text-primary transition-colors" />
                                            <Input
                                                placeholder="Search products by name or SKU..."
                                                className="h-10 pl-10 border-dashed hover:border-primary/50 focus:border-primary transition-all"
                                                value={productSearch}
                                                onChange={(e) => setProductSearch(e.target.value)}
                                            />
                                        </div>

                                        {productSearch && (
                                            <div className="absolute z-[100] w-full mt-1 border rounded-md bg-popover shadow-xl overflow-hidden animate-in fade-in slide-in-from-top-1">
                                                {searchResults.length > 0 ? (
                                                    <div className="max-h-[300px] overflow-y-auto divide-y">
                                                        {searchResults.map(p => (
                                                            <button
                                                                key={p.id}
                                                                type="button"
                                                                className="w-full px-4 py-3 text-left text-sm hover:bg-accent flex justify-between items-center group/item transition-colors"
                                                                onClick={() => addProduct(p)}
                                                            >
                                                                <div className="flex flex-col">
                                                                    <span className="font-bold text-foreground">{p.name}</span>
                                                                    <span className="text-[10px] text-muted-foreground font-mono">SKU: {p.code}</span>
                                                                </div>
                                                                <div className="flex items-center gap-2 opacity-0 group-hover/item:opacity-100 transition-opacity">
                                                                    <span className="text-[10px] font-bold uppercase text-primary">Add Item</span>
                                                                    <Plus className="h-4 w-4 text-primary" />
                                                                </div>
                                                            </button>
                                                        ))}
                                                    </div>
                                                ) : !isSearching && (
                                                    <div className="px-4 py-6 text-sm text-muted-foreground italic text-center flex flex-col items-center gap-2">
                                                        <Search className="h-5 w-5 opacity-20" />
                                                        No matching products found
                                                    </div>
                                                )}
                                                {isSearching && (
                                                    <div className="px-4 py-6 text-sm text-muted-foreground italic text-center flex items-center justify-center gap-2">
                                                        <Loader2 className="h-4 w-4 animate-spin" />
                                                        Searching...
                                                    </div>
                                                )}
                                            </div>
                                        )}
                                    </div>
                                </div>

                                {fields.length > 0 ? (
                                    <div className="rounded-md border overflow-hidden shadow-sm">
                                        <table className="w-full text-sm">
                                            <thead>
                                                <tr className="bg-muted/50 border-b">
                                                    <th className="p-3 text-[10px] font-bold uppercase tracking-widest text-muted-foreground w-12 text-center">#</th>
                                                    <th className="p-3 text-left text-[10px] font-bold uppercase tracking-widest text-muted-foreground">Description</th>
                                                    <th className="p-3 text-center text-[10px] font-bold uppercase tracking-widest text-muted-foreground w-32">Qty Required</th>
                                                    <th className="p-3 w-12"></th>
                                                </tr>
                                            </thead>
                                            <tbody className="divide-y">
                                                {fields.map((field, index) => (
                                                    <tr key={field.id} className="hover:bg-accent/5 transition-colors">
                                                        <td className="p-3 text-center text-xs text-muted-foreground font-mono">{index + 1}</td>
                                                        <td className="p-3">
                                                            <div className="flex flex-col">
                                                                <span className="font-bold text-foreground">{field.productName}</span>
                                                                {/* <span className="text-[10px] text-muted-foreground font-mono">ID: {field.productId}</span> */}
                                                            </div>
                                                        </td>
                                                        <td className="p-3 text-center">
                                                            <Input
                                                                type="number"
                                                                min="0.01"
                                                                step="0.01"
                                                                defaultValue={field.quantity}
                                                                onChange={(e) => {
                                                                    const val = parseFloat(e.target.value);
                                                                    form.setValue(`lines.${index}.quantity`, isNaN(val) ? 0 : val);
                                                                }}
                                                                className="h-9 w-24 mx-auto text-center font-bold bg-background"
                                                            />
                                                        </td>
                                                        <td className="p-3 text-right">
                                                            <Button
                                                                type="button"
                                                                variant="ghost"
                                                                size="icon"
                                                                className="h-8 w-8 text-destructive hover:bg-destructive/10 hover:text-destructive transition-all"
                                                                onClick={() => remove(index)}
                                                            >
                                                                <Trash2 className="h-4 w-4" />
                                                            </Button>
                                                        </td>
                                                    </tr>
                                                ))}
                                            </tbody>
                                        </table>
                                    </div>
                                ) : (
                                    <div className="rounded-lg border border-dashed p-10 text-center space-y-3 bg-muted/5">
                                        <div className="mx-auto w-10 h-10 rounded-full bg-muted flex items-center justify-center">
                                            <Package className="h-5 w-5 text-muted-foreground" />
                                        </div>
                                        <div className="space-y-1">
                                            <p className="text-sm font-medium">No items added yet</p>
                                            <p className="text-xs text-muted-foreground">Use the search bar above to find and add products.</p>
                                        </div>
                                    </div>
                                )}
                            </div>

                            <Separator className="md:col-span-2" />

                            <div className="md:col-span-2 space-y-6">
                                <div className="space-y-1">
                                    <h3 className="text-xs font-bold uppercase tracking-widest text-muted-foreground">Workflow Assignments</h3>
                                    <p className="text-[10px] text-muted-foreground italic">Select the users responsible for subsequent steps in this process.</p>
                                </div>
                                <div className="grid grid-cols-1 gap-3">
                                    {(assignmentOptions || []).map((opt) => {
                                        if (opt.modeCode === 'REQ') return null;
                                        return (
                                            <div key={opt.workflowStepId} className="flex flex-col md:flex-row md:items-center justify-between p-4 bg-muted/20 rounded-lg border border-muted-foreground/10 hover:border-muted-foreground/30 transition-all gap-4">
                                                <div className="space-y-1.5 flex-1">
                                                    <div className="flex items-center gap-2">
                                                        <p className="font-bold text-sm tracking-tight capitalize">{opt.name}</p>
                                                        {opt.isRequired && <Badge variant="destructive" className="h-[14px] text-[8px] px-1 uppercase leading-none">Required</Badge>}
                                                    </div>
                                                    <p className="text-xs text-muted-foreground italic">
                                                        {opt.roleName && opt.departmentName
                                                            ? `Select a user with Role: ${opt.roleName} and Department: ${opt.departmentName}`
                                                            : opt.roleName
                                                                ? `Select a user with Role: ${opt.roleName}`
                                                                : opt.departmentName
                                                                    ? `Select a user with Department: ${opt.departmentName}`
                                                                    : `Select an assignee (${opt.assignmentMode})`
                                                        }
                                                    </p>
                                                </div>
                                                <div className="w-full md:w-[320px]">
                                                    <Select
                                                        onValueChange={(v) => handleAssignmentChange(opt.workflowStepId, [Number(v)])}
                                                        defaultValue={manualAssignments[opt.workflowStepId]?.[0]?.toString()}
                                                    >
                                                        <SelectTrigger className="bg-background shadow-sm h-9">
                                                            <SelectValue placeholder={`Select assignee for ${opt.name}...`} />
                                                        </SelectTrigger>
                                                        <SelectContent>
                                                            {opt.eligibleUsers.map(u => (
                                                                <SelectItem key={u.userId} value={u.userId.toString()}>
                                                                    {u.displayName}
                                                                </SelectItem>
                                                            ))}
                                                        </SelectContent>
                                                    </Select>
                                                </div>
                                            </div>
                                        );
                                    })}
                                </div>
                            </div>
                        </CardContent>
                    </Card>

                    <div className="flex items-center justify-end gap-3 pt-4">
                        <Button
                            type="button"
                            variant="outline"
                            className="h-10 px-8 font-semibold"
                            disabled={isSubmitting}
                            onClick={form.handleSubmit((v) => onSubmit(v, false))}
                        >
                            Save as Draft
                        </Button>
                        <Button
                            type="button"
                            className="h-10 px-10 font-bold bg-zinc-900 text-white dark:bg-zinc-100 dark:text-zinc-900"
                            disabled={isSubmitting}
                            onClick={form.handleSubmit((v) => onSubmit(v, true))}
                        >
                            {isSubmitting ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : "Submit Request"}
                            <ChevronRight className="ml-2 h-4 w-4" />
                        </Button>
                    </div>
                </form>
            </Form>
        </div>
    );
}
