import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface DocumentType {
  id: number;
  name: string;
  description: string;
  isRequired: boolean;
  category: string;
  allowedFileTypes: string;
  maxFileSizeMB: number;
  isActive: boolean;
}

export interface UserDocument {
  id: number;
  documentTypeId: number;
  documentTypeName: string;
  fileName: string;
  originalFileName: string;
  fileSize: number;
  contentType: string;
  verificationStatus: string;
  uploadedDate: Date;
  verifiedDate?: Date;
  verifiedBy?: string;
  notes?: string;
  expiryDate?: Date;
}

export interface DocumentUploadResult {
  success: boolean;
  documentId?: number;
  fileName?: string;
  message?: string;
  errorMessage?: string;
}

export interface UserVerificationStatus {
  overallStatus: string;
  completionPercentage: number;
  requiredDocuments: number;
  uploadedDocuments: number;
  verifiedDocuments: number;
  pendingDocuments: number;
  rejectedDocuments: number;
  documentStatuses: Array<{
    documentTypeName: string;
    isRequired: boolean;
    status: string;
    hasDocument: boolean;
  }>;
}

@Injectable({
  providedIn: 'root'
})
export class DocumentService {
  private apiUrl = `${environment.apiUrl}/api/documents`;
  
  constructor(private http: HttpClient) { }
  
  getDocumentTypes(): Observable<DocumentType[]> {
    return this.http.get<DocumentType[]>(`${this.apiUrl}/types`);
  }
  
  getUserDocuments(): Observable<UserDocument[]> {
    return this.http.get<UserDocument[]>(`${this.apiUrl}`);
  }
  
  getUserDocument(id: number): Observable<UserDocument> {
    return this.http.get<UserDocument>(`${this.apiUrl}/${id}`);
  }
  
  uploadDocument(formData: FormData): Observable<DocumentUploadResult> {
    return this.http.post<DocumentUploadResult>(`${this.apiUrl}/upload`, formData);
  }
  
  deleteDocument(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
  
  downloadDocument(id: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/download/${id}`, { 
      responseType: 'blob' 
    });
  }
  
  getVerificationStatus(): Observable<UserVerificationStatus> {
    return this.http.get<UserVerificationStatus>(`${this.apiUrl}/verification-status`);
  }
}