'use client';

import { useState, useEffect } from 'react';
import { useParams, useRouter } from 'next/navigation';
import {
    useWorkflowTemplate,
    usePublishWorkflow,
    useRoles,
    useDepartments,
    useReferenceData,
    Role,
    Department
} from '@/hooks/useAdmin';
import { useToast } from '@/hooks/use-toast';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue
} from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { ScrollArea } from '@/components/ui/scroll-area';
import {
    ArrowLeft,
    Plus,
    Save,
    Trash2,
    GitBranch,
    UserCheck,
    Zap,
    Lock
} from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { FlowDiagram } from '@/components/workflow/FlowDiagram';

interface WorkflowStep {
    key: string;
    name: string;
    type: string;
    sequence: number;
    isSystemRequired?: boolean;
    rule: {
        assignmentMode: number;
        roleId: number | null;
        departmentId: number | null;
        minApprovers: number;
        requireAll: boolean;
    };
    transitions: {
        action: string;
        to: string;
    }[];
}

export default function WorkflowBuilderPage() {
    const params = useParams();
    const router = useRouter();
    const { toast } = useToast();
    const code = params.code as string;

    const { data: template, isLoading: templateLoading } = useWorkflowTemplate(code);
    const { data: roles } = useRoles();
    const { data: departments } = useDepartments();
    const { data: stepTypes } = useReferenceData('workflow-step-type');
    const { data: actionTypes } = useReferenceData('workflow-action-type');
    const { data: assignmentModes } = useReferenceData('workflow-assignment-mode');

    const publishMutation = usePublishWorkflow();

    const [steps, setSteps] = useState<WorkflowStep[]>([]);
    const [activeStepIndex, setActiveStepIndex] = useState<number | null>(null);

    useEffect(() => {
        if (template?.steps && template.steps.length > 0) {
            // Map from API format
            const mappedSteps: WorkflowStep[] = template.steps.map((s: any) => {
                // Find transitions for this step
                const stepTrans = template.transitions?.filter((t: any) => t.fromWorkflowStepId === s.workflowStepId) || [];

                return {
                    key: s.stepKey,
                    name: s.name,
                    type: s.stepType || s.stepTypeCode || "REVIEW", // Handle potential DTO structure differences
                    sequence: s.sequenceNo,
                    isSystemRequired: s.isSystemRequired,
                    rule: {
                        assignmentMode: s.rule?.assignmentModeId || 1,
                        roleId: s.rule?.roleId || null,
                        departmentId: s.rule?.departmentId || null,
                        minApprovers: s.rule?.minApprovers || 1,
                        requireAll: s.rule?.requireAll || false
                    },
                    transitions: stepTrans.map((t: any) => {
                        const targetStep = template.steps?.find((ts: any) => ts.workflowStepId === t.toWorkflowStepId);
                        return {
                            action: t.actionCode || t.action?.code,
                            to: targetStep?.stepKey || ""
                        };
                    })
                };
            });
            setSteps(mappedSteps);
        } else if (template?.definitionJson) {
            try {
                const def = JSON.parse(template.definitionJson);
                if (def.steps) setSteps(def.steps);
            } catch (error: unknown) {
                console.error("Failed to parse definition", error);
            }
        }
    }, [template]);

    const activeStep = activeStepIndex !== null ? steps[activeStepIndex] : null;

    // Helper to determine if assignment role/dept selectors should be shown
    const getModeCode = (modeId: number) => {
        if (!assignmentModes?.data) return "";
        return assignmentModes.data.find((m: any) => m.id === modeId)?.code || "";
    };

    const showRoleSelector = (modeId: number) => {
        const code = getModeCode(modeId).toUpperCase();
        return code.includes('ROLE');
    };

    const showDeptSelector = (modeId: number) => {
        const code = getModeCode(modeId).toUpperCase();
        return code.includes('DEPT') && !code.includes('REQ_DEPT');
    };

    const addStep = () => {
        const newStep: WorkflowStep = {
            key: `STEP_${steps.length + 1}`,
            name: "New Step",
            type: "REVIEW",
            sequence: steps.length,
            rule: {
                assignmentMode: 1,
                roleId: null,
                departmentId: null,
                minApprovers: 1,
                requireAll: false
            },
            transitions: []
        };
        setSteps([...steps, newStep]);
        setActiveStepIndex(steps.length);
    };


    const updateStep = (index: number, updates: Partial<WorkflowStep>) => {
        const newSteps = [...steps];
        newSteps[index] = { ...newSteps[index], ...updates };
        setSteps(newSteps);
    };

    const updateRule = (index: number, updates: Partial<WorkflowStep['rule']>) => {
        const newSteps = [...steps];
        newSteps[index] = {
            ...newSteps[index],
            rule: { ...newSteps[index].rule, ...updates }
        };
        setSteps(newSteps);
    };

    const addTransition = (index: number) => {
        const newSteps = [...steps];
        newSteps[index].transitions.push({ action: "APPROVE", to: "" });
        setSteps(newSteps);
    };

    const updateTransition = (stepIndex: number, transIndex: number, updates: Partial<{ action: string, to: string }>) => {
        const newSteps = [...steps];
        newSteps[stepIndex].transitions[transIndex] = { ...newSteps[stepIndex].transitions[transIndex], ...updates };
        setSteps(newSteps);
    };

    const removeTransition = (stepIndex: number, transIndex: number) => {
        const newSteps = [...steps];
        newSteps[stepIndex].transitions.splice(transIndex, 1);
        setSteps(newSteps);
    };

    const deleteStep = (index: number) => {
        const newSteps = steps.filter((_, i) => i !== index);
        setSteps(newSteps);
        setActiveStepIndex(null);
    };

    const handlePublish = async () => {
        try {
            await publishMutation.mutateAsync({
                workflowCode: code,
                definitionJson: JSON.stringify({ steps })
            });
            toast({ title: "Success", description: "Workflow published successfully!" });
        } catch (error: unknown) {
            console.error("Publish error:", error);
            toast({ title: "Error", description: "Failed to publish workflow.", variant: "destructive" });
        }
    };

    if (templateLoading) return <div className="p-8 text-center text-muted-foreground">Loading builder...</div>;

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
                </div>
                <div className="flex items-center gap-2">
                    <Button size="sm" onClick={handlePublish} disabled={publishMutation.isPending}>
                        <Save className="mr-2 h-4 w-4" /> Publish Version
                    </Button>
                </div>
            </div>

            <div className="flex flex-1 overflow-hidden">
                {/* Steps Sidebar */}
                <div className="w-80 border-r bg-muted/30 flex flex-col">
                    <div className="p-4 flex items-center justify-between">
                        <h3 className="text-sm font-bold uppercase tracking-wider text-muted-foreground">Steps</h3>
                        <Button variant="ghost" size="icon" className="h-8 w-8" onClick={addStep}>
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
                            {steps.map((step, idx) => (
                                <button
                                    key={`${step.key}-${idx}`}
                                    onClick={() => setActiveStepIndex(idx)}
                                    className={`w-full text-left p-3 rounded-lg border transition-all ${activeStepIndex === idx
                                        ? 'bg-primary/5 border-primary shadow-sm shadow-primary/10'
                                        : 'bg-card hover:border-muted-foreground/30'
                                        }`}
                                >
                                    <div className="flex items-start justify-between">
                                        <div className="space-y-1">
                                            <div className="flex items-center gap-2">
                                                <Badge variant="outline" className="h-5 px-1 font-mono text-[9px]">
                                                    {step.sequence}
                                                </Badge>
                                                <span className="text-sm font-semibold truncate leading-none">
                                                    {step.name}
                                                </span>
                                                {step.isSystemRequired && (
                                                    <Lock className="h-3 w-3 text-muted-foreground/50" />
                                                )}
                                            </div>
                                            <p className="text-[10px] text-muted-foreground uppercase">{step.type}</p>
                                        </div>
                                        <div className="flex flex-col items-end gap-1">
                                            {step.rule.assignmentMode === 1 && <UserCheck className="h-3 w-3 text-primary" />}
                                            <span className="text-[10px] font-mono opacity-50">{step.key}</span>
                                        </div>
                                    </div>
                                </button>
                            ))}
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
                                    <TabsTrigger value="properties" className="data-[state=active]:bg-primary/5 rounded-none border-b-2 border-transparent data-[state=active]:border-primary">Properties</TabsTrigger>
                                    <TabsTrigger value="assignment" className="data-[state=active]:bg-primary/5 rounded-none border-b-2 border-transparent data-[state=active]:border-primary">Assignment & Rules</TabsTrigger>
                                    <TabsTrigger value="transitions" className="data-[state=active]:bg-primary/5 rounded-none border-b-2 border-transparent data-[state=active]:border-primary">Transitions</TabsTrigger>
                                    <TabsTrigger value="visual" className="data-[state=active]:bg-primary/5 rounded-none border-b-2 border-transparent data-[state=active]:border-primary">Visual Flow</TabsTrigger>
                                </TabsList>
                            </div>

                            <ScrollArea className="flex-1">
                                <div className="p-8 max-w-2xl mx-auto space-y-8 pb-20">
                                    <TabsContent value="properties" className="mt-0 space-y-6">
                                        <div className="grid grid-cols-2 gap-4">
                                            <div className="space-y-2">
                                                <Label>Step Key</Label>
                                                <Input
                                                    value={activeStep?.key}
                                                    onChange={(e) => updateStep(activeStepIndex, { key: e.target.value.toUpperCase().replace(/\s/g, '_') })}
                                                    className="font-mono"
                                                />
                                            </div>
                                            <div className="space-y-2">
                                                <Label>Step Name</Label>
                                                <Input
                                                    value={activeStep?.name}
                                                    onChange={(e) => updateStep(activeStepIndex, { name: e.target.value })}
                                                />
                                            </div>
                                        </div>
                                        <div className="space-y-2">
                                            <Label>Step Type</Label>
                                            <Select
                                                value={activeStep?.type}
                                                onValueChange={(val) => updateStep(activeStepIndex, { type: val })}
                                            >
                                                <SelectTrigger>
                                                    <SelectValue />
                                                </SelectTrigger>
                                                <SelectContent>
                                                    {stepTypes?.data.map((t: any) => (
                                                        <SelectItem key={t.id} value={t.code}>{t.name}</SelectItem>
                                                    ))}
                                                </SelectContent>
                                            </Select>
                                        </div>
                                        <div className="pt-8 border-t">
                                            <Button
                                                variant="destructive"
                                                size="sm"
                                                onClick={() => deleteStep(activeStepIndex)}
                                                disabled={activeStep?.isSystemRequired}
                                            >
                                                <Trash2 className="mr-2 h-4 w-4" /> Delete Step
                                            </Button>
                                        </div>
                                    </TabsContent>

                                    <TabsContent value="assignment" className="mt-0 space-y-6">
                                        <div className="space-y-4">
                                            <div className="space-y-2">
                                                <Label>Assignment Mode</Label>
                                                <Select
                                                    value={activeStep?.rule.assignmentMode.toString()}
                                                    onValueChange={(val) => updateRule(activeStepIndex, { assignmentMode: parseInt(val) })}
                                                >
                                                    <SelectTrigger>
                                                        <SelectValue />
                                                    </SelectTrigger>
                                                    <SelectContent>
                                                        {assignmentModes?.data.map((mode: any) => (
                                                            <SelectItem key={mode.id} value={mode.id.toString()}>{mode.name}</SelectItem>
                                                        ))}
                                                    </SelectContent>
                                                </Select>
                                            </div>

                                            {showRoleSelector(activeStep?.rule.assignmentMode || 0) && (
                                                <div className="space-y-2 animate-in slide-in-from-top-2 duration-300">
                                                    <Label>Assigned Role (Optional)</Label>
                                                    <Select
                                                        value={activeStep?.rule.roleId?.toString() || ""}
                                                        onValueChange={(val) => updateRule(activeStepIndex, { roleId: parseInt(val) })}
                                                    >
                                                        <SelectTrigger>
                                                            <SelectValue placeholder="Select a role..." />
                                                        </SelectTrigger>
                                                        <SelectContent>
                                                            {roles?.data.map((r: Role) => (
                                                                <SelectItem key={r.roleId} value={r.roleId.toString()}>{r.name}</SelectItem>
                                                            ))}
                                                        </SelectContent>
                                                    </Select>
                                                </div>
                                            )}

                                            {showDeptSelector(activeStep?.rule.assignmentMode || 0) && (
                                                <div className="space-y-2 animate-in slide-in-from-top-2 duration-300">
                                                    <Label>Assigned Department</Label>
                                                    <Select
                                                        value={activeStep?.rule.departmentId?.toString() || ""}
                                                        onValueChange={(val) => updateRule(activeStepIndex, { departmentId: parseInt(val) })}
                                                    >
                                                        <SelectTrigger>
                                                            <SelectValue placeholder="Select a department..." />
                                                        </SelectTrigger>
                                                        <SelectContent>
                                                            {departments?.map((d: Department) => (
                                                                <SelectItem key={d.departmentId} value={d.departmentId.toString()}>{d.name}</SelectItem>
                                                            ))}
                                                        </SelectContent>
                                                    </Select>
                                                </div>
                                            )}
                                        </div>
                                    </TabsContent>

                                    <TabsContent value="transitions" className="mt-0 space-y-4">
                                        <div className="flex items-center justify-between">
                                            <h4 className="text-sm font-bold uppercase text-muted-foreground">Flow Logic</h4>
                                            <Button variant="outline" size="sm" onClick={() => addTransition(activeStepIndex)}>
                                                <Plus className="mr-2 h-4 w-4" /> Add Transition
                                            </Button>
                                        </div>

                                        <div className="space-y-3">
                                            {activeStep?.transitions.map((trans, tIdx) => (
                                                <div key={`${activeStep.key}-trans-${tIdx}`} className="flex gap-3 items-end p-4 rounded-lg border bg-muted/20 relative group">
                                                    <div className="grid grid-cols-2 gap-4 flex-1">
                                                        <div className="space-y-2">
                                                            <Label className="text-[10px] uppercase">On Action</Label>
                                                            <Select
                                                                value={trans.action}
                                                                onValueChange={(val) => updateTransition(activeStepIndex, tIdx, { action: val })}
                                                            >
                                                                <SelectTrigger>
                                                                    <SelectValue />
                                                                </SelectTrigger>
                                                                <SelectContent>
                                                                    {actionTypes?.data.map((a: any) => (
                                                                        <SelectItem key={a.id} value={a.code}>{a.name}</SelectItem>
                                                                    ))}
                                                                </SelectContent>
                                                            </Select>
                                                        </div>
                                                        <div className="space-y-2">
                                                            <Label className="text-[10px] uppercase">Transition To</Label>
                                                            <Select
                                                                value={trans.to}
                                                                onValueChange={(val) => updateTransition(activeStepIndex, tIdx, { to: val })}
                                                            >
                                                                <SelectTrigger>
                                                                    <SelectValue placeholder="Select next step..." />
                                                                </SelectTrigger>
                                                                <SelectContent>
                                                                    {steps.filter(s => s.key !== activeStep.key).map(s => (
                                                                        <SelectItem key={s.key} value={s.key}>{s.name} ({s.key})</SelectItem>
                                                                    ))}
                                                                </SelectContent>
                                                            </Select>
                                                        </div>
                                                    </div>
                                                    <Button
                                                        variant="ghost"
                                                        size="icon"
                                                        className="h-9 w-9 text-destructive opacity-0 group-hover:opacity-100 transition-opacity"
                                                        onClick={() => removeTransition(activeStepIndex, tIdx)}
                                                    >
                                                        <Trash2 className="h-4 w-4" />
                                                    </Button>
                                                </div>
                                            ))}
                                            {activeStep?.transitions.length === 0 && (
                                                <p className="text-center py-10 text-xs text-muted-foreground border-2 border-dashed rounded-xl">
                                                    No transitions defined for this step. It will be a terminal point.
                                                </p>
                                            )}
                                        </div>
                                    </TabsContent>

                                    <TabsContent value="visual" className="mt-0 h-full">
                                        <FlowDiagram steps={steps} />
                                    </TabsContent>
                                </div>
                            </ScrollArea>
                        </Tabs>
                    )}
                </div>
            </div>
        </div>
    );
}
