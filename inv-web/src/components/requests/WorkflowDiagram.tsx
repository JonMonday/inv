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

export const WorkflowDiagram: React.FC<WorkflowDiagramProps> = ({ steps, transitions }) => {
    const mermaidRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        mermaid.initialize({
            startOnLoad: true,
            theme: 'base',
            themeVariables: {
                primaryColor: '#0f172a',
                primaryTextColor: '#fff',
                primaryBorderColor: '#0f172a',
                lineColor: '#64748b',
                secondaryColor: '#f8fafc',
                tertiaryColor: '#fff',
            },
            securityLevel: 'loose',
            fontFamily: 'Inter, sans-serif'
        });
    }, []);

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
    }, [steps, transitions]);

    const generateMermaidDefinition = (steps: Step[], transitions: Transition[]) => {
        let def = 'graph TD\n';

        // Styling classes
        def += 'classDef completed fill:#0f172a,stroke:#0f172a,color:#fff,stroke-width:2px;\n';
        def += 'classDef active fill:#f1f5f9,stroke:#0f172a,color:#0f172a,stroke-width:3px;\n';
        def += 'classDef pending fill:#fff,stroke:#e2e8f0,color:#64748b,stroke-width:1px,stroke-dasharray: 5 5;\n';

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

            def += `  Step${step.workflowStepId}("${label}")\n`;

            if (isDone) {
                def += `  class Step${step.workflowStepId} completed\n`;
            } else if (isActive) {
                def += `  class Step${step.workflowStepId} active\n`;
            } else {
                def += `  class Step${step.workflowStepId} pending\n`;
            }
        });

        transitions.forEach(t => {
            const safeAction = t.actionType.replace(/"/g, "'");
            def += `  Step${t.fromWorkflowStepId} -->|"${safeAction}"| Step${t.toWorkflowStepId}\n`;
        });

        return def;
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
