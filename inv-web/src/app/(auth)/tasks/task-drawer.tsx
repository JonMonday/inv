'use client';

import { useState } from 'react';
import {
    Drawer,
    DrawerContent,
    DrawerHeader,
    DrawerTitle,
    DrawerFooter,
    DrawerClose,
    DrawerDescription
} from '@/components/ui/drawer';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Textarea } from '@/components/ui/textarea';
import { Task, useTasks, useEligibleAssignees } from '@/hooks/useTasks';
import { useAuthStore } from '@/store/authStore';
import { useFulfillmentDetails } from '@/hooks/useRequests';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import {
    ClipboardList,
    CheckCircle2,
    XCircle,
    Ban,
    Loader2,
    Package,
    ArrowRight
} from 'lucide-react';
import { useToast } from '@/hooks/use-toast';
import { cn } from '@/lib/utils';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow
} from '@/components/ui/table';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";

export function TaskDrawer({
    task,
    isOpen,
    onClose
}: {
    task: Task | null;
    isOpen: boolean;
    onClose: () => void
}) {
    const { user } = useAuthStore();
    const { claimMutation, actionMutation } = useTasks();
    const { data: eligibleAssignees } = useEligibleAssignees(task?.id || null);
    const { data: fulfillmentDetails, isLoading: isFulfillmentLoading, error: fulfillmentError } = useFulfillmentDetails(task?.requestId || null);
    const isStorekeeper = user?.roles.includes('STOREKEEPER');

    const [notes, setNotes] = useState('');
    const [nextAssigneeId, setNextAssigneeId] = useState<number | null>(null);
    const [selectedWarehouses, setSelectedWarehouses] = useState<Record<number, number>>({});
    const { toast } = useToast();

    if (!task) return null;

    const handleClaim = async () => {
        try {
            await claimMutation.mutateAsync(task.id);
            toast({ title: 'Task claimed successfully' });
        } catch (error: unknown) {
            const message = error instanceof Error ? error.message : 'Unknown error';
            toast({
                variant: 'destructive',
                title: 'Failed to claim task',
                description: message
            });
        }
    };

    const handleAction = async (action: 'APPROVE' | 'REJECT' | 'CANCEL' | 'SUBMIT') => {
        try {
            let actualAction: string = action;
            if (task.stepKey === 'START' && action === 'APPROVE') {
                actualAction = 'SUBMIT';
            } else if (isFulfillmentStep && action === 'APPROVE') {
                actualAction = 'COMPLETE';
            }

            const payload: any = { nextAssigneeUserId: nextAssigneeId || undefined };
            if (isFulfillmentStep && action === 'APPROVE') {
                console.log('🔍 DEBUG: selectedWarehouses state:', selectedWarehouses);
                payload.fulfillments = Object.entries(selectedWarehouses).map(([productId, warehouseId]) => ({
                    productId: Number(productId),
                    warehouseId
                }));
                console.log('🔍 DEBUG: fulfillments payload:', payload.fulfillments);
            }

            console.log('🔍 DEBUG: Final payload being sent:', payload);
            await actionMutation.mutateAsync({
                taskId: task.id,
                action: actualAction as any,
                notes,
                payloadJson: JSON.stringify(payload)
            });
            toast({ title: `Task ${actualAction.toLowerCase()}d successfully` });
            onClose();
        } catch (error: unknown) {
            const message = error instanceof Error ? error.message : 'Unknown error';
            toast({
                variant: 'destructive',
                title: `Failed to ${action.toLowerCase()} task`,
                description: message
            });
        }
    };

    const isUnclaimed = !task.claimedByUserId;
    const isClaimedByMe = task.claimedByUserId === user?.userId;
    const isClaimedByOther = task.claimedByUserId && !isClaimedByMe;
    const canAct = !isClaimedByOther;
    const isRequester = user?.userId === task.initiatorUserId;
    const isFulfillmentStep = task.stepKey === 'FULFILL' || task.stepKey === 'FULFILLMENT' || task.stepName === 'Fulfillment';
    const isStartStep = task.stepKey === 'START' || task.stepKey === 'SUBMISSION';

    return (
        <Drawer open={isOpen} onOpenChange={(open) => !open && onClose()} direction="right">
            <DrawerContent className="left-auto right-0 mt-0 h-full w-[500px] rounded-none border-l">
                <DrawerHeader className="border-b space-y-4">
                    <div className="flex items-center justify-between">
                        <Badge variant="outline">{task.stepName}</Badge>
                        <span className="text-[10px] text-muted-foreground uppercase font-bold tracking-wider">REQ-{task.requestId}</span>
                    </div>
                    <DrawerTitle className="text-xl font-bold">{task.title}</DrawerTitle>
                    <DrawerDescription>{task.description}</DrawerDescription>
                </DrawerHeader>

                <div className="flex-1 overflow-y-auto p-6 space-y-8">
                    <div className="space-y-3">
                        <h3 className="text-[11px] font-bold uppercase text-muted-foreground tracking-wider">Status</h3>
                        <div className="flex items-center gap-2">
                            <Badge variant={isClaimedByOther ? "outline" : (isClaimedByMe ? "default" : "secondary")}>
                                {isClaimedByOther ? "Claimed" : (isClaimedByMe ? "In Progress" : "Available")}
                            </Badge>
                            {task.claimedByUserName && (
                                <span className="text-xs text-muted-foreground">
                                    {isClaimedByMe ? "Assigned to you" : `Claimed by ${task.claimedByUserName}`}
                                </span>
                            )}
                        </div>
                    </div>

                    {isFulfillmentStep && (
                        <div className="space-y-4">
                            <h3 className="text-[11px] font-bold uppercase text-muted-foreground tracking-wider flex items-center gap-2">
                                <Package className="h-3 w-3" /> Requested Items & Stock
                            </h3>
                            {isFulfillmentLoading ? (
                                <Loader2 className="h-4 w-4 animate-spin" />
                            ) : (
                                <div className="border rounded-lg overflow-hidden">
                                    <Table>
                                        <TableHeader className="bg-muted/50">
                                            <TableRow>
                                                <TableHead className="text-[10px] uppercase h-8 px-3">Item</TableHead>
                                                <TableHead className="text-[10px] uppercase h-8 px-2 text-center">Qty</TableHead>
                                                <TableHead className="text-[10px] uppercase h-8 px-3">Source Warehouse</TableHead>
                                            </TableRow>
                                        </TableHeader>
                                        <TableBody>
                                            {fulfillmentError ? (
                                                <TableRow>
                                                    <TableCell colSpan={3} className="text-center py-8 text-destructive text-xs">
                                                        Failed to load items. Check permissions.
                                                    </TableCell>
                                                </TableRow>
                                            ) : isFulfillmentLoading ? (
                                                <TableRow>
                                                    <TableCell colSpan={3} className="text-center py-8 text-muted-foreground text-xs">
                                                        <Loader2 className="h-4 w-4 animate-spin mx-auto mb-2" />
                                                        Loading items...
                                                    </TableCell>
                                                </TableRow>
                                            ) : fulfillmentDetails?.lines && fulfillmentDetails.lines.length > 0 ? (
                                                fulfillmentDetails.lines.map((line: any) => (
                                                    <TableRow key={line.productId}>
                                                        <TableCell className="text-xs font-medium py-3 px-3">{line.productName}</TableCell>
                                                        <TableCell className="text-xs text-center py-3 px-2 font-mono">{line.qtyRequested}</TableCell>
                                                        <TableCell className="py-2 px-3">
                                                            {isStorekeeper ? (
                                                                <Select
                                                                    value={selectedWarehouses[line.productId] ? String(selectedWarehouses[line.productId]) : ""}
                                                                    onValueChange={(val) => setSelectedWarehouses(prev => ({ ...prev, [line.productId]: Number(val) }))}
                                                                >
                                                                    <SelectTrigger className={cn("h-8 text-[11px]", isStorekeeper && !selectedWarehouses[line.productId] && "border-destructive text-destructive")}>
                                                                        <SelectValue placeholder="Select Warehouse" />
                                                                    </SelectTrigger>
                                                                    <SelectContent>
                                                                        {line.stock && line.stock.map((s: any) => (
                                                                            <SelectItem key={s.warehouseId} value={String(s.warehouseId)}>
                                                                                <div className="flex flex-col">
                                                                                    <span>{s.warehouseName}</span>
                                                                                    <span className="text-[10px] opacity-70">Avail: {s.availableQty}</span>
                                                                                </div>
                                                                            </SelectItem>
                                                                        ))}
                                                                    </SelectContent>
                                                                </Select>
                                                            ) : (
                                                                <span className="text-xs">{fulfillmentDetails.warehouseName || `WH-${fulfillmentDetails.warehouseId}`}</span>
                                                            )}
                                                        </TableCell>
                                                    </TableRow>
                                                ))
                                            ) : (
                                                <TableRow>
                                                    <TableCell colSpan={3} className="text-center py-8 text-muted-foreground text-xs">
                                                        No items found for this request.
                                                    </TableCell>
                                                </TableRow>
                                            )}
                                        </TableBody>
                                    </Table>
                                </div>
                            )}
                        </div>
                    )}

                    {!isFulfillmentStep && fulfillmentDetails && (
                        <div className="space-y-3">
                            <h3 className="text-[11px] font-bold uppercase text-muted-foreground tracking-wider">Requested Items</h3>
                            <div className="border rounded-lg p-2 space-y-1">
                                {fulfillmentDetails.lines && fulfillmentDetails.lines.length > 0 ? (
                                    fulfillmentDetails.lines.map((line: any) => (
                                        <div key={line.productId} className="flex items-center justify-between text-xs px-2 py-1 hover:bg-accent/50 rounded-sm">
                                            <span className="font-medium text-foreground">{line.productName}</span>
                                            <Badge variant="secondary" className="font-mono px-1 h-5 text-[10px]">{line.qtyRequested}</Badge>
                                        </div>
                                    ))
                                ) : (
                                    <div className="text-center py-4 text-muted-foreground text-[10px]">No items items found.</div>
                                )}
                            </div>
                        </div>
                    )}

                    {isClaimedByOther && (
                        <div className="bg-amber-50 border border-amber-200 rounded-lg p-4 flex gap-3 text-amber-800">
                            <Ban className="h-5 w-5 shrink-0" />
                            <div className="space-y-1">
                                <p className="text-sm font-semibold">Locked Task</p>
                                <p className="text-xs leading-relaxed">This task is currently being handled by {task.claimedByUserName}. You cannot take action until they release it.</p>
                            </div>
                        </div>
                    )}

                    {canAct && (
                        <>
                            {isUnclaimed && (
                                <div className="flex items-center justify-between bg-accent/30 rounded-lg p-3">
                                    <div className="text-xs font-medium">Want to work on this later?</div>
                                    <Button variant="outline" size="sm" onClick={handleClaim} disabled={claimMutation.isPending}>
                                        {claimMutation.isPending ? <Loader2 className="h-3 w-3 animate-spin" /> : "Reserve Task"}
                                    </Button>
                                </div>
                            )}

                            <div className="space-y-3">
                                <h3 className="text-[11px] font-bold uppercase text-muted-foreground tracking-wider">Action Notes</h3>
                                <Textarea
                                    placeholder="Add explanation for your action..."
                                    value={notes}
                                    onChange={(e) => setNotes(e.target.value)}
                                    className="resize-none"
                                    rows={3}
                                />
                            </div>

                            {eligibleAssignees && eligibleAssignees.length > 0 && (
                                <div className="space-y-4">
                                    <h3 className="text-[11px] font-bold uppercase text-muted-foreground tracking-wider">Next Assignee (Optional)</h3>
                                    <div className="grid grid-cols-1 gap-2">
                                        {eligibleAssignees.map((assignee: { userId: number, displayName: string, roles: string[] }) => (
                                            <button
                                                key={assignee.userId}
                                                onClick={() => setNextAssigneeId(nextAssigneeId === assignee.userId ? null : assignee.userId)}
                                                className={cn(
                                                    "flex items-center justify-between rounded-lg border p-3 text-left transition-colors hover:bg-accent",
                                                    nextAssigneeId === assignee.userId ? "border-primary bg-primary/5" : "bg-card"
                                                )}
                                            >
                                                <div className="flex items-center gap-3">
                                                    <Avatar className="h-8 w-8">
                                                        <AvatarFallback>{assignee.displayName[0]}</AvatarFallback>
                                                    </Avatar>
                                                    <div className="flex flex-col">
                                                        <span className="text-sm font-medium">{assignee.displayName}</span>
                                                        <span className="text-[10px] text-muted-foreground uppercase">{assignee.roles?.join(', ') || 'No Roles'}</span>
                                                    </div>
                                                </div>
                                                {nextAssigneeId === assignee.userId && <CheckCircle2 className="h-4 w-4 text-primary" />}
                                            </button>
                                        ))}
                                    </div>
                                </div>
                            )}

                            <div className="grid grid-cols-2 gap-2 pt-4 border-t">
                                <Button
                                    variant="default"
                                    onClick={() => handleAction('APPROVE')}
                                    disabled={actionMutation.isPending || (isFulfillmentStep && isStorekeeper && fulfillmentDetails?.lines?.some((l: any) => !selectedWarehouses[l.productId]))}
                                >
                                    <CheckCircle2 className="mr-2 h-4 w-4" /> {isFulfillmentStep ? 'Approve & Fulfill' : (isStartStep ? 'Submit Request' : 'Approve')}
                                </Button>
                                {!isStartStep && (
                                    <Button variant="outline" onClick={() => handleAction('REJECT')} disabled={actionMutation.isPending}>
                                        <XCircle className="mr-2 h-4 w-4" /> Reject
                                    </Button>
                                )}
                                {isRequester && (
                                    <Button variant="ghost" className="col-span-2 text-destructive hover:text-destructive hover:bg-destructive/10" onClick={() => handleAction('CANCEL')} disabled={actionMutation.isPending}>
                                        <Ban className="mr-2 h-4 w-4" /> Cancel Request
                                    </Button>
                                )}
                            </div>
                        </>
                    )}
                </div>

                <DrawerFooter className="border-t">
                    <DrawerClose asChild>
                        <Button variant="outline">Close</Button>
                    </DrawerClose>
                </DrawerFooter>
            </DrawerContent>
        </Drawer>
    );
}
