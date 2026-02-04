'use client';

import React from 'react';
import { Badge } from '@/components/ui/badge';
import { ArrowRight, Zap } from 'lucide-react';

interface WorkflowStep {
    key: string;
    name: string;
    type: string;
    sequence: number;
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

interface FlowDiagramProps {
    steps: WorkflowStep[];
}

export function FlowDiagram({ steps }: FlowDiagramProps) {
    if (steps.length === 0) {
        return (
            <div className="h-full flex flex-col items-center justify-center text-muted-foreground opacity-30">
                <Zap className="h-16 w-16 mb-4 stroke-1" />
                <p className="text-lg">No steps to visualize</p>
            </div>
        );
    }

    const sortedSteps = [...steps].sort((a, b) => a.sequence - b.sequence);

    return (
        <div className="p-8 h-full overflow-auto bg-muted/10">
            <div className="flex flex-col items-center gap-12 max-w-4xl mx-auto py-10">
                {sortedSteps.map((step, idx) => {
                    return (
                        <React.Fragment key={step.key}>
                            {/* Step Node */}
                            <div className="w-full max-w-sm group">
                                <div className={`
                                    relative p-5 rounded-2xl border-2 transition-all duration-300
                                    ${step.type === 'START' ? 'bg-green-500/10 border-green-500/30 shadow-lg shadow-green-500/5' :
                                        step.type === 'END' ? 'bg-red-500/10 border-red-500/30 shadow-lg shadow-red-500/5' :
                                            'bg-card border-border shadow-md hover:border-primary/50'}
                                `}>
                                    <div className="flex items-center justify-between mb-3">
                                        <Badge variant="outline" className="font-mono text-[9px] px-1.5 h-5">
                                            {step.key}
                                        </Badge>
                                        <span className="text-[9px] uppercase tracking-widest font-bold opacity-40">
                                            {step.type}
                                        </span>
                                    </div>
                                    <h4 className="font-bold text-foreground text-sm mb-1">{step.name}</h4>

                                    {/* Rule Summary */}
                                    <div className="mt-4 pt-4 border-t border-dashed flex items-center gap-2 text-[10px] text-muted-foreground">
                                        <div className="h-1.5 w-1.5 rounded-full bg-primary/40" />
                                        {step.rule.assignmentMode === 1 ? 'Role Based' :
                                            step.rule.assignmentMode === 2 ? 'Dept Based' : 'Requester Dept'}
                                        {step.rule.requireAll && <span className="text-primary font-bold ml-1">(Requires All)</span>}
                                    </div>
                                </div>

                                {/* Transitions from this step */}
                                <div className="mt-4 flex flex-col gap-2 relative">
                                    {step.transitions.map((t, tIdx) => (
                                        <div key={tIdx} className="flex items-center gap-2 animate-in fade-in slide-in-from-left-2 duration-500" style={{ transitionDelay: `${tIdx * 100}ms` }}>
                                            <div className="h-4 w-4 rounded-full bg-primary/10 flex items-center justify-center">
                                                <ArrowRight className="h-2 w-2 text-primary" />
                                            </div>
                                            <div className="px-2 py-0.5 rounded bg-primary/5 border border-primary/10 text-[9px] font-mono font-bold text-primary">
                                                {t.action}
                                            </div>
                                            <div className="flex-1 h-[1px] bg-gradient-to-r from-primary/20 to-transparent" />
                                            <div className="text-[9px] font-bold text-muted-foreground uppercase tracking-tight">
                                                TO {t.to}
                                            </div>
                                        </div>
                                    ))}
                                    {step.transitions.length === 0 && step.type !== 'END' && (
                                        <div className="flex items-center gap-2 text-[10px] text-destructive italic opacity-50">
                                            <Zap className="h-3 w-3" />
                                            No transitions defined
                                        </div>
                                    )}
                                </div>
                            </div>

                            {/* Link to next step (if sequence based) */}
                            {idx < sortedSteps.length - 1 && (
                                <div className="h-12 w-[2px] bg-gradient-to-b from-border to-transparent" />
                            )}
                        </React.Fragment>
                    );
                })}
            </div>
        </div>
    );
}
