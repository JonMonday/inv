'use client';

import React, { useEffect, useRef } from 'react';
import mermaid from 'mermaid';
import { Workflow } from 'lucide-react';

interface Step {
    workflowStepId: number;
    stepName: string;
    stepKey: string;
    status: string;
    rule?: {
        assignmentMode: string;
        roleName?: string;
        departmentName?: string;
        minApprovers: number;
        requireAll: boolean;
    };
}

interface Transition {
    fromWorkflowStepId: number;
    toWorkflowStepId: number;
    actionType: string;
    actionCode: string;
}

interface WorkflowDiagramProps {
    steps: Step[];
    transitions: Transition[];
}

import { useTheme } from 'next-themes';

export const WorkflowDiagram: React.FC<WorkflowDiagramProps> = ({ steps, transitions }) => {
    const mermaidRef = useRef<HTMLDivElement>(null);
    const { resolvedTheme } = useTheme();

    useEffect(() => {
        const isDark = resolvedTheme === 'dark';

        mermaid.initialize({
            startOnLoad: true,
            theme: 'base',
            themeVariables: {
                primaryColor: isDark ? '#1e293b' : '#ffffff',
                primaryTextColor: isDark ? '#f8fafc' : '#0f172a',
                primaryBorderColor: isDark ? '#334155' : '#0f172a',
                lineColor: isDark ? '#94a3b8' : '#64748b',
                secondaryColor: isDark ? '#334155' : '#f8fafc',
                tertiaryColor: isDark ? '#1e293b' : '#fff',
            },
            securityLevel: 'loose',
            fontFamily: 'Inter, sans-serif',
            flowchart: {
                curve: 'basis' // Match builder curve
            }
        });
    }, [resolvedTheme]);

    useEffect(() => {
        if (mermaidRef.current && steps.length > 0) {
            // Clear previous content
            mermaidRef.current.innerHTML = '<div class="flex items-center gap-2 text-muted-foreground animate-pulse"><small>Rendering Diagram...</small></div>';

            try {
                const diagramDefinition = generateMermaidDefinition(steps, transitions);
                const id = `mermaid-${Math.random().toString(36).substr(2, 9)}`;

                mermaid.render(id, diagramDefinition).then((result) => {
                    if (mermaidRef.current) {
                        mermaidRef.current.innerHTML = result.svg;
                    }
                }).catch(err => {
                    console.error('Mermaid render error:', err);
                    if (mermaidRef.current) {
                        mermaidRef.current.innerHTML = '<p class="text-[10px] text-destructive italic">Failed to render process diagram.</p>';
                    }
                });
            } catch (err) {
                console.error('Mermaid definition error:', err);
                if (mermaidRef.current) {
                    mermaidRef.current.innerHTML = '<p class="text-[10px] text-destructive italic">Error generating diagram definition.</p>';
                }
            }
        }
    }, [steps, transitions, resolvedTheme]);

    const generateMermaidDefinition = (steps: Step[], transitions: Transition[]) => {
        const isDark = resolvedTheme === 'dark';

        // Define colors based on theme
        const colors = {
            completed: {
                fill: isDark ? '#052e16' : '#0f172a', // Dark green/slate to match builder Start style? Or keep dark blue? Request diagram had dark blue.
                // Let's keep dark blue for completed as per original
                stroke: isDark ? '#1e293b' : '#0f172a',
                color: '#fff'
            },
            active: {
                fill: isDark ? '#1e293b' : '#f1f5f9',
                stroke: isDark ? '#94a3b8' : '#0f172a',
                color: isDark ? '#f8fafc' : '#0f172a'
            },
            pending: {
                fill: isDark ? '#0f172a' : '#fff', // Dark bg for pending
                stroke: isDark ? '#334155' : '#e2e8f0',
                color: isDark ? '#94a3b8' : '#64748b'
            }
        };

        const defLines = [
            'graph TD',
            // Use array join method here too for safety
            `classDef completed fill:${colors.completed.fill},stroke:${colors.completed.stroke},color:${colors.completed.color},stroke-width:2px,rx:5px,ry:5px;`,
            `classDef active fill:${colors.active.fill},stroke:${colors.active.stroke},color:${colors.active.color},stroke-width:2px,rx:5px,ry:5px;`, // Updated width 3->2 to match
            `classDef pending fill:${colors.pending.fill},stroke:${colors.pending.stroke},color:${colors.pending.color},stroke-width:1px,stroke-dasharray: 5 5,rx:5px,ry:5px;`
        ];

        steps.forEach(step => {
            const isDone = step.status === 'COMPLETED';
            const isActive = step.status === 'AVAILABLE' || step.status === 'CLAIMED' || step.status === 'ACTIVE';

            // Sanitize names for mermaid labels
            const safeName = step.stepName.replace(/"/g, "'");
            let label = `<b>${safeName}</b>`;
            if (step.rule) {
                const ruleParts = [];
                if (step.rule.roleName) ruleParts.push(`Role: ${step.rule.roleName.replace(/"/g, "'")}`);
                if (step.rule.departmentName) ruleParts.push(`Dept: ${step.rule.departmentName.replace(/"/g, "'")}`);
                if (ruleParts.length > 0) label += `<br/><small>${ruleParts.join(' | ')}</small>`;
            }

            defLines.push(`  Step${step.workflowStepId}("${label}")`);

            if (isDone) {
                defLines.push(`  class Step${step.workflowStepId} completed`);
            } else if (isActive) {
                defLines.push(`  class Step${step.workflowStepId} active`);
            } else {
                defLines.push(`  class Step${step.workflowStepId} pending`);
            }
        });

        transitions.forEach(t => {
            const safeAction = t.actionType.replace(/"/g, "'");
            defLines.push(`  Step${t.fromWorkflowStepId} -->|"${safeAction}"| Step${t.toWorkflowStepId}`);
        });

        return defLines.join('\n');
    };

    if (steps.length === 0) {
        return (
            <div className="w-full py-12 flex flex-col items-center justify-center bg-muted/5 rounded-xl border border-dashed border-border/50 gap-2">
                <Workflow className="h-8 w-8 text-muted-foreground/20" />
                <p className="text-[11px] font-bold uppercase tracking-widest text-muted-foreground/50">No process diagram available</p>
            </div>
        );
    }

    return (
        <div className="w-full overflow-x-auto py-8 flex justify-center bg-muted/5 rounded-xl border border-dashed border-border/50">
            <div ref={mermaidRef} className="mermaid-container" />
        </div>
    );
};
