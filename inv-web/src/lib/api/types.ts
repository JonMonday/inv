export interface ApiResponse<T> {
    success: boolean;
    data: T;
    message?: string;
    correlationId?: string;
}

export interface PagedResponse<T> extends ApiResponse<T> {
    pageNumber: number;
    pageSize: number;
    totalPages: number;
    totalRecords: number;
}

export interface PagedRequest {
    pageNumber?: number;
    pageSize?: number;
    searchTerm?: string;
    sortBy?: string;
    sortDescending?: boolean;
}
export interface ReferenceItem {
    id: number;
    code: string;
    name: string;
    description: string;
    isActive: boolean;
}
