import { useState, useEffect } from 'react';
import { useTasks } from '@/hooks/useTasks';
import { useRequests, useUpdateRequest } from '@/hooks/useRequests';
import { useToast } from '@/hooks/use-toast';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Table, TableHeader, TableRow, TableHead, TableBody, TableCell } from '@/components/ui/table';
import { Select, SelectTrigger, SelectValue, SelectContent, SelectItem } from '@/components/ui/select';
import {
    CheckCircle2,
    Loader2,
    Package,
    XCircle,
    Send,
    Save,
    ClipboardCheck,
    ChevronRight
} from 'lucide-react';
import { cn } from '@/lib/utils';

interface TaskActionBarProps {
    task: any;
    request: any;
}

export function TaskActionBar({ task, request }: TaskActionBarProps) {
    const { actionMutation, claimMutation } = useTasks();
    const updateRequestMutation = useUpdateRequest();
    const { toast } = useToast();
    const [notes, setNotes] = useState('');

    const isFulfillmentStep = task.stepKey === 'FULFILL' || task.stepKey === 'FULFILLMENT' || task.stepName === 'Fulfillment';
    const isStartStep = task.stepKey === 'START' || task.stepKey === 'SUBMISSION';
    const isConfirmationStep = task.stepKey === 'CONFIRMATION' || task.stepKey === 'CONFIRM';

    const needsClaim = !task.claimedByUserId && !isStartStep && !isConfirmationStep;

    const handleClaim = async () => {
        try {
            await claimMutation.mutateAsync(task.id);
            toast({ title: 'Task claimed successfully' });
        } catch (error) {
            toast({ variant: 'destructive', title: 'Failed to claim task' });
        }
    };

    const handleAction = async (action: 'APPROVE' | 'REJECT' | 'COMPLETE' | 'SUBMIT') => {
        try {
            await actionMutation.mutateAsync({
                taskId: task.id,
                action: action as any,
                notes,
                payloadJson: "{}"
            });
            toast({ title: `Action processed successfully` });
        } catch (error: any) {
            toast({
                variant: 'destructive',
                title: `Action failed`,
                description: error.message || 'Unknown error'
            });
        }
    };

    const handleSave = async () => {
        try {
            // Reconstruct the DTO expected by the backend
            const dto = {
                requestTypeId: request.requestTypeId,
                warehouseId: request.warehouseId,
                departmentId: request.departmentId,
                notes: request.notes,
                workflowTemplateId: request.workflowTemplateId,
                lines: (request.lines || []).map((l: any) => ({
                    productId: l.productId,
                    quantity: l.qtyRequested
                }))
            };

            await updateRequestMutation.mutateAsync({
                requestId: request.requestId,
                data: dto
            });
            toast({ title: 'Draft saved successfully' });
        } catch (error: any) {
            toast({
                variant: 'destructive',
                title: 'Failed to save draft',
                description: error.message || 'Unknown error'
            });
        }
    };

    // if (needsClaim) {
    //     return (
    //         <Card className="border-primary/50 bg-primary/5 mb-6">
    //             <CardHeader className="py-4">
    //                 <CardTitle className="text-sm font-bold uppercase tracking-tight flex items-center gap-2 text-primary">
    //                     <CheckCircle2 className="h-4 w-4" /> Action Required: {task.stepName}
    //                 </CardTitle>
    //             </CardHeader>
    //             <CardContent className="flex flex-col gap-4">
    //                 <p className="text-sm text-muted-foreground">You are a candidate for this task. Claim it to start working.</p>
    //                 <Button onClick={handleClaim} disabled={claimMutation.isPending} className="w-fit">
    //                     {claimMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : null}
    //                     Claim Task
    //                 </Button>
    //             </CardContent>
    //         </Card>
    //     );
    // }

    const isPending = actionMutation.isPending || updateRequestMutation.isPending;

    return (
        <div className="flex flex-wrap justify-end items-center gap-3">
            {isStartStep ? (
                <>
                    <Button
                        onClick={handleSave}
                        variant="outline"
                        className="px-8 h-11 text-sm font-bold border-muted-foreground/30 hover:bg-accent/10"
                        disabled={isPending}
                    >
                        {updateRequestMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : null}
                        Save as Draft
                    </Button>
                    <Button
                        onClick={() => handleAction('SUBMIT')}
                        className="px-8 h-11 text-sm font-bold bg-foreground text-background hover:bg-foreground/90 group"
                        disabled={isPending}
                    >
                        {actionMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : null}
                        Submit Request
                        <ChevronRight className="ml-2 h-4 w-4 text-background/70 group-hover:translate-x-0.5 transition-transform" />
                    </Button>
                </>
            ) : isConfirmationStep ? (
                <>
                    <Button
                        onClick={() => handleAction('APPROVE')}
                        className="px-8 h-11 text-sm font-bold bg-foreground text-background hover:bg-foreground/90 group"
                        disabled={isPending}
                    >
                        {actionMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : null}
                        Confirm Received
                        <ChevronRight className="ml-2 h-4 w-4 text-background/70 group-hover:translate-x-0.5 transition-transform" />
                    </Button>
                    <Button
                        variant="ghost"
                        onClick={() => handleAction('REJECT')}
                        className="px-8 h-11 text-sm font-bold text-destructive hover:bg-destructive/10"
                        disabled={isPending}
                    >
                        {actionMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : <XCircle className="h-4 w-4 mr-2" />}
                        Dispute / Reject
                    </Button>
                </>
            ) : isFulfillmentStep ? (
                <Button
                    onClick={() => handleAction('COMPLETE')}
                    className="px-8 h-11 text-sm font-bold bg-foreground text-background hover:bg-foreground/90 group"
                    disabled={isPending}
                >
                    {actionMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : <Package className="h-4 w-4 mr-2" />}
                    Complete Fulfillment
                    <ChevronRight className="ml-2 h-4 w-4 text-background/70 group-hover:translate-x-0.5 transition-transform" />
                </Button>
            ) : (
                <>
                    <Button
                        onClick={() => handleAction('APPROVE')}
                        className="px-8 h-11 text-sm font-bold bg-foreground text-background hover:bg-foreground/90 group"
                        disabled={isPending}
                    >
                        {actionMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : <CheckCircle2 className="h-4 w-4 mr-2" />}
                        Approve
                        <ChevronRight className="ml-2 h-4 w-4 text-background/70 group-hover:translate-x-0.5 transition-transform" />
                    </Button>
                    {/* <Button
                        variant="ghost"
                        onClick={() => handleAction('REJECT')}
                        className="px-8 h-11 text-sm font-bold text-destructive hover:bg-destructive/10"
                        disabled={isPending}
                    >
                        {actionMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : <XCircle className="h-4 w-4 mr-2" />}
                        Reject
                    </Button> */}
                    <Button
                        onClick={() => handleAction('REJECT')}
                        className="px-8 h-11 text-sm font-bold bg-destructive text-white hover:bg-destructive/90 group"
                        disabled={isPending}
                    >
                        {actionMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : <XCircle className="h-4 w-4 mr-2" />}
                        Reject
                    </Button>
                </>
            )}
        </div>
    );
}
