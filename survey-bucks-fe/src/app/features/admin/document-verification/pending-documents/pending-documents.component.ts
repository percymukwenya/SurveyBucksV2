import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DocumentVerificationService, AdminUserDocument, PaginatedDocuments } from '../../../../core/services/document-verification.service';

@Component({
  selector: 'app-pending-documents',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './pending-documents.component.html',
  styleUrl: './pending-documents.component.scss'
})
export class PendingDocumentsComponent implements OnInit {
  documents: AdminUserDocument[] = [];
  loading = false;
  error: string | null = null;
  
  // Pagination
  currentPage = 1;
  pageSize = 20;
  totalCount = 0;
  totalPages = 0;
  
  // Filtering
  selectedDocumentType: any = null;
  documentTypes: any[] = [];
  
  // Selection and batch operations
  selectedDocuments = new Set<number>();
  selectAll = false;
  
  // Verification modal
  showVerificationModal = false;
  selectedDocument: AdminUserDocument | null = null;
  verificationAction: 'approve' | 'reject' = 'approve';
  verificationNotes = '';
  
  // Batch verification
  showBatchModal = false;
  batchAction: 'approve' | 'reject' = 'approve';
  batchNotes = '';

  constructor(private documentVerificationService: DocumentVerificationService) { }

  ngOnInit(): void {
    this.loadDocumentTypes();
    this.loadPendingDocuments();
  }

  async loadDocumentTypes(): Promise<void> {
    try {
      this.documentTypes = await this.documentVerificationService.getDocumentTypes().toPromise() || [];
    } catch (error) {
      console.error('Error loading document types:', error);
    }
  }

  async loadPendingDocuments(): Promise<void> {
    this.loading = true;
    this.error = null;

    try {
      const documentTypeId = this.selectedDocumentType ? Number(this.selectedDocumentType) : undefined;
      const result = await this.documentVerificationService.getPendingDocuments(
        documentTypeId,
        this.pageSize,
        this.currentPage
      );

      this.documents = result.documents || [];
      this.totalCount = result.pagination.totalCount || 0;
      this.totalPages = result.pagination.totalPages || 0;
      
      // Clear selections when loading new data
      this.selectedDocuments.clear();
      this.selectAll = false;

    } catch (error: any) {
      this.error = error.message || 'Failed to load pending documents';
      console.error('Error loading pending documents:', error);
    } finally {
      this.loading = false;
    }
  }

  onDocumentTypeFilterChange(): void {
    // Convert string values properly
    if (this.selectedDocumentType === 'null' || this.selectedDocumentType === '') {
      this.selectedDocumentType = null;
    } else if (this.selectedDocumentType && typeof this.selectedDocumentType === 'string') {
      this.selectedDocumentType = parseInt(this.selectedDocumentType);
    }
    this.currentPage = 1;
    this.loadPendingDocuments();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadPendingDocuments();
  }

  toggleDocumentSelection(documentId: number): void {
    if (this.selectedDocuments.has(documentId)) {
      this.selectedDocuments.delete(documentId);
    } else {
      this.selectedDocuments.add(documentId);
    }
    this.updateSelectAllState();
  }

  toggleSelectAll(): void {
    if (this.selectAll) {
      // Deselect all
      this.selectedDocuments.clear();
    } else {
      // Select all current page documents
      this.documents.forEach(doc => this.selectedDocuments.add(doc.id));
    }
    this.selectAll = !this.selectAll;
  }

  updateSelectAllState(): void {
    const visibleDocumentIds = this.documents.map(doc => doc.id);
    this.selectAll = visibleDocumentIds.every(id => this.selectedDocuments.has(id)) && visibleDocumentIds.length > 0;
  }

  openVerificationModal(document: AdminUserDocument, action: 'approve' | 'reject'): void {
    this.selectedDocument = document;
    this.verificationAction = action;
    this.verificationNotes = '';
    this.showVerificationModal = true;
  }

  closeVerificationModal(): void {
    this.showVerificationModal = false;
    this.selectedDocument = null;
    this.verificationNotes = '';
  }

