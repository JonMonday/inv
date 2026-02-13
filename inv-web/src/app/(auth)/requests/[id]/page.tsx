'use client';

import { useParams, useRouter } from 'next/navigation';
import { useAuthStore } from '@/store/authStore';
import { useTasks } from '@/hooks/useTasks';
import { useRequest, useRequestHistory, useUpdateFulfillmentWarehouse, useUpdateFulfillmentQuantities } from '@/hooks/useRequests';
import { useWarehouses, useStockLevels } from '@/hooks/useInventory';
import { useState, useEffect } from 'react';
import { useToast } from '@/hooks/use-toast';
import { cn } from '@/lib/utils';
import {
    Loader2,
    AlertCircle,
    ArrowLeft,
    Calendar,
    Building2,
    Warehouse,
    ClipboardList,
    CheckCircle2,
    UserCheck,
    ShieldCheck,
    Workflow,
    Package,
    User,
    GitBranch,
    Save
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import { Textarea } from '@/components/ui/textarea';
import { Input } from '@/components/ui/input';
import { WorkflowDiagram } from '@/components/requests/WorkflowDiagram';
import { TaskActionBar } from '@/components/workflow/TaskActionBar';

export default function RequestDetailPage() {
    const { id } = useParams();
    const router = useRouter();
    const { user } = useAuthStore();
    const { data: request, isLoading } = useRequest(Number(id));
    const { data: historyData, isLoading: historyLoading } = useRequestHistory(Number(id));
    const { tasksQuery } = useTasks(); // Fetch my tasks to see if I have one for this request

    const history = historyData?.history || [];
    const transitions = historyData?.transitions || [];
    const rawTasks = historyData?.tasks || [];
    const manualAssignments = historyData?.manualAssignments || [];

    const updateWH = useUpdateFulfillmentWarehouse();
    const updateQuantities = useUpdateFulfillmentQuantities();
    const warehousesQuery = useWarehouses();
    const { toast } = useToast();

    const [approvedQuantities, setApprovedQuantities] = useState<Record<number, number>>({});

    const stockQuery = useStockLevels(request?.warehouseId);
    const warehouseStock = stockQuery.data || [];

    useEffect(() => {
        if (request?.lines) {
            const initialQs: Record<number, number> = {};
            request.lines.forEach((l: any) => {
                initialQs[l.productId] = l.qtyApproved ?? l.qtyRequested;
            });
            setApprovedQuantities(initialQs);
        }
    }, [request?.lines]);

    const handleQuantityChange = (productId: number, val: string) => {
        const num = parseFloat(val);
        if (!isNaN(num)) {
            setApprovedQuantities(prev => ({ ...prev, [productId]: num }));
        }
    };

    const onSaveQuantities = () => {
        const payload = Object.entries(approvedQuantities).map(([productId, quantity]) => ({
            productId: Number(productId),
            quantity
        }));
        updateQuantities.mutate({ requestId: Number(id), quantities: payload }, {
            onSuccess: () => {
                toast({
                    title: "Success",
                    description: "Fulfillment quantities updated successfully.",
                });
            }
        });
    };

    const hasQuantityChanges = () => {
        if (!request?.lines) return false;
        return request.lines.some((l: any) =>
            approvedQuantities[l.productId] !== (l.qtyApproved ?? l.qtyRequested)
        );
    };

    // Find active task for this request assigned to me
    const myActiveTask = tasksQuery.data?.data?.find((t: any) =>
        t.requestId === Number(id) &&
        (t.status === 'PENDING' || t.status === 'CLAIMED')
    );

    const isFulfillmentStep = myActiveTask?.stepKey === 'FULFILL' ||
        myActiveTask?.stepKey === 'FULFILLMENT' ||
        myActiveTask?.stepName === 'Fulfillment';

    const canAction = !!myActiveTask;

    // Default Warehouse Selection logic
    useEffect(() => {
        const whs = warehousesQuery.data?.data;
        if (isFulfillmentStep && canAction && !request?.warehouseId && whs && whs.length > 0) {
            const firstWH = whs[0] as any;
            updateWH.mutate({ requestId: Number(id), warehouseId: firstWH.warehouseId });
        }
    }, [isFulfillmentStep, canAction, request?.warehouseId, warehousesQuery.data]);

    // Calculate rejection counts per step
    const getRejectionCount = (stepKey: string) => {
        if (!rawTasks) return 0;
        return rawTasks.filter((t: any) =>
            t.stepKey === stepKey &&
            t.actions?.some((a: any) =>
                a.actionType?.toLowerCase().includes('reject') ||
                a.actionType?.toLowerCase().includes('dispute')
            )
        ).length;
    };

    if (isLoading) return (
        <div className="flex flex-col items-center justify-center min-h-[400px] gap-4">
            <Loader2 className="h-8 w-8 animate-spin text-primary" />
            <p className="text-sm text-muted-foreground animate-pulse">Loading request details...</p>
        </div>
    );

    if (!request) return (
        <div className="max-w-md mx-auto py-20 text-center space-y-4">
            <div className="mx-auto w-12 h-12 rounded-full bg-destructive/10 flex items-center justify-center">
                <AlertCircle className="h-6 w-6 text-destructive" />
            </div>
            <h2 className="text-xl font-bold">Request Not Found</h2>
            <p className="text-sm text-muted-foreground">The request you are looking for (REQ-{id}) does not exist or has been removed.</p>
            <Button onClick={() => router.push('/requests')}>Back to List</Button>
        </div>
    );

    const isDraft = request.status?.code === 'DRAFT';
    const isCompleted = request.status?.code === 'COMPLETED';

    return (
        <div className="max-w-5xl mx-auto space-y-6 pb-20 animate-in fade-in duration-500">
            {/* Header Area */}
            <div className="flex items-center justify-between">
                <div className="flex items-center gap-4">
                    <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 rounded-md bg-muted"
                        onClick={() => router.push('/requests')}
                    >
                        <ArrowLeft className="h-4 w-4" />
                    </Button>
                    <div>
                        <div className="flex items-center gap-2">
                            <h1 className="text-xl font-bold tracking-tight font-mono">REQ-{id}</h1>
                            <Badge variant={isDraft ? 'outline' : (isCompleted ? 'default' : 'secondary')} className="uppercase text-[9px] font-bold tracking-widest px-2">
                                {request.status?.name || request.status?.code}
                            </Badge>
                        </div>
                        <p className="text-xs text-muted-foreground flex items-center gap-1">
                            <Calendar className="h-3 w-3" />
                            Initiated on {new Date(request.requestedAt).toLocaleDateString()}
                        </p>
                    </div>
                </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
                <div className="lg:col-span-3 space-y-6">

                    <Card>
                        <CardHeader className="border-b py-4">
                            <CardTitle className="text-sm font-bold uppercase tracking-tight">Request Details</CardTitle>
                        </CardHeader>
                        <CardContent className="pt-6 grid md:grid-cols-2 gap-6">
                            <div className="space-y-2">
                                <label className="text-xs font-bold uppercase text-muted-foreground tracking-widest">Department</label>
                                <div className="h-10 px-3 flex items-center bg-muted/10 border rounded-md font-medium text-sm">
                                    <Building2 className="mr-2 h-3.5 w-3.5 text-muted-foreground" />
                                    {request.department?.name || `Dept-${request.departmentId}`}
                                </div>
                            </div>
                            <div className="space-y-2">
                                <label className="text-xs font-bold uppercase text-muted-foreground tracking-widest">Warehouse</label>
                                {isFulfillmentStep && canAction ? (
                                    <Select
                                        disabled={updateWH.isPending}
                                        value={String(request.warehouseId)}
                                        onValueChange={(val) => updateWH.mutate({ requestId: Number(id), warehouseId: Number(val) })}
                                    >
                                        <SelectTrigger className="h-10 bg-primary/5 border-primary/20 hover:bg-primary/10 transition-colors">
                                            <div className="flex items-center font-medium text-sm">
                                                {updateWH.isPending ? (
                                                    <Loader2 className="mr-2 h-3.5 w-3.5 animate-spin text-primary" />
                                                ) : (
                                                    <Warehouse className="mr-2 h-3.5 w-3.5 text-primary" />
                                                )}
                                                <SelectValue />
                                            </div>
                                        </SelectTrigger>
                                        <SelectContent>
                                            {warehousesQuery.data?.data?.map((wh: any, idx: number) => (
                                                <SelectItem key={wh.warehouseId || idx} value={String(wh.warehouseId)}>
                                                    {wh.name}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                ) : (
                                    <div className="h-10 px-3 flex items-center bg-muted/10 border rounded-md font-medium text-sm">
                                        <Warehouse className="mr-2 h-3.5 w-3.5 text-muted-foreground" />
                                        {request.warehouse?.name || `WH-${request.warehouseId}`}
                                    </div>
                                )}
                            </div>

                            <div className="space-y-2">
                                <label className="text-xs font-bold uppercase text-muted-foreground tracking-widest">Requested By</label>
                                <div className="h-10 px-3 flex items-center bg-muted/10 border rounded-md font-medium text-sm">
                                    <User className="mr-2 h-3.5 w-3.5 text-muted-foreground" />
                                    {request.requestedByUser?.displayName || request.requestedByUser?.email || 'Unknown'}
                                    {/* <span className="ml-2 text-[10px] text-muted-foreground font-mono">#{request.requestedByUserId}</span> */}
                                </div>
                            </div>

                            <div className="space-y-2">
                                <label className="text-xs font-bold uppercase text-muted-foreground tracking-widest">Workflow Type</label>
                                <div className="h-10 px-3 flex items-center bg-muted/10 border rounded-md font-medium text-sm">
                                    <GitBranch className="mr-2 h-3.5 w-3.5 text-muted-foreground" />
                                    {request.workflowTemplate?.name || 'Standard Workflow'}
                                    {/* {request.workflowTemplateId && (
                                        <span className="ml-2 text-[10px] text-muted-foreground font-mono">#{request.workflowTemplateId}</span>
                                    )} */}
                                </div>
                            </div>

                            <div className="md:col-span-2 space-y-2">
                                <label className="text-xs font-bold uppercase text-muted-foreground tracking-widest">Justification</label>
                                <div className="p-4 bg-muted/5 border rounded-md text-sm leading-relaxed whitespace-pre-wrap">
                                    {request.notes || 'No notes provided for this request.'}
                                </div>
                            </div>

                            <div className="md:col-span-2 space-y-4">
                                <div className="flex items-center justify-between border-b pb-2 mt-4">
                                    <h3 className="text-xs font-bold uppercase tracking-widest text-primary">Requested Items</h3>
                                    <p className="text-[10px] text-muted-foreground italic">{request.lines?.length || 0} Products Included</p>
                                </div>

                                <div className="rounded-md border overflow-hidden shadow-sm">
                                    <Table>
                                        <TableHeader className="bg-muted/50 border-b">
                                            <TableRow>
                                                <TableHead className="w-12 text-center text-[10px] font-bold uppercase tracking-widest">#</TableHead>
                                                <TableHead className="text-[10px] font-bold uppercase tracking-widest">Description</TableHead>
                                                <TableHead className="text-center text-[10px] font-bold uppercase tracking-widest w-20">Stock</TableHead>
                                                <TableHead className="text-center text-[10px] font-bold uppercase tracking-widest w-24">Req</TableHead>
                                                <TableHead className="text-center text-[10px] font-bold uppercase tracking-widest w-24">Appr</TableHead>
                                                <TableHead className="text-right text-[10px] font-bold uppercase tracking-widest w-24 pr-6">Fulfilled</TableHead>
                                            </TableRow>
                                        </TableHeader>
                                        <TableBody className="divide-y">
                                            {request.lines.map((line: any, index: number) => (
                                                <TableRow key={line.requestLineId || index} className="hover:bg-accent/5 transition-colors h-14">
                                                    <td className="p-3 text-center text-xs text-muted-foreground font-mono">{index + 1}</td>
                                                    <TableCell>
                                                        <div className="flex flex-col">
                                                            <span className="font-bold text-foreground text-sm">{line.product?.name || `Prod-${line.productId}`}</span>
                                                            <span className="text-[10px] text-muted-foreground font-mono uppercase">{line.product?.sku || `SKU-${line.productId}`}</span>
                                                        </div>
                                                    </TableCell>
                                                    <TableCell className="text-center">
                                                        <Badge variant="outline" className="font-mono text-[10px]">
                                                            {warehouseStock.find((s: any) => s.productId === line.productId)?.availableQty ?? 0}
                                                        </Badge>
                                                    </TableCell>
                                                    <TableCell className="text-center font-mono font-bold text-sm bg-muted/5">{line.qtyRequested}</TableCell>
                                                    <TableCell className="text-center">
                                                        {isFulfillmentStep && canAction ? (
                                                            <Input
                                                                type="number"
                                                                className="h-8 w-20 mx-auto text-center font-mono text-sm border-primary/20 bg-primary/5 focus-visible:ring-primary"
                                                                value={approvedQuantities[line.productId] ?? line.qtyRequested}
                                                                onChange={(e) => handleQuantityChange(line.productId, e.target.value)}
                                                            />
                                                        ) : (
                                                            <span className="font-mono text-sm text-muted-foreground">{line.qtyApproved || '-'}</span>
                                                        )}
                                                    </TableCell>
                                                    <TableCell className="text-right pr-6 font-mono font-bold text-sm text-primary">{line.qtyFulfilled || 0}</TableCell>
                                                </TableRow>
                                            ))}
                                        </TableBody>
                                    </Table>
                                    {isFulfillmentStep && canAction && hasQuantityChanges() && (
                                        <div className="p-2 border-t bg-primary/5 flex justify-end">
                                            <Button
                                                size="sm"
                                                className="h-8 gap-2 bg-primary hover:bg-primary/90"
                                                onClick={onSaveQuantities}
                                                disabled={updateQuantities.isPending}
                                            >
                                                {updateQuantities.isPending ? <Loader2 className="h-3 w-3 animate-spin" /> : <Save className="h-3 w-3" />}
                                                Save Approved Quantities
                                            </Button>
                                        </div>
                                    )}
                                </div>

                                <div className="flex items-center justify-between border-b pb-2">
                                    <h3 className="text-xs font-bold uppercase tracking-widest text-primary">Workflow Diagrams & History</h3>
                                    <Badge variant="outline" className="text-[10px] font-bold tracking-widest px-2 py-0">Audit View</Badge>
                                </div>

                                <div className="space-y-6">
                                    <div className="p-4 bg-muted/5 border rounded-lg">
                                        <p className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground mb-4">Process Visual Diagram</p>
                                        <WorkflowDiagram steps={history} transitions={transitions} />
                                        <div className="flex items-center gap-6 justify-center pt-4">
                                            <div className="flex items-center gap-1.5">
                                                <div className="h-2.5 w-2.5 rounded-full bg-primary" />
                                                <span className="text-[10px] text-muted-foreground uppercase font-bold tracking-widest">Completed</span>
                                            </div>
                                            <div className="flex items-center gap-1.5">
                                                <div className="h-2.5 w-2.5 rounded-full border-2 border-primary bg-primary/5" />
                                                <span className="text-[10px] text-muted-foreground uppercase font-bold tracking-widest">Active</span>
                                            </div>
                                            <div className="flex items-center gap-1.5">
                                                <div className="h-2.5 w-2.5 rounded-full border border-dashed border-border" />
                                                <span className="text-[10px] text-muted-foreground uppercase font-bold tracking-widest">Pending</span>
                                            </div>
                                        </div>
                                    </div>

                                    <div className="space-y-4">
                                        <div className="flex items-center gap-2">
                                            <ClipboardList className="h-4 w-4 text-primary" />
                                            <h4 className="text-xs font-bold uppercase tracking-widest">Chronological Task log</h4>
                                        </div>
                                        <div className="rounded-md border overflow-hidden">
                                            <table className="w-full text-[11px]">
                                                <thead>
                                                    <tr className="bg-muted/50 border-b">
                                                        <th className="p-2 text-left font-bold uppercase tracking-widest text-muted-foreground">Task Step</th>
                                                        <th className="p-2 text-center font-bold uppercase tracking-widest text-muted-foreground">Status</th>
                                                        <th className="p-2 text-left font-bold uppercase tracking-widest text-muted-foreground">Assignees / ClaimedBy</th>
                                                        <th className="p-2 text-right font-bold uppercase tracking-widest text-muted-foreground">Completed</th>
                                                    </tr>
                                                </thead>
                                                <tbody className="divide-y">
                                                    {rawTasks.length > 0 ? rawTasks.map((t: any, idx: number) => (
                                                        <tr key={t.workflowTaskId || idx} className="hover:bg-accent/5 transition-colors">
                                                            <td className="p-2 font-bold">{t.stepName}</td>
                                                            <td className="p-2 text-center">
                                                                <Badge variant={t.statusCode === 'COMPLETED' ? 'default' : 'outline'} className="text-[8px] h-4 px-1">
                                                                    {t.status}
                                                                </Badge>
                                                            </td>
                                                            <td className="p-2 text-muted-foreground">
                                                                {t.claimedBy ? (
                                                                    <span className="font-bold text-foreground">Done by: {t.claimedBy.displayName}</span>
                                                                ) : (
                                                                    <span>Assigned to: {t.assignees.map((a: any) => a.displayName).join(', ') || 'N/A'}</span>
                                                                )}
                                                            </td>
                                                            <td className="p-2 text-right tabular-nums text-muted-foreground">
                                                                {t.completedAt ? new Date(t.completedAt).toLocaleString() : '-'}
                                                            </td>
                                                        </tr>
                                                    )) : (
                                                        <tr>
                                                            <td colSpan={4} className="p-8 text-center text-muted-foreground italic">No tasks created yet.</td>
                                                        </tr>
                                                    )}
                                                </tbody>
                                            </table>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </CardContent>
                    </Card>

                    {/* Action Bar Below Details */}
                    {myActiveTask && (
                        <TaskActionBar
                            task={myActiveTask}
                            request={request}
                        />
                    )}
                </div>

                {/* Workflow Tracker Sidebar */}
                <div className="space-y-6">
                    <Card>
                        <CardHeader className="border-b py-4">
                            <CardTitle className="text-sm font-bold uppercase tracking-tight flex items-center gap-2">
                                <Workflow className="h-4 w-4 text-primary" />
                                Process Flow
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="pt-6 px-4 space-y-3">
                            {historyLoading ? (
                                <div className="flex justify-center py-6">
                                    <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
                                </div>
                            ) : history?.map((step: any, idx: number) => {
                                const isDone = step.status === 'COMPLETED';
                                const isActive = step.status === 'AVAILABLE' || step.status === 'CLAIMED' || step.status === 'ACTIVE';

                                // Find explicit manual assignments for this step
                                const stepManualAssignments = manualAssignments
                                    .filter((ma: any) => ma.workflowStepId === step.workflowStepId)
                                    .map((ma: any) => ma.userDisplayName);

                                const rejectionCount = getRejectionCount(step.stepKey);

                                return (
                                    <div
                                        key={step.workflowStepId || idx}
                                        className={`p-4 rounded-lg border transition-all duration-300 ${isActive ? 'bg-primary/5 border-primary shadow-sm' : 'bg-muted/10 border-border/50'}`}
                                    >
                                        <div className="flex items-center justify-between mb-2">
                                            <div className="flex items-center gap-2">
                                                <div className={`h-6 w-6 rounded-full flex items-center justify-center text-[10px] font-bold ${isDone ? 'bg-primary text-primary-foreground' : (isActive ? 'bg-primary/20 text-primary' : 'bg-muted text-muted-foreground')}`}>
                                                    {isDone ? <CheckCircle2 className="h-3.5 w-3.5" /> : idx + 1}
                                                </div>
                                                <div className="flex flex-col">
                                                    <p className={`text-xs font-bold uppercase tracking-tight ${isActive ? 'text-primary' : 'text-foreground'}`}>{step.stepName}</p>
                                                    {rejectionCount > 0 && (
                                                        <span className="text-[9px] font-bold text-destructive uppercase tracking-tighter">
                                                            Rejected {rejectionCount} {rejectionCount === 1 ? 'time' : 'times'}
                                                        </span>
                                                    )}
                                                </div>
                                            </div>
                                            <Badge variant={isDone ? 'default' : (isActive ? 'secondary' : 'outline')} className="text-[8px] uppercase font-bold tracking-widest px-1.5 py-0">
                                                {isDone ? 'Completed' : (isActive ? 'Current' : 'Planned')}
                                            </Badge>
                                        </div>

                                        <div className="space-y-1.5">
                                            <div className="flex items-start gap-2">
                                                <UserCheck className={`h-3 w-3 mt-0.5 ${isActive ? 'text-primary' : 'text-muted-foreground'}`} />
                                                <p className="text-[10px] leading-tight flex-1">
                                                    <span className="font-bold text-muted-foreground uppercase tracking-widest mr-1">Assignee:</span>
                                                    <span className="font-semibold text-foreground">
                                                        {(step.assignees || []).map((a: any) => a.displayName).join(', ') || 'To be determined'}
                                                    </span>
                                                </p>
                                            </div>

                                            {stepManualAssignments.length > 0 && !isDone && (
                                                <div className="flex items-start gap-2 pt-1 border-t border-dashed mt-1">
                                                    <ShieldCheck className="h-3 w-3 mt-0.5 text-primary/60" />
                                                    <p className="text-[9px] leading-tight italic">
                                                        <span className="font-bold text-muted-foreground uppercase tracking-widest mr-1">Pre-Selected:</span>
                                                        <span className="text-foreground">{stepManualAssignments.join(', ')}</span>
                                                    </p>
                                                </div>
                                            )}

                                            {isDone && step.completedAt && (
                                                <div className="flex items-start gap-2">
                                                    <Calendar className="h-3 w-3 mt-0.5 text-muted-foreground" />
                                                    <p className="text-[10px] leading-tight">
                                                        <span className="font-bold text-muted-foreground uppercase tracking-widest mr-1">Action At:</span>
                                                        <span className="text-muted-foreground">
                                                            {new Date(step.completedAt).toLocaleDateString()}
                                                        </span>
                                                    </p>
                                                </div>
                                            )}
                                        </div>
                                    </div>
                                );
                            })}
                        </CardContent>
                    </Card>

                    <Card className="bg-muted/10 border-dashed">
                        <CardContent className="pt-6 flex flex-col items-center text-center gap-3">
                            <div className="p-3 rounded-full bg-background border border-border shadow-sm">
                                <Building2 className="h-5 w-5 text-muted-foreground" />
                            </div>
                            <div className="space-y-1">
                                <p className="text-xs font-bold uppercase tracking-widest">Process Transparency</p>
                                <p className="text-[10px] text-muted-foreground">This view shows all steps including historical actions and planned assignees.</p>
                            </div>
                        </CardContent>
                    </Card>
                </div>
            </div>
        </div >
    );
}





function TimelineItem({
    title,
    subtitle,
    icon,
    isCompleted,
    active
}: {
    title: string;
    subtitle: string;
    icon: React.ReactNode;
    isCompleted: boolean;
    active?: boolean;
}) {
    // This is now deprecated but kept for backward compatibility if needed elsewhere
    return null;
}
