import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface BankingDetail {
  id: number;
  userId: string;
  bankName: string;
  accountHolderName: string;
  accountNumber: string;
  accountType: string;
  branchCode?: string;
  branchName?: string;
  swiftCode?: string;
  routingNumber?: string;
  isPrimary: boolean;
  isVerified: boolean;
  createdDate: Date;
  verifiedDate?: Date;
  verifiedBy?: string;
  notes?: string;
}

export interface CreateBankingDetail {
  bankName: string;
  accountHolderName: string;
  accountNumber: string;
  accountType: string;
  branchCode?: string;
  branchName?: string;
  swiftCode?: string;
  routingNumber?: string;
  isPrimary?: boolean;
}

export interface UpdateBankingDetail extends CreateBankingDetail {
  id: number;
}

@Injectable({
  providedIn: 'root'
})
export class BankingService {
  private apiUrl = `${environment.apiUrl}/api/banking`;
  
  constructor(private http: HttpClient) { }
  
  getBankingDetails(): Observable<BankingDetail[]> {
    return this.http.get<BankingDetail[]>(`${this.apiUrl}`);
  }
  
  getBankingDetail(id: number): Observable<BankingDetail> {
    return this.http.get<BankingDetail>(`${this.apiUrl}/${id}`);
  }
  
  createBankingDetail(bankingDetail: CreateBankingDetail): Observable<BankingDetail> {
    return this.http.post<BankingDetail>(`${this.apiUrl}`, bankingDetail);
  }
  
  updateBankingDetail(bankingDetail: UpdateBankingDetail): Observable<any> {
    return this.http.put(`${this.apiUrl}/${bankingDetail.id}`, bankingDetail);
  }
  
  deleteBankingDetail(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
  
  setPrimaryBankingDetail(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/set-primary`, {});
  }
}