  async verifyDocument(): Promise<void> {
    if (!this.selectedDocument) return;

    const status = this.verificationAction === 'approve' ? 'Approved' : 'Rejected';
    
    if (this.verificationAction === 'reject' && !this.verificationNotes.trim()) {
      alert('Please provide notes when rejecting a document.');
      return;
    }

    this.loading = true;

    try {
      await this.documentVerificationService.verifyDocument(
        this.selectedDocument.id,
        {
          status: status,
          notes: this.verificationNotes
        }
      );

      // Remove the verified document from the list
      this.documents = this.documents.filter(doc => doc.id !== this.selectedDocument!.id);
      this.selectedDocuments.delete(this.selectedDocument.id);
      
      // Update counts
      this.totalCount = Math.max(0, this.totalCount - 1);
      
      this.closeVerificationModal();
      
      // Show success message
      const actionText = this.verificationAction === 'approve' ? 'approved' : 'rejected';
      alert(`Document has been ${actionText} successfully.`);

    } catch (error: any) {
      console.error('Error verifying document:', error);
      alert(error.message || 'Failed to verify document');
    } finally {
      this.loading = false;
    }
  }

  openBatchModal(action: 'approve' | 'reject'): void {
    if (this.selectedDocuments.size === 0) {
      alert('Please select documents to verify.');
      return;
    }

    this.batchAction = action;
    this.batchNotes = '';
    this.showBatchModal = true;
  }

  closeBatchModal(): void {
    this.showBatchModal = false;
    this.batchNotes = '';
  }

  async batchVerifyDocuments(): Promise<void> {
    if (this.selectedDocuments.size === 0) return;

    const status = this.batchAction === 'approve' ? 'Approved' : 'Rejected';
    
    if (this.batchAction === 'reject' && !this.batchNotes.trim()) {
      alert('Please provide notes when rejecting documents.');
      return;
    }

    this.loading = true;

    try {
      const documentVerifications = Array.from(this.selectedDocuments).map(docId => ({
        documentId: docId,
        status: status,
        notes: this.batchNotes
      }));

      const result = await this.documentVerificationService.batchVerifyDocuments({
        documentVerifications: documentVerifications
      });

      // Remove successfully verified documents from the list
      const successfulIds = result.results
        .filter(r => r.success)
        .map(r => r.documentId);

      this.documents = this.documents.filter(doc => !successfulIds.includes(doc.id));
      this.selectedDocuments.clear();
      this.selectAll = false;
      
      // Update counts
      this.totalCount = Math.max(0, this.totalCount - result.successCount);
      
      this.closeBatchModal();
      
      // Show results
      const actionText = this.batchAction === 'approve' ? 'approved' : 'rejected';
      alert(`${result.successCount} document(s) ${actionText} successfully. ${result.failureCount} failed.`);

      // If current page is empty, go to previous page
      if (this.documents.length === 0 && this.currentPage > 1) {
        this.currentPage--;
        await this.loadPendingDocuments();
      }

    } catch (error: any) {
      console.error('Error batch verifying documents:', error);
      alert(error.message || 'Failed to verify documents');
    } finally {
      this.loading = false;
    }
  }

  async downloadDocument(userDoc: AdminUserDocument): Promise<void> {
    try {
      const blob = await this.documentVerificationService.downloadDocumentAsync(userDoc.id);
      
      // Create download link using DOM
      const url = window.URL.createObjectURL(blob);
      const dom = window.document;
      const linkElement = dom.createElement('a');
      linkElement.href = url;
      linkElement.download = userDoc.originalFileName;
      linkElement.style.display = 'none';
      dom.body.appendChild(linkElement);
      linkElement.click();
      dom.body.removeChild(linkElement);
      window.URL.revokeObjectURL(url);
      
    } catch (error: any) {
      console.error('Error downloading document:', error);
      alert(error.message || 'Failed to download document');
    }
  }

  getPages(): number[] {
    const pages: number[] = [];
    const maxVisiblePages = 5;
    
    let startPage = Math.max(1, this.currentPage - Math.floor(maxVisiblePages / 2));
    let endPage = Math.min(this.totalPages, startPage + maxVisiblePages - 1);
    
    // Adjust start page if we're near the end
    startPage = Math.max(1, endPage - maxVisiblePages + 1);
    
    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }
    
    return pages;
  }

  getStatusBadgeClass(status: string): string {
    return this.documentVerificationService.getStatusBadgeClass(status);
  }

  getStatusIcon(status: string): string {
    return this.documentVerificationService.getStatusIcon(status);
  }

  formatFileSize(bytes: number): string {
    return this.documentVerificationService.formatFileSize(bytes);
  }

  formatDate(dateString: string): string {
    return this.documentVerificationService.formatDate(dateString);
  }

  getDocumentTypeIcon(category: string): string {
    return this.documentVerificationService.getDocumentTypeIcon(category);
  }
}