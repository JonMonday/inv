'use client';

import { useState } from 'react';
import { useTasks, Task } from '@/hooks/useTasks';
import { Skeleton } from '@/components/ui/skeleton';
import { Badge } from '@/components/ui/badge';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { ClipboardList, Clock } from 'lucide-react';
import { cn } from '@/lib/utils';
import { TaskDrawer } from './task-drawer';
import { PaginationControls } from '@/components/ui/pagination-controls';

export default function TasksPage() {
    const [page, setPage] = useState(1);
    const { tasksQuery } = useTasks({ pageNumber: page, pageSize: 20 });
    const [selectedTask, setSelectedTask] = useState<Task | null>(null);

    const backlogStatuses = ['PENDING', 'AVAILABLE'];
    const backlog = tasksQuery.data?.data?.filter(t => backlogStatuses.includes(t.status)) || [];
    const inProgress = tasksQuery.data?.data?.filter(t => t.status === 'CLAIMED') || [];

    const totalPages = tasksQuery.data?.totalPages || 0;
    const totalRecords = tasksQuery.data?.totalRecords || 0;

    if (tasksQuery.isLoading) {
        return (
            <div className="space-y-6">
                <div className="space-y-2">
                    <Skeleton className="h-4 w-32" />
                    <Skeleton className="h-12 w-full" />
                    <Skeleton className="h-12 w-full" />
                </div>
                <div className="space-y-2">
                    <Skeleton className="h-4 w-32" />
                    <Skeleton className="h-12 w-full" />
                    <Skeleton className="h-12 w-full" />
                </div>
            </div>
        );
    }

    return (
        <div className="space-y-8">
            <TaskSection
                title="In Progress"
                count={inProgress.length}
                tasks={inProgress}
                onSelect={setSelectedTask}
            />
            <TaskSection
                title="Backlog"
                count={backlog.length}
                tasks={backlog}
                onSelect={setSelectedTask}
            />

            <TaskDrawer
                task={selectedTask}
                isOpen={!!selectedTask}
                onClose={() => setSelectedTask(null)}
            />

            <PaginationControls
                pageNumber={page}
                totalPages={totalPages}
                totalRecords={totalRecords}
                onPageChange={setPage}
                pageSize={20}
            />
        </div>
    );
}

function TaskSection({
    title,
    count,
    tasks,
    onSelect
}: {
    title: string;
    count: number;
    tasks: Task[];
    onSelect: (task: Task) => void
}) {
    return (
        <div className="space-y-3">
            <div className="flex items-center gap-2 px-1">
                <h2 className="text-sm font-semibold">{title}</h2>
                <Badge variant="secondary" className="h-5 px-1.5 text-[10px] font-bold">
                    {count}
                </Badge>
            </div>
            <div className="rounded-lg border bg-card text-card-foreground shadow-sm overflow-hidden">
                {tasks.length === 0 ? (
                    <div className="p-8 text-center text-sm text-muted-foreground">
                        No tasks in this section
                    </div>
                ) : (
                    <div className="divide-y">
                        {tasks.map((task) => (
                            <div
                                key={task.id}
                                className="group flex items-center justify-between p-4 transition-colors hover:bg-accent/50 cursor-pointer"
                                onClick={() => onSelect(task)}
                            >
                                <div className="flex items-center gap-4">
                                    <div className={cn(
                                        "flex h-8 w-8 items-center justify-center rounded-full bg-background border",
                                        task.status === 'CLAIMED' ? "text-primary border-primary/20" : "text-muted-foreground"
                                    )}>
                                        {task.status === 'CLAIMED' ? <Clock className="h-4 w-4" /> : <ClipboardList className="h-4 w-4" />}
                                    </div>
                                    <div>
                                        <div className="flex items-center gap-2">
                                            <span className="text-sm font-medium">{task.title}</span>
                                            <Badge variant="outline" className="h-4 px-1 text-[9px] uppercase font-bold">
                                                {task.stepName}
                                            </Badge>
                                        </div>
                                        <p className="text-xs text-muted-foreground line-clamp-1">{task.description}</p>
                                    </div>
                                </div>
                                <div className="flex items-center gap-4">
                                    <span className="text-xs text-muted-foreground">REQ-{task.requestId}</span>
                                    <Avatar className="h-6 w-6 border-2 border-background">
                                        <AvatarFallback className="text-[10px] bg-muted font-bold">
                                            {task.claimedByUserName ? task.claimedByUserName[0] : '?'}
                                        </AvatarFallback>
                                    </Avatar>
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
}
