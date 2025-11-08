import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AdminDocumentStats {
  totalDocuments: number;
  pendingDocuments: number;
  approvedDocuments: number;
  rejectedDocuments: number;
  documentsThisWeek: number;
  documentsThisMonth: number;
  verificationCompletionRate: number;
  averageVerificationTime: string;
  totalUsersWithDocuments: number;
  fullyVerifiedUsers: number;
  documentTypeBreakdown: DocumentTypeStats[];
  recentTrends: VerificationTrend[];
}

export interface DocumentTypeStats {
  documentTypeId: number;
  documentTypeName: string;
  category: string;
  totalCount: number;
  pendingCount: number;
  approvedCount: number;
  rejectedCount: number;
  approvalRate: number;
}

export interface VerificationTrend {
  date: string;
  documentsSubmitted: number;
  documentsVerified: number;
  documentsApproved: number;
  documentsRejected: number;
}

export interface AdminUserDocument {
  id: number;
  userId: string;
  documentTypeId: number;
  documentTypeName: string;
  category: string;
  fileName: string;
  originalFileName: string;
  fileSize: number;
  contentType: string;
  verificationStatus: string;
  verificationNotes?: string;
  verifiedDate?: string;
  verifiedBy?: string;
  expiryDate?: string;
  uploadedDate: string;
  isRequired: boolean;
  storagePath: string;
  // Admin-specific properties
  email: string;
  firstName: string;
  lastName: string;
}

export interface DocumentVerificationResult {
  documentId: number;
  success: boolean;
  newStatus?: string;
  message?: string;
  errorMessage?: string;
}

export interface PaginatedDocuments {
  documents: AdminUserDocument[];
  pagination: {
    currentPage: number;
    pageSize: number;
    totalCount?: number;
    totalPages?: number;
  };
}

export interface DocumentVerificationRequest {
  status: string; // 'Approved' or 'Rejected'
  notes?: string;
}

export interface BatchVerificationRequest {
  documentVerifications: DocumentVerificationItem[];
}

export interface DocumentVerificationItem {
  documentId: number;
  status: string;
  notes?: string;
}

export interface BatchVerificationResult {
  totalProcessed: number;
  successCount: number;
  failureCount: number;
  results: DocumentVerificationResult[];
}

export interface DocumentHistory {
  id: number;
  userDocumentId: number;
  previousStatus: string;
  newStatus: string;
  notes?: string;
  verifiedBy: string;
  verifiedDate: string;
  createdDate: string;
}

@Injectable({
  providedIn: 'root'
})
export class DocumentVerificationService {
  private apiUrl = `${environment.apiUrl}/api/admin/document-verification`;
  
  constructor(private http: HttpClient) { }

  // Dashboard and Statistics
  async getDocumentStats(): Promise<AdminDocumentStats> {
    return firstValueFrom(this.http.get<AdminDocumentStats>(`${this.apiUrl}/stats`));
  }

  // Document Management
  async getPendingDocuments(
    documentTypeId?: number, 
    pageSize: number = 50, 
    pageNumber: number = 1
  ): Promise<PaginatedDocuments> {
    let params = new HttpParams()
      .set('pageSize', pageSize.toString())
      .set('pageNumber', pageNumber.toString());
    
    if (documentTypeId) {
      params = params.set('documentTypeId', documentTypeId.toString());
    }

    return firstValueFrom(
      this.http.get<PaginatedDocuments>(`${this.apiUrl}/pending`, { params })
    );
  }

  async getDocumentsByStatus(
    status: string, 
    documentTypeId?: number, 
    pageSize: number = 50, 
    pageNumber: number = 1
  ): Promise<PaginatedDocuments> {
    let params = new HttpParams()
      .set('pageSize', pageSize.toString())
      .set('pageNumber', pageNumber.toString());
    
    if (documentTypeId) {
      params = params.set('documentTypeId', documentTypeId.toString());
    }

    return firstValueFrom(
      this.http.get<PaginatedDocuments>(`${this.apiUrl}/status/${status}`, { params })
    );
  }

  async searchDocuments(
    searchTerm: string, 
    status?: string, 
    pageSize: number = 50, 
    pageNumber: number = 1
  ): Promise<PaginatedDocuments> {
    let params = new HttpParams()
      .set('searchTerm', searchTerm)
      .set('pageSize', pageSize.toString())
      .set('pageNumber', pageNumber.toString());
    
    if (status) {
      params = params.set('status', status);
    }

    return firstValueFrom(
      this.http.get<PaginatedDocuments>(`${this.apiUrl}/search`, { params })
    );
  }

  // Document Verification
  async verifyDocument(
    documentId: number, 
    request: DocumentVerificationRequest
  ): Promise<DocumentVerificationResult> {
    return firstValueFrom(
      this.http.post<DocumentVerificationResult>(`${this.apiUrl}/verify/${documentId}`, request)
    );
  }

  async batchVerifyDocuments(
    request: BatchVerificationRequest
  ): Promise<BatchVerificationResult> {
    return firstValueFrom(
      this.http.post<BatchVerificationResult>(`${this.apiUrl}/verify/batch`, request)
    );
  }

  // Document Operations
  downloadDocument(documentId: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/download/${documentId}`, { 
      responseType: 'blob' 
    });
  }

  async downloadDocumentAsync(documentId: number): Promise<Blob> {
    return firstValueFrom(this.downloadDocument(documentId));
  }

  async getDocumentHistory(documentId: number): Promise<DocumentHistory[]> {
    return firstValueFrom(
      this.http.get<DocumentHistory[]>(`${this.apiUrl}/history/${documentId}`)
    );
  }

  // Utility Methods
  getDocumentTypes(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/document-types`);
  }

  getStatusBadgeClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'pending':
        return 'badge bg-warning text-dark';
      case 'approved':
        return 'badge bg-success';
      case 'rejected':
        return 'badge bg-danger';
      default:
        return 'badge bg-secondary';
    }
  }

  getStatusIcon(status: string): string {
    switch (status?.toLowerCase()) {
      case 'pending':
        return 'fas fa-clock';
      case 'approved':
        return 'fas fa-check-circle';
      case 'rejected':
        return 'fas fa-times-circle';
      default:
        return 'fas fa-question-circle';
    }
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  formatDate(dateString: string): string {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getDocumentTypeIcon(category: string): string {
    switch (category?.toLowerCase()) {
      case 'identity':
        return 'fas fa-id-card';
      case 'address':
        return 'fas fa-home';
      case 'income':
        return 'fas fa-dollar-sign';
      default:
        return 'fas fa-file-alt';
    }
  }
}