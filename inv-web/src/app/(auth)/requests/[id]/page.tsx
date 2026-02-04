'use client';

import { useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { useRequest, useSubmitRequest, useFulfillment } from '@/hooks/useRequests';
import { useAuthStore } from '@/store/authStore';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow
} from '@/components/ui/table';
import { useTasks, Task } from '@/hooks/useTasks';
import { TaskDrawer } from '../../tasks/task-drawer';
import {
    CheckCircle2,
    Clock,
    Package,
    ArrowRight,
    ArrowLeft,
    Loader2,
    FileText,
    Warehouse,
    Building2,
    Calendar,
    ClipboardList
} from 'lucide-react';
import { useToast } from '@/hooks/use-toast';

export default function RequestDetailPage() {
    const { id } = useParams();
    const router = useRouter();
    const { toast } = useToast();
    const { user } = useAuthStore();
    const { data: request, isLoading: isRequestLoading } = useRequest(Number(id));
    const { tasksQuery } = useTasks();
    const [selectedTask, setSelectedTask] = useState<Task | null>(null);
    const submitMutation = useSubmitRequest();
    const fulfillmentMutation = useFulfillment();

    if (isRequestLoading || tasksQuery.isLoading) return <div className="p-8"><Loader2 className="animate-spin h-8 w-8" /></div>;
    if (!request) return <div className="p-8">Request not found</div>;

    const myTask = tasksQuery.data?.data?.find(t => t.requestId === Number(id));

    const isStorekeeper = user?.roles.includes('STOREKEEPER');
    const isDraft = request.status?.code === 'DRAFT';
    const isFulfillment = request.status?.code === 'FULFILLMENT';

    const handleSubmit = async () => {
        try {
            await submitMutation.mutateAsync({ requestId: Number(id) });
            toast({ title: 'Request submitted successfully' });
        } catch (error: unknown) {
            const message = error instanceof Error ? error.message : 'Unknown error';
            toast({
                variant: 'destructive',
                title: 'Submission failed',
                description: message
            });
        }
    };

    const handleFulfillment = async (action: 'reserve' | 'release' | 'issue') => {
        const fulfillmentData = {
            movementTypeCode: action.toUpperCase(),
            warehouseId: request.warehouseId,
            requestId: Number(id),
            lines: request.lines.map((l: { productId: number, qtyRequested: number }) => ({
                productId: l.productId,
                qtyDeltaOnHand: action === 'issue' ? -l.qtyRequested : 0,
                qtyDeltaReserved: action === 'reserve' ? l.qtyRequested : (action === 'release' ? -l.qtyRequested : 0),
            }))
        };

        try {
            await fulfillmentMutation.mutateAsync({ requestId: Number(id), action, data: fulfillmentData });
            toast({ title: `Successfully performed ${action}` });
        } catch (error: unknown) {
            const message = error instanceof Error ? error.message : 'Unknown error';
            toast({
                variant: 'destructive',
                title: `Fulfillment action failed`,
                description: message
            });
        }
    };

    return (
        <div className="max-w-6xl mx-auto space-y-6">
            <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                    <Button variant="ghost" size="sm" onClick={() => router.push('/requests')}>
                        <ArrowLeft className="mr-2 h-4 w-4" /> Back to List
                    </Button>
                    <div className="h-4 w-px bg-border" />
                    <h1 className="text-xl font-bold font-mono">REQ-{id}</h1>
                    <Badge variant={request.status?.code === 'DRAFT' ? 'outline' : 'default'}>
                        {request.status?.name || request.status?.code}
                    </Badge>
                </div>
                <div className="flex gap-2">
                    {isDraft && (
                        <>
                            <Button variant="outline" size="sm">Edit Draft</Button>
                            <Button size="sm" onClick={handleSubmit} disabled={submitMutation.isPending}>
                                {submitMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                                Submit Request
                            </Button>
                        </>
                    )}
                </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                <div className="lg:col-span-2 space-y-6">
                    <Card>
                        <CardHeader>
                            <CardTitle className="text-sm font-semibold flex items-center gap-2">
                                <FileText className="h-4 w-4" /> Request Items
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="p-0">
                            <Table>
                                <TableHeader>
                                    <TableRow>
                                        <TableHead className="pl-6">Product</TableHead>
                                        <TableHead>Requested</TableHead>
                                        <TableHead>Approved</TableHead>
                                        <TableHead className="pr-6 text-right">Fulfilling</TableHead>
                                    </TableRow>
                                </TableHeader>
                                <TableBody>
                                    {request.lines.map((line: any) => (
                                        <TableRow key={line.requestLineId}>
                                            <TableCell className="pl-6">
                                                <div className="font-medium text-sm">{line.product?.name || `Prod-${line.productId}`}</div>
                                                <div className="text-[10px] text-muted-foreground uppercase">{line.product?.sku || `SKU-${line.productId}`}</div>
                                            </TableCell>
                                            <TableCell className="text-sm">{line.qtyRequested}</TableCell>
                                            <TableCell className="text-sm">{line.qtyApproved || '-'}</TableCell>
                                            <TableCell className="pr-6 text-right font-medium">{line.qtyFulfilled || 0}</TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        </CardContent>
                    </Card>

                    {isStorekeeper && isFulfillment && (
                        <Card className="border-primary/20 bg-primary/5">
                            <CardHeader>
                                <CardTitle className="text-sm font-semibold flex items-center gap-2">
                                    <Package className="h-4 w-4" /> Fulfillment Actions
                                </CardTitle>
                                <CardDescription>As a Storekeeper, you can manage stock for this request</CardDescription>
                            </CardHeader>
                            <CardContent className="flex gap-3">
                                <Button size="sm" onClick={() => handleFulfillment('reserve')}>Reserve Stock</Button>
                                <Button size="sm" variant="outline" onClick={() => handleFulfillment('release')}>Release Reserve</Button>
                                <div className="flex-1" />
                                <Button size="sm" variant="default" className="bg-emerald-600 hover:bg-emerald-700" onClick={() => handleFulfillment('issue')}>Issue Items</Button>
                            </CardContent>
                        </Card>
                    )}
                </div>

                <div className="space-y-6">
                    {myTask && (
                        <Card className="border-blue-200 bg-blue-50/50">
                            <CardHeader className="pb-3">
                                <CardTitle className="text-sm font-semibold flex items-center justify-between">
                                    <div className="flex items-center gap-2">
                                        <ClipboardList className="h-4 w-4 text-blue-600" />
                                        Your Assigned Task
                                    </div>
                                    <Badge variant="outline" className="bg-blue-100 text-blue-700 border-blue-200 capitalize">
                                        {myTask.status.toLowerCase()}
                                    </Badge>
                                </CardTitle>
                                <CardDescription className="text-[10px] leading-relaxed">
                                    You have an active <strong>{myTask.stepName}</strong> task. Please review and take action.
                                </CardDescription>
                            </CardHeader>
                            <CardContent>
                                <Button size="sm" className="w-full h-8 text-xs font-semibold" onClick={() => setSelectedTask(myTask)}>
                                    Process Task
                                </Button>
                            </CardContent>
                        </Card>
                    )}

                    <Card>
                        <CardHeader>
                            <CardTitle className="text-sm font-semibold">Summary</CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="flex items-center justify-between text-sm">
                                <div className="flex items-center text-muted-foreground">
                                    <Warehouse className="mr-2 h-4 w-4" /> Warehouse
                                </div>
                                <span className="font-medium">{request.warehouse?.name || `WH-${request.warehouseId}`}</span>
                            </div>
                            <div className="flex items-center justify-between text-sm">
                                <div className="flex items-center text-muted-foreground">
                                    <Building2 className="mr-2 h-4 w-4" /> Department
                                </div>
                                <span className="font-medium">{request.department?.name || `Dept-${request.departmentId}`}</span>
                            </div>
                            <div className="flex items-center justify-between text-sm">
                                <div className="flex items-center text-muted-foreground">
                                    <Calendar className="mr-2 h-4 w-4" /> Created
                                </div>
                                <span className="font-medium">{new Date(request.requestedAt).toLocaleDateString()}</span>
                            </div>
                            <div className="pt-2 border-t">
                                <p className="text-[10px] uppercase font-bold text-muted-foreground mb-1">Notes</p>
                                <p className="text-xs">{request.notes || 'No notes provided'}</p>
                            </div>
                        </CardContent>
                    </Card>

                    <Card>
                        <CardHeader>
                            <CardTitle className="text-sm font-semibold">Timeline</CardTitle>
                        </CardHeader>
                        <CardContent className="relative pl-8 space-y-6">
                            <div className="absolute left-[19px] top-6 bottom-6 w-[2px] bg-border" />

                            <TimelineItem
                                title="Draft Created"
                                subtitle={new Date(request.requestedAt).toLocaleDateString()}
                                icon={<Clock className="h-3 w-3" />}
                                isCompleted={true}
                            />
                            <TimelineItem
                                title="Workflow"
                                subtitle={request.status?.code === 'DRAFT' ? 'Pending submission' : 'In Progress'}
                                icon={<ArrowRight className="h-3 w-3" />}
                                isCompleted={!isDraft}
                                active={request.status?.code === 'IN_WORKFLOW'}
                            />
                            <TimelineItem
                                title="Fulfillment"
                                subtitle="Warehouse release"
                                icon={<Package className="h-3 w-3" />}
                                isCompleted={request.status?.code === 'COMPLETED'}
                                active={isFulfillment}
                            />
                            <TimelineItem
                                title="Completed"
                                subtitle="All items issued"
                                icon={<CheckCircle2 className="h-3 w-3" />}
                                isCompleted={request.status?.code === 'COMPLETED'}
                            />
                        </CardContent>
                    </Card>
                </div>
            </div>

            <TaskDrawer
                task={selectedTask}
                isOpen={!!selectedTask}
                onClose={() => setSelectedTask(null)}
            />
        </div>
    );
}

function TimelineItem({ title, subtitle, icon, isCompleted, active }: { title: string, subtitle: string, icon: React.ReactNode, isCompleted: boolean, active?: boolean }) {
    return (
        <div className="relative">
            <div className={`absolute -left-[27px] flex h-5 w-5 items-center justify-center rounded-full border bg-background z-10 ${isCompleted ? 'border-primary text-primary' : (active ? 'border-primary' : 'text-muted-foreground')}`}>
                {icon}
            </div>
            <div className="space-y-0.5">
                <p className={`text-xs font-semibold ${active ? 'text-primary' : ''}`}>{title}</p>
                <p className="text-[10px] text-muted-foreground">{subtitle}</p>
            </div>
        </div>
    );
}
