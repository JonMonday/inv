'use client';

import React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue
} from '@/components/ui/select';
import { AssignmentOption } from '@/hooks/useAdmin';
import { User, Users, Building2, ShieldCheck, UserCheck } from 'lucide-react';
import { cn } from '@/lib/utils';

interface WorkflowAssignmentsCardProps {
    options: AssignmentOption[] | undefined;
    assignments: Record<number, number[]>;
    onAssignmentChange: (stepId: number, userIds: number[]) => void;
    currentUserDisplayName?: string;
    isLoading?: boolean;
}

export function WorkflowAssignmentsCard({
    options,
    assignments,
    onAssignmentChange,
    currentUserDisplayName,
    isLoading
}: WorkflowAssignmentsCardProps) {
    if (isLoading) {
        return (
            <Card>
                <CardHeader>
                    <CardTitle className="text-sm font-bold">Workflow Assignments</CardTitle>
                    <CardDescription>Loading assignment options...</CardDescription>
                </CardHeader>
                <CardContent className="h-32 flex items-center justify-center">
                    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary" />
                </CardContent>
            </Card>
        );
    }

    if (!options || options.length === 0) {
        return null;
    }

    // Determine the next step (first step after submission)
    const nextStep = options[0]; // Assuming sorted by sequence
    const nextAssignees = assignments[nextStep.workflowStepId] || [];

    let nextStepPreview = "(not selected)";
    if (nextStep.modeCode === 'REQ') {
        nextStepPreview = currentUserDisplayName || "Current User";
    } else if (nextAssignees.length > 0) {
        const selectedUser = nextStep.eligibleUsers.find(u => u.userId === nextAssignees[0]);
        nextStepPreview = selectedUser?.displayName || "(Unknown)";
    }

    return (
        <div className="space-y-4">
            <Card>
                <CardHeader>
                    <div className="flex items-center justify-between">
                        <div>
                            <CardTitle className="text-sm font-bold">Workflow Assignments</CardTitle>
                            <CardDescription className="text-xs">Choose who handles each step of this process</CardDescription>
                        </div>
                        <Badge variant="outline" className="text-[10px] font-bold bg-muted/30">
                            {options.length} {options.length === 1 ? 'Step' : 'Steps'}
                        </Badge>
                    </div>
                </CardHeader>
                <CardContent className="space-y-3">
                    {options.map((opt) => {
                        const isManual = opt.isManual;
                        const isRequester = opt.modeCode === 'REQ';
                        const selectedUserId = assignments[opt.workflowStepId]?.[0];

                        return (
                            <div key={opt.workflowStepId} className="group relative flex items-center justify-between p-3 rounded-lg border bg-card/50 transition-all hover:border-primary/20">
                                <div className="flex flex-col gap-1">
                                    <div className="flex items-center gap-2">
                                        <span className="text-sm font-bold tracking-tight">{opt.name}</span>
                                        {isRequester ? (
                                            <Badge className="bg-blue-500/10 text-blue-500 border-blue-500/20 text-[9px] h-4 uppercase tracking-tighter">
                                                Self
                                            </Badge>
                                        ) : isManual ? (
                                            <Badge className="bg-[#befc5c]/10 text-[#7a9e3b] border-[#befc5c]/20 text-[9px] h-4 uppercase tracking-tighter">
                                                Manual
                                            </Badge>
                                        ) : (
                                            <Badge variant="outline" className="text-[9px] h-4 uppercase tracking-tighter opacity-70">
                                                Auto
                                            </Badge>
                                        )}
                                    </div>
                                    <div className="flex items-center gap-1.5 text-[10px] text-muted-foreground font-medium">
                                        {opt.modeCode === 'SPECIFIC_ROLE' && <ShieldCheck className="h-3 w-3 opacity-50" />}
                                        {opt.modeCode === 'DEPARTMENT_HEAD' && <Building2 className="h-3 w-3 opacity-50" />}
                                        {isRequester && <UserCheck className="h-3 w-3 opacity-50" />}
                                        <span className="capitalize">{opt.assignmentMode}</span>
                                        {(opt.roleName || opt.departmentName) && (
                                            <>
                                                <span className="opacity-30">•</span>
                                                <span className="truncate max-w-[120px]">
                                                    {opt.roleName} {opt.departmentName ? `/ ${opt.departmentName}` : ''}
                                                </span>
                                            </>
                                        )}
                                    </div>
                                </div>

                                <div className="flex items-center gap-2">
                                    {isRequester ? (
                                        <div className="flex items-center gap-2 px-3 py-1.5 rounded-md bg-blue-50 dark:bg-blue-900/10 text-blue-600 dark:text-blue-400 text-xs font-bold border border-blue-100 dark:border-blue-900/30">
                                            <User className="h-3 w-3" />
                                            {currentUserDisplayName || 'Requester'}
                                        </div>
                                    ) : isManual ? (
                                        <div className="w-64">
                                            <Select
                                                value={selectedUserId?.toString() || ""}
                                                onValueChange={(v) => onAssignmentChange(opt.workflowStepId, [Number(v)])}
                                            >
                                                <SelectTrigger className={cn(
                                                    "h-9 text-xs transition-all",
                                                    !selectedUserId && "border-dashed border-red-500/50 bg-red-50/50 dark:bg-red-950/20"
                                                )}>
                                                    <SelectValue placeholder="Select Assignee..." />
                                                </SelectTrigger>
                                                <SelectContent>
                                                    {opt.eligibleUsers.map((u) => (
                                                        <SelectItem key={u.userId} value={u.userId.toString()} className="text-xs">
                                                            <div className="flex flex-col">
                                                                <span className="font-bold">{u.displayName}</span>
                                                                <span className="text-[10px] opacity-60">{u.description}</span>
                                                            </div>
                                                        </SelectItem>
                                                    ))}
                                                    {opt.eligibleUsers.length === 0 && (
                                                        <div className="p-2 text-center text-xs text-muted-foreground italic">
                                                            No eligible users found
                                                        </div>
                                                    )}
                                                </SelectContent>
                                            </Select>
                                        </div>
                                    ) : (
                                        <div className="text-[10px] font-bold text-muted-foreground italic px-3 py-1.5 rounded-md bg-muted/50 border border-transparent">
                                            System will auto-assign
                                        </div>
                                    )}
                                </div>
                            </div>
                        );
                    })}
                </CardContent>
            </Card>

            {/* Next Step Preview */}
            <div className="flex items-center gap-3 p-4 rounded-xl bg-[#befc5c]/10 border border-[#befc5c]/20 shadow-sm animate-in fade-in slide-in-from-bottom-2">
                <div className="h-10 w-10 rounded-full bg-[#befc5c] flex items-center justify-center text-black">
                    <Users className="h-5 w-5" />
                </div>
                <div>
                    <h4 className="text-[10px] font-black uppercase tracking-widest text-[#7a9e3b]">Next Step Preview</h4>
                    <p className="text-sm font-bold text-zinc-800 dark:text-zinc-200">
                        {nextStep.name} will be handled by <span className="text-primary">{nextStepPreview}</span>
                    </p>
                </div>
            </div>
        </div>
    );
}
