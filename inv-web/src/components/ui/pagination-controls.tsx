import { Button } from "@/components/ui/button";
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from "lucide-react";

interface PaginationControlsProps {
    pageNumber: number;
    totalPages: number;
    totalRecords: number;
    onPageChange: (page: number) => void;
    pageSize?: number;
}

export function PaginationControls({
    pageNumber,
    totalPages,
    totalRecords,
    onPageChange,
    pageSize = 10,
}: PaginationControlsProps) {
    if (totalPages <= 1) return null;

    return (
        <div className="flex items-center justify-between px-2 py-4">
            <div className="text-sm text-muted-foreground">
                Showing <span className="font-medium">{(pageNumber - 1) * pageSize + 1}</span> to{" "}
                <span className="font-medium">
                    {Math.min(pageNumber * pageSize, totalRecords)}
                </span>{" "}
                of <span className="font-medium">{totalRecords}</span> results
            </div>
            <div className="flex items-center space-x-2">
                <Button
                    variant="outline"
                    size="icon"
                    className="h-8 w-8"
                    onClick={() => onPageChange(1)}
                    disabled={pageNumber === 1}
                >
                    <ChevronsLeft className="h-4 w-4" />
                </Button>
                <Button
                    variant="outline"
                    size="icon"
                    className="h-8 w-8"
                    onClick={() => onPageChange(pageNumber - 1)}
                    disabled={pageNumber === 1}
                >
                    <ChevronLeft className="h-4 w-4" />
                </Button>
                <div className="flex items-center justify-center text-sm font-medium">
                    Page {pageNumber} of {totalPages}
                </div>
                <Button
                    variant="outline"
                    size="icon"
                    className="h-8 w-8"
                    onClick={() => onPageChange(pageNumber + 1)}
                    disabled={pageNumber === totalPages}
                >
                    <ChevronRight className="h-4 w-4" />
                </Button>
                <Button
                    variant="outline"
                    size="icon"
                    className="h-8 w-8"
                    onClick={() => onPageChange(totalPages)}
                    disabled={pageNumber === totalPages}
                >
                    <ChevronsRight className="h-4 w-4" />
                </Button>
            </div>
        </div>
    );
}
