import React, { useEffect, useRef, useState } from 'react';
import mermaid from 'mermaid';
import { Zap } from 'lucide-react';
import { WorkflowTemplate } from '@/hooks/useAdmin';
import { useTheme } from 'next-themes';

interface FlowDiagramProps {
    template: WorkflowTemplate;
    customSteps?: any[];
    rejectionMode?: string;
}

export function FlowDiagram({ template, customSteps, rejectionMode = 'START' }: FlowDiagramProps) {
    const mermaidRef = useRef<HTMLDivElement>(null);
    const { resolvedTheme } = useTheme();
    // const [svgContent, setSvgContent] = useState<string>(''); // No longer needed
    // const [error, setError] = useState<string | null>(null); // No longer needed

    // Use customSteps (for builder) or template steps (for viewer)
    const steps = customSteps || template?.steps || [];

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
                curve: 'basis'
            }
        });
    }, [resolvedTheme]);

    useEffect(() => {
        if (mermaidRef.current) {
            // Clear previous content
            mermaidRef.current.innerHTML = '<div class="flex items-center gap-2 text-muted-foreground animate-pulse"><small>Rendering Diagram...</small></div>';

            if (!steps.length) {
                if (mermaidRef.current) mermaidRef.current.innerHTML = '';
                return;
            }

            const renderDiagram = async () => {
                try {
                    const definition = generateMermaidDefinition(steps);
                    const id = `flowDiagram-${Math.random().toString(36).substr(2, 9)}`;
                    const { svg } = await mermaid.render(id, definition);
                    if (mermaidRef.current) {
                        mermaidRef.current.innerHTML = svg;
                    }
                } catch (err) {
                    console.error('Mermaid render error:', err);
                    if (mermaidRef.current) {
                        mermaidRef.current.innerHTML = '<p class="text-[10px] text-destructive italic">Failed to render flow diagram.</p>';
                    }
                }
            };

            renderDiagram();
        }
    }, [steps, rejectionMode, resolvedTheme]); // Re-render when mode changes

    const generateMermaidDefinition = (steps: any[]) => {
        const isDark = resolvedTheme === 'dark';

        // Define colors based on theme
        const colors = {
            default: {
                fill: isDark ? '#1e293b' : '#fff',
                stroke: isDark ? '#94a3b8' : '#0f172a',
                color: isDark ? '#f8fafc' : '#0f172a'
            },
            start: {
                fill: isDark ? '#052e16' : '#0f172a', // Dark green/slate
                stroke: isDark ? '#166534' : '#0f172a',
                color: '#fff'
            },
            end: {
                fill: isDark ? '#450a0a' : '#fef2f2',
                stroke: isDark ? '#991b1b' : '#dc2626',
                color: isDark ? '#fecaca' : '#b91c1c'
            }
        };

        const defLines = [
            'graph TD',
            `classDef flowDefault fill:${colors.default.fill},stroke:${colors.default.stroke},color:${colors.default.color},stroke-width:1px,rx:5px,ry:5px;`, // Rounded
            `classDef flowStart fill:${colors.start.fill},stroke:${colors.start.stroke},color:${colors.start.color},stroke-width:2px,rx:5px,ry:5px;`, // Dark fill for start
            `classDef flowEnd fill:${colors.end.fill},stroke:${colors.end.stroke},color:${colors.end.color},stroke-width:2px,rx:5px,ry:5px;`
        ];

        const sortedSteps = [...steps].sort((a, b) => a.sequenceNo - b.sequenceNo);
        // rejectionMode is now a prop, no need to read from template causing TS error

        sortedSteps.forEach((step, index) => {
            // Node Definition
            const safeName = step.name.replace(/"/g, "'");
            let label = `<b>${safeName}</b>`;

            // Add Role/Department info
            if (step.rule) {
                const ruleParts = [];
                if (step.rule.roleName) ruleParts.push(`Role: ${step.rule.roleName.replace(/"/g, "'")}`);
                if (step.rule.departmentName) ruleParts.push(`Dept: ${step.rule.departmentName.replace(/"/g, "'")}`);
                if (ruleParts.length > 0) {
                    label += `<br/><small>${ruleParts.join(' | ')}</small>`;
                }
            }

            defLines.push(`  ${step.stepKey}("${label}")`);

            // Styling
            if (step.stepKey === 'SUBMISSION') {
                defLines.push(`  class ${step.stepKey} flowStart`);
            } else if (['FULFILLMENT', 'CONFIRMATION', 'END'].includes(step.stepKey)) {
                defLines.push(`  class ${step.stepKey} flowEnd`);
            } else {
                defLines.push(`  class ${step.stepKey} flowDefault`);
            }

            // LOGICAL EDGES
            // 1. Approval/Forward Path
            if (index < sortedSteps.length - 1) {
                const nextStep = sortedSteps[index + 1];
                let actionLabel = "Approve";
                if (step.stepKey === 'SUBMISSION') actionLabel = "Submit";

                defLines.push(`  ${step.stepKey} -->|"${actionLabel}"| ${nextStep.stepKey}`);
            }

            // 2. Rejection/Backward Path
            // Only for intermediate approval steps (not Submission, not End)
            const isStart = step.stepKey === 'SUBMISSION';
            const isFinal = step.stepKey === 'END';

            if (!isStart && !isFinal) {
                let rejectTargetKey = '';

                if (rejectionMode === 'START') {
                    rejectTargetKey = sortedSteps[0]?.stepKey;
                } else if (rejectionMode === 'PREVIOUS') {
                    if (index > 0) {
                        rejectTargetKey = sortedSteps[index - 1].stepKey;
                    }
                }

                if (rejectTargetKey) {
                    defLines.push(`  ${step.stepKey} -.->|"Reject"| ${rejectTargetKey}`);
                }
            }
        });

        return defLines.join('\n');
    };

    if (steps.length === 0) {
        return (
            <div className="h-full flex flex-col items-center justify-center text-muted-foreground opacity-30">
                <Zap className="h-16 w-16 mb-4 stroke-1" />
                <p className="text-lg">No steps to visualize</p>
            </div>
        );
    }

    return (
        <div className="w-full h-full overflow-auto py-8 flex justify-center bg-muted/5 rounded-xl border border-dashed border-border/50">
            <div ref={mermaidRef} className="mermaid-container" />
        </div>
    );
}
