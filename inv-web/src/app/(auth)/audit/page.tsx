'use client';

import { useAuditLogs, AuditLog } from '@/hooks/useAdmin';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow
} from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
    Search,
    Filter,
    User as UserIcon,
    Calendar,
    Eye,
    ArrowRight
} from 'lucide-react';
import { Skeleton } from '@/components/ui/skeleton';
import { useState, useCallback } from 'react';
import { PaginationControls } from '@/components/ui/pagination-controls';
import { debounce } from 'lodash';
import {
    Dialog,
    DialogContent,
    DialogHeader,
    DialogTitle,
    DialogDescription,
} from "@/components/ui/dialog";

export default function AuditLogsPage() {
    const [page, setPage] = useState(1);
    const [searchTerm, setSearchTerm] = useState('');
    const { data: logsData, isLoading } = useAuditLogs({ pageNumber: page, pageSize: 20, searchTerm });
    const [selectedLog, setSelectedLog] = useState<AuditLog | null>(null);

    const logs = logsData?.data || [];
    const totalPages = logsData?.totalPages || 0;
    const totalRecords = logsData?.totalRecords || 0;

    const debouncedSearch = useCallback(
        debounce((term: string) => {
            setSearchTerm(term);
            setPage(1);
        }, 500),
        []
    );

    const formatJson = (json: string) => {
        try {
            return JSON.stringify(JSON.parse(json), null, 2);
        } catch {
            return json || 'N/A';
        }
    };

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight text-foreground">Audit Logs</h1>
                    <p className="text-sm text-muted-foreground">Monitor system-wide activity and structural changes.</p>
                </div>
                <Button variant="outline" size="sm">
                    <Filter className="mr-2 h-4 w-4" /> Filter Range
                </Button>
            </div>

            <div className="flex items-center gap-4">
                <div className="relative flex-1 max-w-sm">
                    <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                    <Input
                        placeholder="Search by Action or User..."
                        className="pl-8"
                        onChange={(e) => debouncedSearch(e.target.value)}
                    />
                </div>
            </div>

            <div className="rounded-md border bg-card overflow-hidden">
                <Table>
                    <TableHeader className="bg-muted/50">
                        <TableRow>
                            <TableHead className="w-[180px]">Timestamp</TableHead>
                            <TableHead>Action</TableHead>
                            <TableHead>Performed By</TableHead>
                            <TableHead className="text-right">Details</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {isLoading ? (
                            Array.from({ length: 15 }).map((_, i) => (
                                <TableRow key={i}>
                                    <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-40" /></TableCell>
                                    <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                                    <TableCell className="text-right"><Skeleton className="h-8 w-8 ml-auto" /></TableCell>
                                </TableRow>
                            ))
                        ) : logs.length === 0 ? (
                            <TableRow>
                                <TableCell colSpan={4} className="h-32 text-center text-muted-foreground">
                                    No audit logs found.
                                </TableCell>
                            </TableRow>
                        ) : (
                            logs.map((log: AuditLog) => (
                                <TableRow key={log.auditLogId} className="group hover:bg-muted/30 transition-colors">
                                    <TableCell className="text-xs text-muted-foreground">
                                        <div className="flex items-center gap-2">
                                            <Calendar className="h-3 w-3" />
                                            {new Date(log.timestamp).toLocaleString()}
                                        </div>
                                    </TableCell>
                                    <TableCell>
                                        <Badge variant="secondary" className="font-mono text-[10px] tracking-tighter uppercase px-2 py-0.5">
                                            {log.action}
                                        </Badge>
                                    </TableCell>
                                    <TableCell>
                                        <div className="flex items-center gap-2 text-sm font-medium">
                                            <UserIcon className="h-3 w-3 text-muted-foreground" />
                                            {log.performedBy}
                                        </div>
                                    </TableCell>
                                    <TableCell className="text-right">
                                        <Button variant="ghost" size="icon" onClick={() => setSelectedLog(log)}>
                                            <Eye className="h-4 w-4" />
                                        </Button>
                                    </TableCell>
                                </TableRow>
                            ))
                        )}
                    </TableBody>
                </Table>
            </div>

            <PaginationControls
                pageNumber={page}
                totalPages={totalPages}
                totalRecords={totalRecords}
                onPageChange={setPage}
                pageSize={20}
            />

            <Dialog open={!!selectedLog} onOpenChange={() => setSelectedLog(null)}>
                <DialogContent className="max-w-2xl">
                    <DialogHeader>
                        <DialogTitle>Audit Log Details</DialogTitle>
                        <DialogDescription>
                            Full change set for action: <span className="font-bold">{selectedLog?.action}</span>
                        </DialogDescription>
                    </DialogHeader>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6 py-4">
                        <div className="space-y-2">
                            <h4 className="text-xs font-bold uppercase text-muted-foreground">Old Value</h4>
                            <pre className="bg-muted p-3 rounded text-[10px] overflow-auto max-h-60 border">
                                {selectedLog ? formatJson(selectedLog.oldValue) : 'N/A'}
                            </pre>
                        </div>
                        <div className="space-y-2">
                            <h4 className="text-xs font-bold uppercase text-muted-foreground flex items-center gap-1">
                                New Value <ArrowRight className="h-3 w-3" />
                            </h4>
                            <pre className="bg-primary/5 text-primary p-3 rounded text-[10px] overflow-auto max-h-60 border border-primary/20 font-medium">
                                {selectedLog ? formatJson(selectedLog.newValue) : 'N/A'}
                            </pre>
                        </div>
                    </div>
                </DialogContent>
            </Dialog>
        </div>
    );
}
