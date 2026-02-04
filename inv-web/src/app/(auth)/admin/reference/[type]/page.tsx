'use client';

import { useReferenceList } from '@/hooks/useReference';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table';
// Unused import removed

export default function ReferenceTypePage({ params }: { params: { type: string } }) {
    // Note: params are passed as props in Next.js App Router for dynamic routes
    const type = params.type;
    const { data: items, isLoading } = useReferenceList(type);

    // Format title: "inventory-request-status" -> "Inventory Request Status"
    const title = type
        .split('-')
        .map(word => word.charAt(0).toUpperCase() + word.slice(1))
        .join(' ');

    if (isLoading) {
        return <div>Loading reference data...</div>;
    }

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight">{title}</h1>
                    <p className="text-sm text-muted-foreground">System reference values for {title}</p>
                </div>
            </div>

            <Card>
                <CardHeader>
                    <CardTitle>Values</CardTitle>
                    <CardDescription>
                        Read-only list of configured values.
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead className="w-[100px]">ID</TableHead>
                                <TableHead>Code</TableHead>
                                <TableHead>Name</TableHead>
                                <TableHead>Status</TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {items?.map((item) => (
                                <TableRow key={item.id || item.code}>
                                    <TableCell className="font-mono text-xs">{item.id}</TableCell>
                                    <TableCell>
                                        <Badge variant="outline" className="font-mono">
                                            {item.code}
                                        </Badge>
                                    </TableCell>
                                    <TableCell className="font-medium">{item.name}</TableCell>
                                    <TableCell>
                                        <Badge variant={item.isActiveOrTerminal ? "secondary" : "default"} className="text-xs">
                                            {/* Logic matches ReferenceController: IsActive or IsTerminal. 
                                                For status tables, IsTerminal usually means "Closed/Final".
                                                For type tables, IsActive means "Active". 
                                                Simple display generic logic: */}
                                            {type.includes('status')
                                                ? (item.isActiveOrTerminal ? 'Terminal' : 'Open')
                                                : (item.isActiveOrTerminal ? 'Active' : 'Inactive')
                                            }
                                        </Badge>
                                    </TableCell>
                                </TableRow>
                            ))}
                            {(!items || items.length === 0) && (
                                <TableRow>
                                    <TableCell colSpan={4} className="text-center text-muted-foreground py-4">
                                        No values found.
                                    </TableCell>
                                </TableRow>
                            )}
                        </TableBody>
                    </Table>
                </CardContent>
            </Card>
        </div>
    );
}
