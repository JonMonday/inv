'use client';

import React, { useMemo, useState } from 'react';
import { WorkflowStep, WorkflowTransition, WorkflowTemplate } from '@/hooks/useAdmin';
import { cn } from '@/lib/utils';
import {
    Play,
    CheckCircle2,
    User,
    Users,
    Building2,
    FileText,
    XCircle,
    ArrowDown
} from 'lucide-react';

interface WorkflowFlowViewProps {
    template: WorkflowTemplate;
}

export function WorkflowFlowView({ template }: WorkflowFlowViewProps) {
    const { steps = [], transitions = [] } = template;
    const [hoveredTransition, setHoveredTransition] = useState<number | null>(null);

    const layout = useMemo(() => {
        if (!steps.length) return { rows: [], connections: [], nodePositions: new Map() };

        const stepMap = new Map<string, WorkflowStep>();
        steps.forEach(s => stepMap.set(s.stepKey, s));

        // Group by Sequence No for a predictable 'top-to-bottom' logical flow
        const rowMap: Map<number, WorkflowStep[]> = new Map();
        const sortedSequences = Array.from(new Set(steps.map(s => s.sequenceNo))).sort((a, b) => a - b);
        const sequenceToRow = new Map<number, number>();
        sortedSequences.forEach((seq, idx) => sequenceToRow.set(seq, idx));

        steps.forEach(s => {
            const rowIdx = sequenceToRow.get(s.sequenceNo) ?? 0;
            const list = rowMap.get(rowIdx) || [];
            list.push(s);
            rowMap.set(rowIdx, list);
        });

        const sortedRowIndices = Array.from(rowMap.keys()).sort((a, b) => a - b);
        const rows = sortedRowIndices.map(r => rowMap.get(r)!);

        // Calculate positions
        const nodePositions = new Map<string, { x: number, y: number }>();
        const rowHeight = 220;
        const colWidth = 280;
        const viewWidth = Math.max(1000, ...rows.map(r => r.length * colWidth));

        rows.forEach((row, rowIdx) => {
            const rowXStart = (viewWidth - (row.length * colWidth)) / 2;
            row.forEach((step, colIdx) => {
                nodePositions.set(step.stepKey, {
                    x: rowXStart + colIdx * colWidth + colWidth / 2,
                    y: rowIdx * rowHeight + 100
                });
            });
        });

        // Map transitions with orthogonal path logic
        const connections = transitions.map((t, idx) => {
            const from = nodePositions.get(t.fromStepKey);
            const to = nodePositions.get(t.toStepKey);
            if (!from || !to) return null;

            const isBackwards = to.y <= from.y;
            const action = t.actionCode.toUpperCase();
            const isNegative = action.includes('REJECT') || action.includes('CANCEL') || action.includes('FAIL');
            const isPositive = action.includes('APPROVE') || action.includes('SUBMIT') || action.includes('START');

            return {
                id: t.workflowTransitionId,
                from,
                to,
                label: t.actionCode,
                isBackwards,
                isNegative,
                isPositive,
                offset: (idx % 3 - 1) * 25
            };
        }).filter(c => c !== null) as any[];

        return { rows, connections, nodePositions, viewWidth, totalHeight: rows.length * rowHeight + 200 };
    }, [steps, transitions]);

    if (!steps.length) {
        return (
            <div className="flex flex-col items-center justify-center h-[500px] text-zinc-400">
                <p>No steps defined for this workflow.</p>
            </div>
        );
    }

    return (
        <div className="relative w-full h-[70vh] bg-[#f8f9fa] overflow-auto select-none border border-zinc-200 rounded-xl">
            {/* Dot Grid Background */}
            <div className="absolute inset-0 pointer-events-none opacity-[0.4]"
                style={{ backgroundImage: 'radial-gradient(#d1d5db 1px, transparent 1px)', backgroundSize: '24px 24px' }} />

            <div className="p-12 min-w-[max-content]" style={{ width: layout.viewWidth, height: layout.totalHeight }}>
                <div className="relative">
                    {/* SVG Connector Layer - Orthogonal Manhattan Paths */}
                    <svg className="absolute inset-0 w-full h-full pointer-events-none overflow-visible">
                        <defs>
                            <marker id="arrowhead-gray" markerWidth="8" markerHeight="8" refX="8" refY="4" orient="auto">
                                <path d="M0,0 L8,4 L0,8 Z" fill="#94a3b8" />
                            </marker>
                            <marker id="arrowhead-green" markerWidth="8" markerHeight="8" refX="8" refY="4" orient="auto">
                                <path d="M0,0 L8,4 L0,8 Z" fill="#22c55e" />
                            </marker>
                            <marker id="arrowhead-red" markerWidth="8" markerHeight="8" refX="8" refY="4" orient="auto">
                                <path d="M0,0 L8,4 L0,8 Z" fill="#ef4444" />
                            </marker>
                        </defs>
                        {layout.connections.map((conn) => {
                            const isBackwards = conn.isBackwards;
                            const startX = conn.from.x;
                            const startY = conn.from.y + 40; // Bottom of card
                            const endX = conn.to.x;
                            const endY = conn.to.y - 40; // Top of card

                            const isHovered = hoveredTransition === conn.id;
                            const color = conn.isNegative ? '#ef4444' : (conn.isPositive ? '#22c55e' : '#94a3b8');
                            const marker = conn.isNegative ? 'url(#arrowhead-red)' : (conn.isPositive ? 'url(#arrowhead-green)' : 'url(#arrowhead-gray)');

                            // Orthogonal path logic
                            let d = "";
                            if (!isBackwards) {
                                // Straight down or with slight offset
                                const midY = startY + (endY - startY) / 2 + conn.offset;
                                d = `M ${startX} ${startY} L ${startX} ${midY} L ${endX} ${midY} L ${endX} ${endY}`;
                            } else {
                                // Loop around for backwards transition
                                const loopX = startX + 180 + conn.offset;
                                const midY = endY + (startY - endY) / 2;
                                d = `M ${startX + 100} ${startY - 40} L ${loopX} ${startY - 40} L ${loopX} ${endY + 20} L ${endX + 100} ${endY + 20}`;
                            }

                            return (
                                <g
                                    key={conn.id}
                                    className="pointer-events-auto cursor-pointer"
                                    onMouseEnter={() => setHoveredTransition(conn.id)}
                                    onMouseLeave={() => setHoveredTransition(null)}
                                    style={{ opacity: hoveredTransition ? (isHovered ? 1 : 0.1) : 0.8 }}
                                >
                                    <path
                                        d={d}
                                        fill="none"
                                        stroke={color}
                                        strokeWidth={isHovered ? 2.5 : 1.5}
                                        markerEnd={marker}
                                        strokeDasharray={isBackwards ? "5 5" : "none"}
                                        className="transition-all duration-300"
                                    />

                                    {/* Action Label */}
                                    <foreignObject
                                        x={isBackwards ? (startX + 200) : (startX + endX) / 2 - 40}
                                        y={isBackwards ? (startY + endY) / 2 : (startY + endY) / 2 - 10 + conn.offset}
                                        width="80" height="24"
                                    >
                                        <div className="flex justify-center">
                                            <span className={cn(
                                                "px-2 py-0.5 rounded border text-[8px] font-bold uppercase transition-all duration-300 shadow-sm",
                                                isHovered ? "bg-zinc-800 text-white border-zinc-800" : "bg-white border-zinc-200 text-zinc-500",
                                                isHovered && conn.isNegative && "bg-red-500 border-red-500 text-white",
                                                isHovered && conn.isPositive && "bg-green-500 border-green-500 text-white"
                                            )}>
                                                {conn.label}
                                            </span>
                                        </div>
                                    </foreignObject>
                                </g>
                            );
                        })}
                    </svg>

                    {/* Nodes Layer - Vertical Rows */}
                    <div className="relative flex flex-col gap-[140px]">
                        {layout.rows.map((row, rowIdx) => (
                            <div key={rowIdx} className="flex justify-center" style={{ gap: 80 }}>
                                {row.map((step) => {
                                    const isStart = step.stepTypeCode === 'START';
                                    const isEnd = step.stepTypeCode === 'END' || step.stepTypeCode === 'CANCELLED' || step.stepTypeCode === 'COMPLETED';
                                    const rule = step.rule;

                                    return (
                                        <div
                                            key={step.stepKey}
                                            className="relative flex flex-col items-center"
                                            style={{ width: 200 }}
                                        >
                                            {/* Step Card */}
                                            <div
                                                className={cn(
                                                    "w-[190px] p-4 bg-white border border-zinc-200 rounded-lg shadow-[0_2px_10px_-4px_rgba(0,0,0,0.1)] transition-all duration-300",
                                                    "hover:shadow-lg hover:-translate-y-1 z-10",
                                                    isStart && "border-l-4 border-l-green-500",
                                                    isEnd && "border-l-4 border-l-zinc-400 bg-zinc-50"
                                                )}
                                            >
                                                <div className="flex items-center gap-3 mb-2">
                                                    <div className={cn(
                                                        "h-8 w-8 rounded-md flex items-center justify-center",
                                                        isStart ? "bg-green-100 text-green-600" :
                                                            isEnd ? "bg-zinc-100 text-zinc-500" :
                                                                "bg-blue-50 text-blue-600"
                                                    )}>
                                                        {isStart ? <Play className="h-4 w-4" /> :
                                                            isEnd ? <CheckCircle2 className="h-4 w-4" /> :
                                                                <FileText className="h-4 w-4" />}
                                                    </div>
                                                    <div className="flex flex-col min-w-0">
                                                        <span className="text-[11px] font-bold text-zinc-900 truncate">{step.name}</span>
                                                        <span className="text-[8px] font-mono text-zinc-400 uppercase tracking-tighter">{step.stepKey}</span>
                                                    </div>
                                                </div>

                                                {/* Assignment Data */}
                                                {!isEnd && (
                                                    <div className="mt-3 pt-3 border-t border-zinc-100">
                                                        <div className="flex items-center gap-2 text-[9px] text-zinc-500">
                                                            {rule?.assignmentModeCode === 'SPECIFIC_ROLE' ? (
                                                                <><User className="h-3 w-3 text-zinc-300" /> <span className="truncate">{rule.roleName || 'Role Assignment'}</span></>
                                                            ) : rule?.assignmentModeCode === 'DEPARTMENT_HEAD' ? (
                                                                <><Building2 className="h-3 w-3 text-zinc-300" /> <span className="truncate">Dept Head</span></>
                                                            ) : rule?.assignmentModeCode === 'REQUESTER' ? (
                                                                <><Users className="h-3 w-3 text-zinc-300" /> <span>Requester</span></>
                                                            ) : (
                                                                <div className="h-3 w-3 rounded-full bg-zinc-100" />
                                                            )}
                                                        </div>
                                                    </div>
                                                )}
                                            </div>

                                            {/* Visual Connector Dot Top/Bottom */}
                                            <div className="absolute -top-1.5 h-3 w-3 rounded-full border-2 border-zinc-200 bg-white z-20" />
                                            <div className="absolute -bottom-1.5 h-3 w-3 rounded-full border-2 border-zinc-200 bg-white z-20" />
                                        </div>
                                    );
                                })}
                            </div>
                        ))}
                    </div>
                </div>
            </div>

            {/* Legend Overlay */}
            <div className="absolute top-6 left-6 flex flex-col gap-1 p-3 bg-white/80 backdrop-blur-sm border border-zinc-200 rounded-lg shadow-sm z-30">
                <h3 className="text-[10px] font-bold text-zinc-500 uppercase tracking-widest mb-1">Logic Key</h3>
                <div className="flex items-center gap-2 text-[9px] font-medium text-zinc-600">
                    <div className="h-2 w-2 rounded-full bg-green-500" /> Progress (Submission/Approval)
                </div>
                <div className="flex items-center gap-2 text-[9px] font-medium text-zinc-600">
                    <div className="h-2 w-2 rounded-full bg-red-500" /> Exit (Reject/Cancel)
                </div>
            </div>
        </div>
    );
}
