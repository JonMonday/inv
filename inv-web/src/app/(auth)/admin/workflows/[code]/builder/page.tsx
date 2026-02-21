'use client';

import { useState, useEffect } from 'react';
import { useParams, useRouter } from 'next/navigation';
import {
    useWorkflowTemplate,
    useUpsertTemplateDefinition,
    useDepartments,
    useRoles,
    useReferenceData,
    ReferenceItem
} from '@/hooks/useAdmin';
import { useToast } from '@/hooks/use-toast';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { ScrollArea } from '@/components/ui/scroll-area';
import {
    ArrowLeft,
    Plus,
    Save,
    Trash2,
    GitBranch,
    Zap,
    GripVertical,
    Lock,
    UserCheck
} from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import { FlowDiagram } from '@/components/workflow/FlowDiagram';
import {
    DndContext,
    closestCenter,
    KeyboardSensor,
    PointerSensor,
    useSensor,
    useSensors,
    DragEndEvent
} from '@dnd-kit/core';
import {
    arrayMove,
    SortableContext,
    sortableKeyboardCoordinates,
    useSortable,
    verticalListSortingStrategy
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';

interface WorkflowStep {
    stepKey: string;
    name: string;
    sequenceNo: number;
    rule?: {
        assignmentModeCode: string;
        roleId?: number;
        roleName?: string;
        departmentId?: number;
        departmentName?: string;
        minApprovers: number;
        requireAll: boolean;
        allowRequesterSelect: boolean;
    };
}

function SortableStepItem({ step, isActive, onClick, disabled, locked }: { step: WorkflowStep; isActive: boolean; onClick: () => void; disabled: boolean; locked?: boolean }) {
    const {
        attributes,
        listeners,
        setNodeRef,
        transform,
        transition,
    } = useSortable({ id: step.stepKey, disabled: disabled || locked });

    const style = {
        transform: CSS.Transform.toString(transform),
        transition,
    };

    return (
        <div ref={setNodeRef} style={style} className="mb-2 touch-none">
            <div
                className={`w-full flex items-center gap-2 p-2 rounded-lg border transition-all ${isActive
                    ? 'bg-primary/5 border-primary shadow-sm shadow-primary/10'
                    : 'bg-card hover:border-muted-foreground/30'
                    } ${locked ? 'opacity-80 bg-muted/20' : ''}`}
            >
                <div className="flex-1 cursor-pointer py-1" onClick={onClick}>
                    <div className="flex items-center justify-between w-full">
                        <div className="space-y-1">
                            <div className="flex items-center gap-2">
                                <Badge variant="outline" className="h-5 px-1 font-mono text-[9px]">
                                    {step.sequenceNo}
                                </Badge>
                                <span className="text-sm font-semibold truncate leading-none">
                                    {step.name}
                                </span>
                                {locked && <Badge variant="secondary" className="text-[9px] h-4 px-1">Fixed</Badge>}
                            </div>
                            <p className="text-[10px] font-mono opacity-50">{step.stepKey}</p>
                        </div>
                    </div>
                </div>

                {!disabled && !locked && (
                    <div {...attributes} {...listeners} className="cursor-grab p-1 text-muted-foreground hover:text-foreground">
                        <GripVertical className="h-4 w-4" />
                    </div>
                )}
            </div>
        </div>
    );
}

export default function WorkflowBuilderPage() {
    const params = useParams();
    const router = useRouter();
    const { toast } = useToast();
    const code = params.code as string;

    const { data: template, isLoading: templateLoading } = useWorkflowTemplate(code);
    const { data: departments } = useDepartments();
    const { data: roles } = useRoles({ pageNumber: 1, pageSize: 100 });
    const { data: rejectionModes } = useReferenceData('workflow-rejection-mode');
    const { data: assignmentModes } = useReferenceData('workflow-assignment-mode');

    const saveDefMutation = useUpsertTemplateDefinition();

    const [steps, setSteps] = useState<WorkflowStep[]>([]);
    const [meta, setMeta] = useState({ name: '', departmentId: 0, rejectionModeId: 0, isActive: true });
    const [activeStepIndex, setActiveStepIndex] = useState<number | null>(null);

    useEffect(() => {
        if (template) {
            setMeta({
                name: template.name,
                departmentId: template.departmentId || 0,
                rejectionModeId: template.rejectionModeId || 0,
                isActive: template.isActive
            });

            if (template.steps && template.steps.length > 0) {
                // Show ALL steps (both system and user-defined)
                const allSteps = template.steps
                    .map(s => ({
                        stepKey: s.stepKey,
                        name: s.name,
                        sequenceNo: s.sequenceNo,
                        rule: s.rule ? {
                            assignmentModeCode: s.rule.assignmentModeCode,
                            roleId: s.rule.roleId,
                            roleName: s.rule.roleName,
                            departmentId: s.rule.departmentId,
                            departmentName: s.rule.departmentName,
                            minApprovers: s.rule.minApprovers || 1,
                            requireAll: s.rule.requireAll ?? true,
                            allowRequesterSelect: s.rule.allowRequesterSelect ?? true
                        } : undefined
                    }))
                    .sort((a, b) => a.sequenceNo - b.sequenceNo);

                setSteps(allSteps);
            } else {
                // Show placeholder steps when no steps are defined
                // These are UI-only and won't be saved unless user explicitly saves
                setSteps([
                    { stepKey: 'SUBMISSION', name: 'Submission', sequenceNo: 0 },
                    { stepKey: 'FULFILLMENT', name: 'Fulfillment', sequenceNo: 1 },
                    { stepKey: 'CONFIRMATION', name: 'Requester Confirmation', sequenceNo: 2 },
                    { stepKey: 'END', name: 'End', sequenceNo: 3 }
                ]);
            }
        }
    }, [template]);

    const activeStep = activeStepIndex !== null ? steps[activeStepIndex] : null;

    const fixedStart = ['SUBMISSION'];
    const fixedEnd = ['FULFILLMENT', 'CONFIRMATION', 'END'];
    const isFixed = activeStep && [...fixedStart, ...fixedEnd].includes(activeStep.stepKey);

    // Split steps into sections
    const startSteps = steps.filter(s => fixedStart.includes(s.stepKey)).sort((a, b) => a.sequenceNo - b.sequenceNo);
    const endSteps = steps.filter(s => fixedEnd.includes(s.stepKey)).sort((a, b) => a.sequenceNo - b.sequenceNo);
    const reorderableSteps = steps.filter(s => !fixedStart.includes(s.stepKey) && !fixedEnd.includes(s.stepKey)).sort((a, b) => a.sequenceNo - b.sequenceNo);

    // Drag and drop sensors
    const sensors = useSensors(
        useSensor(PointerSensor),
        useSensor(KeyboardSensor, {
            coordinateGetter: sortableKeyboardCoordinates,
        })
    );

    const handleDragEnd = (event: DragEndEvent) => {
        const { active, over } = event;

        if (over && active.id !== over.id) {
            const oldIndex = reorderableSteps.findIndex(item => item.stepKey === active.id);
            const newIndex = reorderableSteps.findIndex(item => item.stepKey === over.id);

            if (oldIndex !== -1 && newIndex !== -1) {
                const newReorderable = arrayMove(reorderableSteps, oldIndex, newIndex);

                // Reconstruct full list and re-sequence
                const fullList = [...startSteps, ...newReorderable, ...endSteps];

                const resequenced = fullList.map((step, index) => ({
                    ...step,
                    sequenceNo: index
                }));

                setSteps(resequenced);

                // Maintenance active step index
                if (activeStepIndex !== null) {
                    const activeKey = steps[activeStepIndex].stepKey;
                    const newActiveIndex = resequenced.findIndex(s => s.stepKey === activeKey);
                    setActiveStepIndex(newActiveIndex);
                }
            }
        }
    };

    const addStep = () => {
        // Find existing custom steps max sequence to insert after them but before fixed end steps
        // Actually, we can just insert before the first fixed end step

        const insertIndex = startSteps.length + reorderableSteps.length;

        const newStep: WorkflowStep = {
            stepKey: `NEW_STEP_${Date.now()}`, // Temporary key
            name: 'New Step',
            sequenceNo: 0, // Will be recalculated
            rule: {
                assignmentModeCode: 'SYSTEM',
                minApprovers: 1,
                requireAll: true,
                allowRequesterSelect: true
            }
        };

        const newSteps = [...steps];
        newSteps.splice(insertIndex, 0, newStep);

        // Recalculate all sequences
        const resequenced = newSteps.map((s, i) => ({ ...s, sequenceNo: i }));

        setSteps(resequenced);
        setActiveStepIndex(insertIndex);
    };

    const updateStep = (index: number, updates: Partial<WorkflowStep>) => {
        const newSteps = [...steps];
        newSteps[index] = { ...newSteps[index], ...updates };
        setSteps(newSteps);
    };

    const deleteStep = (index: number) => {
        const stepToDelete = steps[index];
        const isFixedStep = [...fixedStart, ...fixedEnd].includes(stepToDelete.stepKey);

        if (isFixedStep) {
            toast({ title: "Cannot delete step", description: "This is a system required step.", variant: "destructive" });
            return;
        }

        const newSteps = steps.filter((_, i) => i !== index);
        // Recalculate sequences
        const resequenced = newSteps.map((s, i) => ({ ...s, sequenceNo: i }));
        setSteps(resequenced);
        setActiveStepIndex(null);
    };

    const handleSaveDefinition = async () => {
        try {
            // The backend automatically wraps user steps with SUBMISSION/FULFILLMENT/etc.
            const userSteps = steps.filter(s => !fixedStart.includes(s.stepKey) && !fixedEnd.includes(s.stepKey));
            await saveDefMutation.mutateAsync({
                id: Number(template?.workflowTemplateId),
                data: { steps: userSteps, transitions: [] }
            });
            toast({ title: "Success", description: "Workflow definition updated." });
        } catch (error: unknown) {
            const err = error as { response?: { data?: { message?: string } } };
            toast({ title: "Error", description: err.response?.data?.message || "Failed to save definition.", variant: "destructive" });
        }
    };


    if (templateLoading) return <div className="p-8 text-center text-muted-foreground">Loading builder...</div>;

    const isPublished = template?.status === 'PUBLISHED';

    return (
        <div className="flex flex-col h-[calc(100vh-100px)] -m-6">
            {/* Header */}
            <div className="flex items-center justify-between px-6 py-3 border-b bg-card">
                <div className="flex items-center gap-4">
                    <Button variant="ghost" size="icon" onClick={() => router.back()}>
                        <ArrowLeft className="h-4 w-4" />
                    </Button>
                    <div>
                        <h1 className="text-lg font-bold">Builder: {template?.name}</h1>
                        <p className="text-[10px] uppercase font-mono text-muted-foreground">{code}</p>
                    </div>
                    {isPublished && (
                        <Badge variant="secondary" className="text-xs">
                            Read-Only - Published
                        </Badge>
                    )}
                </div>
                <div className="flex items-center gap-2">
                    {!isPublished && (
                        <Button size="sm" variant="outline" onClick={handleSaveDefinition} disabled={saveDefMutation.isPending}>
                            <Save className="mr-2 h-4 w-4" /> Save Builder
                        </Button>
                    )}
                </div>
            </div>

            <div className="flex flex-1 overflow-hidden">
                {/* Steps Sidebar */}
                <div className="w-80 border-r bg-muted/30 flex flex-col">
                    <div className="p-4 flex items-center justify-between">
                        <h3 className="text-sm font-bold uppercase tracking-wider text-muted-foreground">Steps</h3>
                        <Button variant="ghost" size="icon" className="h-8 w-8" onClick={addStep} disabled={isPublished}>
                            <Plus className="h-4 w-4" />
                        </Button>
                    </div>
                    <ScrollArea className="flex-1">
                        <div className="px-3 pb-4 space-y-1">
                            {steps.length === 0 && (
                                <div className="p-8 text-center border-2 border-dashed rounded-lg opacity-50">
                                    <Zap className="h-8 w-8 mx-auto mb-2" />
                                    <p className="text-xs">No steps defined. Add the first step to get started.</p>
                                </div>
                            )}

                            <div className="space-y-2">
                                {/* Fixed Start Steps */}
                                {startSteps.map((step) => {
                                    const idx = steps.findIndex(s => s.stepKey === step.stepKey);
                                    return (
                                        <SortableStepItem
                                            key={step.stepKey}
                                            step={step}
                                            isActive={activeStepIndex === idx}
                                            onClick={() => setActiveStepIndex(idx)}
                                            disabled={isPublished}
                                            locked={true}
                                        />
                                    );
                                })}

                                {/* Sortable Middle Steps */}
                                <DndContext
                                    sensors={sensors}
                                    collisionDetection={closestCenter}
                                    onDragEnd={handleDragEnd}
                                >
                                    <SortableContext
                                        items={reorderableSteps.map(s => s.stepKey)}
                                        strategy={verticalListSortingStrategy}
                                    >
                                        {reorderableSteps.map((step) => {
                                            const idx = steps.findIndex(s => s.stepKey === step.stepKey);
                                            return (
                                                <SortableStepItem
                                                    key={step.stepKey}
                                                    step={step}
                                                    isActive={activeStepIndex === idx}
                                                    onClick={() => setActiveStepIndex(idx)}
                                                    disabled={isPublished}
                                                    locked={false}
                                                />
                                            );
                                        })}
                                    </SortableContext>
                                </DndContext>

                                {/* Fixed End Steps */}
                                {endSteps.map((step) => {
                                    const idx = steps.findIndex(s => s.stepKey === step.stepKey);
                                    return (
                                        <SortableStepItem
                                            key={step.stepKey}
                                            step={step}
                                            isActive={activeStepIndex === idx}
                                            onClick={() => setActiveStepIndex(idx)}
                                            disabled={isPublished}
                                            locked={true}
                                        />
                                    );
                                })}
                            </div>
                        </div>
                    </ScrollArea>
                </div>

                {/* Main Editor */}
                <div className="flex-1 bg-card overflow-hidden">
                    {activeStepIndex === null ? (
                        <div className="h-full flex flex-col items-center justify-center text-muted-foreground opacity-30">
                            <GitBranch className="h-16 w-16 mb-4 stroke-1" />
                            <p className="text-lg">Select a step to configure its properties</p>
                        </div>
                    ) : (
                        <Tabs defaultValue="properties" className="h-full flex flex-col">
                            <div className="px-6 border-b">
                                <TabsList className="bg-transparent h-12 border-b-0">
                                    <TabsTrigger value="properties" className="data-[state=active]:bg-primary/5 rounded-none border-b-2 border-transparent data-[state=active]:border-primary">Step Properties</TabsTrigger>
                                    <TabsTrigger value="assignment" className="data-[state=active]:bg-primary/5 rounded-none border-b-2 border-transparent data-[state=active]:border-primary">Assignment Mode</TabsTrigger>
                                    <TabsTrigger value="visual" className="data-[state=active]:bg-primary/5 rounded-none border-b-2 border-transparent data-[state=active]:border-primary">Flow Visualizer</TabsTrigger>
                                </TabsList>
                            </div>

                            <ScrollArea className="flex-1">
                                <div className="p-8 max-w-2xl mx-auto space-y-8 pb-20">
                                    <TabsContent value="properties" className="mt-0 space-y-6">
                                        {isFixed ? (
                                            <div className="bg-muted/30 p-8 rounded-lg border-2 border-dashed flex flex-col items-center justify-center text-center space-y-3">
                                                <Lock className="h-10 w-10 text-muted-foreground/40" />
                                                <div className="space-y-1">
                                                    <h3 className="font-semibold text-lg">Fixed System Step</h3>
                                                    <p className="text-sm text-muted-foreground max-w-sm">
                                                        This is a core workflow step ({activeStep?.name}) that cannot be renamed, rekeyed, or deleted.
                                                        Its behavior is defined by the system.
                                                    </p>
                                                </div>
                                            </div>
                                        ) : (
                                            <>
                                                <div className="grid grid-cols-2 gap-4">
                                                    <div className="space-y-2">
                                                        <Label>Step Key (Unique)</Label>
                                                        <Input
                                                            value={activeStep?.stepKey}
                                                            onChange={(e) => updateStep(activeStepIndex, { stepKey: e.target.value.toUpperCase().replace(/\s/g, '_') })}
                                                            className="font-mono"
                                                            disabled={isPublished}
                                                        />
                                                    </div>
                                                    <div className="space-y-2">
                                                        <Label>Step Name</Label>
                                                        <Input
                                                            value={activeStep?.name}
                                                            onChange={(e) => updateStep(activeStepIndex, { name: e.target.value })}
                                                            disabled={isPublished}
                                                        />
                                                    </div>
                                                </div>

                                                <div className="pt-4 border-t space-y-4">
                                                    <h4 className="text-sm font-semibold text-muted-foreground">Transitions</h4>

                                                    <div className="grid grid-cols-2 gap-4">
                                                        <div className="p-3 bg-muted/30 rounded-lg border">
                                                            <div className="text-xs font-medium text-muted-foreground mb-1">ON APPROVE &rarr;</div>
                                                            <div className="font-mono text-sm font-bold text-green-600 dark:text-green-400">
                                                                {activeStepIndex !== null && activeStepIndex < steps.length - 1
                                                                    ? steps[activeStepIndex + 1].name
                                                                    : <span className="text-muted-foreground italic">None (End)</span>}
                                                            </div>
                                                        </div>
                                                        {(activeStep?.stepKey != "END") && (activeStep?.stepKey != "SUBMISSION")
                                                            ? <div className="p-3 bg-muted/30 rounded-lg border">
                                                                <div className="text-xs font-medium text-muted-foreground mb-1">ON REJECT &rarr;</div>
                                                                <div className="font-mono text-sm font-bold text-red-600 dark:text-red-400">
                                                                    {(() => {
                                                                        const mode = rejectionModes?.data?.find((m: ReferenceItem) => m.id === meta.rejectionModeId)?.code;

                                                                        if (mode === 'START') {
                                                                            return steps[0]?.name || 'SUBMISSION';
                                                                        } else if (mode === 'PREVIOUS') {
                                                                            return activeStepIndex > 0 ? steps[activeStepIndex - 1].name : 'SUBMISSION';
                                                                        }

                                                                        return <span className="text-muted-foreground italic">Unknown Mode</span>;
                                                                    })()}
                                                                </div>
                                                            </div>
                                                            : <div></div>
                                                        }
                                                    </div>
                                                </div>

                                                {activeStep && (
                                                    <div className="pt-8 border-t">
                                                        <Button
                                                            variant="destructive"
                                                            size="sm"
                                                            onClick={() => deleteStep(activeStepIndex)}
                                                            disabled={isPublished}
                                                        >
                                                            <Trash2 className="mr-2 h-4 w-4" /> Delete Step
                                                        </Button>
                                                    </div>
                                                )}
                                            </>
                                        )}
                                    </TabsContent>

                                    <TabsContent value="assignment" className="mt-0 h-full">
                                        {activeStepIndex !== null ? (
                                            isFixed ? (
                                                <div className="bg-muted/30 p-8 rounded-lg border-2 border-dashed flex flex-col items-center justify-center text-center space-y-3 mx-auto max-w-2xl">
                                                    <UserCheck className="h-10 w-10 text-muted-foreground/40" />
                                                    <div className="space-y-1">
                                                        <h3 className="font-semibold text-lg">System Assignment</h3>
                                                        <p className="text-sm text-muted-foreground max-w-sm">
                                                            Assignment for this core step is managed automatically by the system or follows global workflow settings.
                                                        </p>
                                                    </div>
                                                </div>
                                            ) : (
                                                <div className="bg-muted/30 p-6 rounded-lg border space-y-6 max-w-2xl mx-auto">
                                                    <div className="space-y-1 pb-4 border-b">
                                                        <h4 className="font-semibold text-lg">Step Assignment: {activeStep?.name}</h4>
                                                        <p className="text-sm text-muted-foreground">Configure who is responsible for completing this step.</p>
                                                    </div>

                                                    <div className="grid gap-6">
                                                        <div className="space-y-3">
                                                            <Label className="text-base">Assignment Mode</Label>
                                                            <Select
                                                                value={activeStep?.rule?.assignmentModeCode || 'SYSTEM'}
                                                                onValueChange={(val) => {
                                                                    updateStep(activeStepIndex, {
                                                                        rule: {
                                                                            ...(activeStep?.rule || { minApprovers: 1, requireAll: true }),
                                                                            assignmentModeCode: val,
                                                                            allowRequesterSelect: true
                                                                        }
                                                                    });
                                                                }}
                                                            >
                                                                <SelectTrigger className="h-11">
                                                                    <SelectValue placeholder="Select mode" />
                                                                </SelectTrigger>
                                                                <SelectContent>
                                                                    {assignmentModes?.data?.map((mode: ReferenceItem) => (
                                                                        <SelectItem key={mode.id} value={mode.code}>
                                                                            {mode.name}
                                                                        </SelectItem>
                                                                    ))}
                                                                </SelectContent>
                                                            </Select>
                                                        </div>

                                                        {(activeStep?.rule?.assignmentModeCode === 'ROLE' || activeStep?.rule?.assignmentModeCode === 'ROLE_DEPT') && (
                                                            <div className="space-y-3 animate-in fade-in slide-in-from-top-1 duration-200">
                                                                <Label className="text-base">Target Role</Label>
                                                                <Select
                                                                    value={activeStep?.rule?.roleId?.toString() || ""}
                                                                    onValueChange={(val) => {
                                                                        const role = roles?.data?.find(r => r.roleId === parseInt(val));
                                                                        updateStep(activeStepIndex, {
                                                                            rule: {
                                                                                ...(activeStep?.rule || { assignmentModeCode: 'ROLE', minApprovers: 1, requireAll: true }),
                                                                                roleId: parseInt(val),
                                                                                roleName: role?.name,
                                                                                allowRequesterSelect: true
                                                                            }
                                                                        });
                                                                    }}
                                                                >
                                                                    <SelectTrigger className="h-11">
                                                                        <SelectValue placeholder="Select role" />
                                                                    </SelectTrigger>
                                                                    <SelectContent>
                                                                        {roles?.data?.map((role) => (
                                                                            <SelectItem key={role.roleId} value={role.roleId.toString()}>
                                                                                {role.name}
                                                                            </SelectItem>
                                                                        ))}
                                                                    </SelectContent>
                                                                </Select>
                                                            </div>
                                                        )}

                                                        {(activeStep?.rule?.assignmentModeCode === 'DEPT' || activeStep?.rule?.assignmentModeCode === 'ROLE_DEPT') && (
                                                            <div className="space-y-3 animate-in fade-in slide-in-from-top-1 duration-200">
                                                                <Label className="text-base">Target Department</Label>
                                                                <Select
                                                                    value={activeStep?.rule?.departmentId?.toString() || ""}
                                                                    onValueChange={(val) => {
                                                                        const dept = departments?.find(d => d.departmentId === parseInt(val));
                                                                        updateStep(activeStepIndex, {
                                                                            rule: {
                                                                                ...(activeStep?.rule || { assignmentModeCode: 'DEPT', minApprovers: 1, requireAll: true }),
                                                                                departmentId: parseInt(val),
                                                                                departmentName: dept?.name,
                                                                                allowRequesterSelect: true
                                                                            }
                                                                        });
                                                                    }}
                                                                >
                                                                    <SelectTrigger className="h-11">
                                                                        <SelectValue placeholder="Select department" />
                                                                    </SelectTrigger>
                                                                    <SelectContent>
                                                                        {departments?.map((dept) => (
                                                                            <SelectItem key={dept.departmentId} value={dept.departmentId.toString()}>
                                                                                {dept.name}
                                                                            </SelectItem>
                                                                        ))}
                                                                    </SelectContent>
                                                                </Select>
                                                            </div>
                                                        )}

                                                        <div className="pt-4 border-t opacity-50 px-1">
                                                            <span className="text-xs text-muted-foreground flex items-center gap-2">
                                                                <div className="h-1.5 w-1.5 rounded-full bg-primary" />
                                                                Individual assignee selection is always enabled for this workflow.
                                                            </span>
                                                        </div>
                                                    </div>
                                                </div>
                                            )
                                        ) : (
                                            <div className="h-full flex flex-col items-center justify-center text-muted-foreground p-8 text-center border-2 border-dashed rounded-lg">
                                                <GitBranch className="h-12 w-12 mb-4 opacity-20" />
                                                <h3 className="font-medium text-lg text-foreground">No Step Selected</h3>
                                                <p className="max-w-[250px]">Select a step from the list on the left to configure its assignment rules.</p>
                                            </div>
                                        )}
                                    </TabsContent>

                                    <TabsContent value="visual" className="mt-0 h-full">
                                        {template && (() => {
                                            const mode = rejectionModes?.data?.find((m: ReferenceItem) => m.id === meta.rejectionModeId)?.code || 'START';
                                            return <FlowDiagram template={template} customSteps={steps} rejectionMode={mode} />;
                                        })()}
                                    </TabsContent>
                                </div>
                            </ScrollArea>
                        </Tabs>
                    )
                    }
                </div >
            </div >
        </div >
    );
}
