// src/app/features/profile/profile-management/profile-management.component.ts
import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
  FormsModule,
} from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatRadioModule } from '@angular/material/radio';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { MatSliderModule } from '@angular/material/slider';
import { MatDividerModule } from '@angular/material/divider';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { COMMA, ENTER } from '@angular/cdk/keycodes';
import { MatChipInputEvent } from '@angular/material/chips';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { UserProfileService } from '../../../../core/services/user-profile.service';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipGrid } from '@angular/material/chips';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { BankingService } from '../../../../core/services/banking.service';
import { DocumentService } from '../../../../core/services/document.service';
import { MatBadgeModule } from '@angular/material/badge';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterModule } from '@angular/router';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';

interface ProfileSection {
  sectionName: string;
  weight: number;
  completionPercentage: number;
  completedFields: string[];
  missingFields: string[];
  suggestions: string[];
  statusIcon: string;
  statusText: string;
  status: number;
}

interface DetailedProfileCompletion {
  userId: string;
  overallCompletionPercentage: number;
  demographics: ProfileSection;
  documents: ProfileSection;
  banking: ProfileSection;
  interests: ProfileSection;
  nextSteps: any[];
  isEligibleForSurveys: boolean;
  lastUpdated: string;
  completionStatusText: string;
  eligibilityStatusText: string;
}

// Models for Documents
interface DocumentType {
  id: number;
  name: string;
  description: string;
  isRequired: boolean;
  category: string;
  allowedFileTypes: string;
  maxFileSizeMB: number;
}

interface UserDocument {
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

interface DocumentUpload {
  file: File;
  documentTypeId: number;
  expiryDate?: Date;
}

// Models for Banking
interface BankingDetail {
  id: number;
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
}

interface CreateBankingDetail {
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

@Component({
  selector: 'app-profile-management',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatRadioModule,
    MatProgressBarModule,
    MatChipsModule,
    MatSnackBarModule,
    MatTabsModule,
    MatSliderModule,
    MatDividerModule,
    MatSlideToggleModule,
    MatDatepickerModule,
    MatCheckboxModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatBadgeModule,
    MatTooltipModule,
    MatDialogModule,
    EmptyStateComponent
  ],
  templateUrl: './profile-management.component.html',
  styleUrls: ['./profile-management.component.scss'],
})
export class ProfileManagementComponent implements OnInit, OnDestroy {
  demographicsForm!: FormGroup;
  bankingForm!: FormGroup;
  profileCompletion: number = 0;
  userInterests: any[] = [];
  userEngagement: any = null;
  loading: boolean = true;
  saving: boolean = false;

  detailedCompletion?: DetailedProfileCompletion;

  // Auto-save properties
  private destroy$ = new Subject<void>();
  autoSaveStatus: 'saved' | 'saving' | 'error' | 'idle' = 'idle';
  lastSaveTime?: Date;
  
  // Celebration tracking
  private hasShownBasicInfoCelebration = false;
  private hasShownEducationCelebration = false;
  private hasShownIncomeCelebration = false;

  // Documents state
  documentTypes: DocumentType[] = [];
  userDocuments: UserDocument[] = [];
  uploadingDocument: boolean = false;
  selectedDocumentType: DocumentType | null = null;
  selectedFile: File | null = null;
  documentExpiryDate: Date | null = null;

  // Banking state
  bankingDetails: BankingDetail[] = [];
  showBankingForm: boolean = false;
  editingBanking: BankingDetail | null = null;
  loadingBanking: boolean = false;
  savingBanking: boolean = false;

  // For mat-chip-list
  readonly separatorKeysCodes = [ENTER, COMMA] as const;
  interestLevel: number = 3; // Default interest level (1-5)

  @ViewChild('chipList') chipList!: MatChipGrid;
  @ViewChild('fileInput') fileInput: any;

  // Demographics options
  genderOptions: string[] = [
    'Male',
    'Female',
    'Non-binary',
    'Prefer not to say',
  ];

  industryOptions: string[] = [
    'Agriculture & Forestry',
    'Automotive',
    'Banking & Financial Services',
    'Construction',
    'Consulting',
    'Education',
    'Energy & Utilities',
    'Entertainment & Media',
    'Fashion & Retail',
    'Food & Beverage',
    'Government',
    'Healthcare',
    'Hospitality & Tourism',
    'Information Technology',
    'Insurance',
    'Legal Services',
    'Manufacturing',
    'Mining',
    'Non-Profit/NGO',
    'Pharmaceuticals',
    'Real Estate',
    'Telecommunications',
    'Transportation & Logistics',
    'Other',
  ];

  occupationOptions: string[] = [
    'Accountant',
    'Administrative Assistant',
    'Analyst',
    'Architect',
    'Attorney/Advocate',
    'Banker',
    'Business Owner',
    'Call Centre Agent',
    'Consultant',
    'Data Scientist',
    'Designer',
    'Developer/Programmer',
    'Doctor/Medical Practitioner',
    'Engineer',
    'Executive/Manager',
    'Financial Advisor',
    'Government Employee',
    'Healthcare Worker',
    'HR Professional',
    'Insurance Agent',
    'IT Professional',
    'Marketing Professional',
    'Miner',
    'Nurse',
    'Project Manager',
    'Real Estate Agent',
    'Researcher',
    'Retired',
    'Sales Representative',
    'Social Worker',
    'Student',
    'Teacher/Educator',
    'Unemployed',
    'Other',
  ];

  educationOptions: string[] = [
    'Grade 12/Matric',
    'Certificate',
    'Diploma',
    'Higher Certificate',
    'Advanced Certificate',
    "Bachelor's Degree",
    'Honours Degree',
    "Master's Degree",
    'Doctorate/PhD',
    'Professional Qualification',
    'Other',
  ];

  incomeRangeOptions: string[] = [
    'Under R50,000',
    'R50,000 - R99,999',
    'R100,000 - R149,999',
    'R150,000 - R199,999',
    'R200,000 - R299,999',
    'R300,000 - R399,999',
    'R400,000 - R499,999',
    'R500,000 - R749,999',
    'R750,000 - R999,999',
    'R1,000,000+',
    'Prefer not to say',
  ];

  currencyOptions: string[] = ['ZAR', 'USD', 'EUR', 'GBP', 'Other'];

  availableInterests: string[] = [
    'Technology',
    'Healthcare',
    'Finance & Banking',
    'Education',
    'Entertainment',
    'Sports (Rugby, Cricket, Soccer)',
    'Travel & Tourism',
    'Food & Dining',
    'Fashion',
    'Automotive',
    'Real Estate',
    'Gaming',
    'Music',
    'Movies & TV',
    'Books & Reading',
    'Fitness & Health',
    'Cooking',
    'Photography',
    'Art & Design',
    'Environment & Conservation',
    'Politics',
    'Business & Entrepreneurship',
    'Shopping & Retail',
    'Home & Garden',
    'Parenting & Family',
    'Pets & Animals',
    'Science',
    'History',
    'News & Current Events',
    'Social Media',
    'Beauty & Cosmetics',
    'DIY & Crafts',
    'Outdoor Activities',
    'Religion & Spirituality',
    'Investment & Trading',
    'Cryptocurrency',
    'Sustainability',
    'Mental Health',
    'Career Development',
    'Retirement Planning',
    'Agriculture & Farming',
    'Mining & Resources',
    'Local Culture & Heritage',
  ];

  bankOptions: string[] = [
    'ABSA Bank',
    'Standard Bank',
    'First National Bank (FNB)',
    'Nedbank',
    'Capitec Bank',
    'African Bank',
    'Bidvest Bank',
    'Discovery Bank',
    'Grindrod Bank',
    'Investec Bank',
    'Mercantile Bank',
    'Sasfin Bank',
    'Tyme Bank',
    'Other',
  ];

  accountTypeOptions: string[] = [
    'Savings',
    'Cheque',
    'Current',
    'Transmission',
  ];

  provinceOptions: string[] = [
    'Eastern Cape',
    'Free State',
    'Gauteng',
    'KwaZulu-Natal',
    'Limpopo',
    'Mpumalanga',
    'Northern Cape',
    'North West',
    'Western Cape',
  ];

  employmentStatusOptions: string[] = [
    'Full-time',
    'Part-time',
    'Self-employed',
    'Unemployed',
    'Student',
    'Retired',
    'Other',
  ];
  maritalStatusOptions: string[] = [
    'Single',
    'Married',
    'Divorced',
    'Widowed',
    'Separated',
    'In a relationship',
    'Prefer not to say',
  ];
  urbanRuralOptions: string[] = ['Urban', 'Suburban', 'Rural', 'Unknown'];
  companySizeOptions: string[] = [
    'Self-employed',
    '1-10 employees (Small business)',
    '11-50 employees (Medium business)',
    '51-200 employees (Large business)',
    '201-500 employees',
    '501-1000 employees',
    '1001+ employees (Corporate)',
    'Government entity',
    'Parastatal',
  ];
  deviceTypeOptions: string[] = [
    'Smartphone',
    'Tablet',
    'Desktop Computer',
    'Laptop',
    'Smart TV',
    'Gaming Console',
    'Wearable Device',
    'Other',
  ];

  // Document status colors
  getStatusColor(status: string): string {
    switch (status.toLowerCase()) {
      case 'approved':
        return 'green';
      case 'pending':
        return 'orange';
      case 'rejected':
        return 'red';
      default:
        return 'gray';
    }
  }

  // Document status icons
  getStatusIcon(status: string): string {
    switch (status.toLowerCase()) {
      case 'approved':
        return 'check_circle';
      case 'pending':
        return 'schedule';
      case 'rejected':
        return 'cancel';
      default:
        return 'help';
    }
  }

  isExpiringSoon(expiryDate: Date): boolean {
    if (!expiryDate) return false;

    const today = new Date();
    const expiry = new Date(expiryDate);
    const diffTime = expiry.getTime() - today.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

    // Consider expiring if less than 30 days
    return diffDays <= 30 && diffDays > 0;
  }

  // Clear selected file (for the close button in file drop zone)
  clearSelectedFile(): void {
    this.selectedFile = null;
    if (this.fileInput) {
      this.fileInput.nativeElement.value = '';
    }
  }

  // Handle drag and drop for file upload (optional enhancement)
  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();

    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      const file = event.dataTransfer.files[0];
      if (
        this.selectedDocumentType &&
        this.validateFile(file, this.selectedDocumentType)
      ) {
        this.selectedFile = file;
      }
    }
  }

  constructor(
    private fb: FormBuilder,
    private userProfileService: UserProfileService,
    private documentService: DocumentService,
    private bankingService: BankingService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) {
    this.initializeForms();
  }

  private initializeForms(): void {
    // Demographics form (existing)
    this.demographicsForm = this.fb.group({
      id: [0],
      userId: [''],
      gender: ['', Validators.required],
      age: [
        null,
        [Validators.required, Validators.min(18), Validators.max(120)],
      ],
      highestEducation: ['', Validators.required],
      income: [null], // Keep for backward compatibility
      incomeRange: ['', [Validators.required]],
      incomeCurrency: ['ZAR'], // Default to ZAR for SA users
      location: ['', Validators.required],
      occupation: ['', Validators.required],
      maritalStatus: [''],
      householdSize: [null],
      hasChildren: [false],
      numberOfChildren: [null],
      country: ['South Africa'], // Default country
      state: [''],
      city: [''],
      zipCode: [''],
      urbanRural: [''],
      industry: [''],
      jobTitle: [''],
      yearsOfExperience: [null],
      employmentStatus: [''],
      companySize: [''],
      fieldOfStudy: [''],
      yearOfGraduation: [null],
      deviceTypes: [''],
      internetUsageHoursPerWeek: [null],
    });

    // Banking form
    this.bankingForm = this.fb.group({
      id: [0],
      bankName: ['', Validators.required],
      accountHolderName: ['', Validators.required],
      accountNumber: [
        '',
        [
          Validators.required,
          Validators.minLength(6),
          Validators.maxLength(20),
        ],
      ],
      accountType: ['', Validators.required],
      branchCode: ['', [Validators.pattern(/^\d{6}$/)]], // SA branch codes are 6 digits
      branchName: [''],
      swiftCode: [''],
      routingNumber: [''],
      isPrimary: [false],
    });
  }

  getBranchCodeError(): string {
    const branchCodeControl = this.bankingForm.get('branchCode');
    if (branchCodeControl?.hasError('pattern')) {
      return 'Branch code must be exactly 6 digits';
    }
    return '';
  }

  validateSAIdNumber(idNumber: string): boolean {
    // Basic SA ID validation - 13 digits
    const regex = /^\d{13}$/;
    return regex.test(idNumber);
  }

  getUploadProgress(): number {
    return this.uploadingDocument ? 50 : 0;
  }

  ngOnInit(): void {
    this.loadProfileData();
    this.loadDocumentTypes();
    this.loadUserDocuments();
    this.loadBankingDetails();
    this.setupAutoSave();
  }

  loadProfileData(): void {
    this.loading = true;

    // Get profile completion (now returns detailed object)
    this.userProfileService.getProfileCompletion().subscribe({
      next: (completion) => {
        console.log('Profile completion loaded:', completion);

        // Check if this is the detailed response (your API returns this)
        if (
          completion &&
          typeof completion === 'object' &&
          completion.overallCompletionPercentage !== undefined
        ) {
          this.detailedCompletion = completion as DetailedProfileCompletion;
          this.profileCompletion =
            this.detailedCompletion.overallCompletionPercentage;
        } else if (typeof completion === 'number') {
          // Fallback if it's just a number
          this.profileCompletion = completion;
        } else {
          console.warn('Unexpected completion response format:', completion);
          this.profileCompletion = 0;
        }
      },
      error: (error) => {
        console.error('Error loading profile completion', error);
      },
    });

    // Get demographics
    this.userProfileService.getUserDemographics().subscribe({
      next: (demographics) => {
        console.log('Demographics loaded:', demographics); // Debug log
        this.demographicsForm.patchValue(demographics);
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading demographics', error);
        this.loading = false;
      },
    });

    // Get interests
    this.userProfileService.getUserInterests().subscribe({
      next: (interests) => {
        console.log('Interests loaded:', interests); // Debug log
        this.userInterests = interests;
      },
      error: (error) => {
        console.error('Error loading interests', error);
      },
    });

    // Get engagement
    this.userProfileService.getUserEngagement().subscribe({
      next: (engagement) => {
        console.log('Engagement loaded:', engagement); // Debug log
        this.userEngagement = engagement;
      },
      error: (error) => {
        console.error('Error loading engagement', error);
      },
    });
  }

  // Enhanced save methods with better error handling
  saveDemographicsEnhanced(): void {
    if (this.demographicsForm.invalid) {
      // Mark all fields as touched to show validation errors
      Object.keys(this.demographicsForm.controls).forEach((key) => {
        this.demographicsForm.get(key)?.markAsTouched();
      });

      this.snackBar.open(
        'Please fill out all required fields correctly.',
        'Close',
        {
          duration: 5000,
        }
      );
      return;
    }

    this.saveDemographics(); // Call your existing method
  }

  getTabCompletionPercentage(tabName: string): number {
    switch (tabName.toLowerCase()) {
      case 'demographics':
        return this.detailedCompletion?.demographics?.completionPercentage || 0;
      case 'documents':
        return this.detailedCompletion?.documents?.completionPercentage || 0;
      case 'banking':
        return this.detailedCompletion?.banking?.completionPercentage || 0;
      case 'interests':
        return this.detailedCompletion?.interests?.completionPercentage || 0;
      default:
        return 0;
    }
  }

  getTabStatusIcon(tabName: string): string {
    const percentage = this.getTabCompletionPercentage(tabName);
    if (percentage >= 100) return 'check_circle';
    if (percentage >= 50) return 'pending';
    return 'radio_button_unchecked';
  }

  // Method to get tab status color
  getTabStatusColor(tabName: string): string {
    const percentage = this.getTabCompletionPercentage(tabName);
    if (percentage >= 100) return 'primary';
    if (percentage >= 50) return 'accent';
    return 'warn';
  }

  // Document Management Methods
  loadDocumentTypes(): void {
    this.documentService.getDocumentTypes().subscribe({
      next: (types: DocumentType[]) => {
        this.documentTypes = types;
      },
      error: (error: any) => {
        console.error('Error loading document types', error);
        this.snackBar.open('Error loading document types', 'Close', {
          duration: 5000,
        });
      },
    });
  }

  loadUserDocuments(): void {
    this.documentService.getUserDocuments().subscribe({
      next: (documents: UserDocument[]) => {
        this.userDocuments = documents;
      },
      error: (error: any) => {
        console.error('Error loading user documents', error);
        this.snackBar.open('Error loading your documents', 'Close', {
          duration: 5000,
        });
      },
    });
  }

  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file && this.selectedDocumentType) {
      // Validate file
      if (this.validateFile(file, this.selectedDocumentType)) {
        this.selectedFile = file;
      } else {
        this.selectedFile = null;
        this.fileInput.nativeElement.value = '';
      }
    }
  }

  validateFile(file: File, documentType: DocumentType): boolean {
    // Check file size
    const maxSizeBytes = documentType.maxFileSizeMB * 1024 * 1024;
    if (file.size > maxSizeBytes) {
      this.snackBar.open(
        `File size (${this.formatFileSize(file.size)}) exceeds the ${
          documentType.maxFileSizeMB
        }MB limit for ${documentType.name}`,
        'Close',
        { duration: 6000 }
      );
      return false;
    }

    // Check file type
    const allowedTypes = documentType.allowedFileTypes
      .split(',')
      .map((type) => type.trim().toLowerCase());
    const fileExtension = file.name.split('.').pop()?.toLowerCase() || '';

    if (!allowedTypes.includes(fileExtension)) {
      this.snackBar.open(
        `File type '.${fileExtension}' is not allowed for ${documentType.name}. Allowed types: ${documentType.allowedFileTypes}`,
        'Close',
        { duration: 6000 }
      );
      return false;
    }

    return true;
  }

  uploadDocument(): void {
    if (!this.selectedFile || !this.selectedDocumentType) {
      this.snackBar.open(
        'Please select both a file and document type',
        'Close',
        { duration: 3000 }
      );
      return;
    }

    // Double-check validation before upload
    if (!this.validateFile(this.selectedFile, this.selectedDocumentType)) {
      return;
    }

    this.uploadingDocument = true;

    const uploadData = new FormData();
    uploadData.append('File', this.selectedFile);
    uploadData.append(
      'DocumentTypeId',
      this.selectedDocumentType.id.toString()
    );

    if (this.documentExpiryDate) {
      uploadData.append('ExpiryDate', this.documentExpiryDate.toISOString());
    }

    this.documentService.uploadDocument(uploadData).subscribe({
      next: (result: any) => {
        this.uploadingDocument = false;
        this.snackBar.open(
          `${this.selectedDocumentType?.name} uploaded successfully! It will be reviewed for verification.`,
          'Close',
          { duration: 4000 }
        );

        // Reset form
        this.clearSelectedFile();
        this.selectedDocumentType = null;
        this.documentExpiryDate = null;

        // Reload documents and profile completion
        this.loadUserDocuments();
        this.refreshProfileCompletionSilently();
      },
      error: (error: { error: { message: any } }) => {
        this.uploadingDocument = false;
        console.error('Error uploading document', error);

        let errorMessage = 'Error uploading document';
        if (error.error?.message) {
          errorMessage = error.error.message;
        } else if (error.error) {
          errorMessage = 'Upload failed. Please try again.';
        }

        this.snackBar.open(errorMessage, 'Close', { duration: 6000 });
      },
    });
  }

  downloadDocument(userDoc: UserDocument): void {
    this.documentService.downloadDocument(userDoc.id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = userDoc.originalFileName;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: (error) => {
        console.error('Error downloading document', error);
        this.snackBar.open('Error downloading document', 'Close', {
          duration: 5000,
        });
      },
    });
  }

  deleteDocument(document: UserDocument): void {
    if (
      confirm(`Are you sure you want to delete ${document.originalFileName}?`)
    ) {
      this.documentService.deleteDocument(document.id).subscribe({
        next: () => {
          this.snackBar.open('Document deleted successfully', 'Close', {
            duration: 3000,
          });
          this.loadUserDocuments();
          this.userProfileService
            .getProfileCompletion()
            .subscribe((completion) => {
              this.profileCompletion = completion;
            });
        },
        error: (error: { error: { message: any } }) => {
          console.error('Error deleting document', error);
          this.snackBar.open(
            error.error?.message || 'Error deleting document',
            'Close',
            { duration: 5000 }
          );
        },
      });
    }
  }

  // Banking Management Methods
  loadBankingDetails(): void {
    this.loadingBanking = true;
    this.bankingService.getBankingDetails().subscribe({
      next: (details: BankingDetail[]) => {
        console.log('Loaded banking details:', details);

        this.bankingDetails = details;
        this.loadingBanking = false;
      },
      error: (error: any) => {
        console.error('Error loading banking details', error);
        this.snackBar.open('Error loading banking details', 'Close', {
          duration: 5000,
        });
        this.loadingBanking = false;
      },
    });
  }

  showAddBankingForm(): void {
    this.showBankingForm = true;
    this.editingBanking = null;
    this.bankingForm.reset();
    this.bankingForm.patchValue({
      isPrimary: this.bankingDetails.length === 0,
    });
  }

  editBankingDetail(banking: BankingDetail): void {
    if (banking.isVerified) {
      this.snackBar.open('Cannot edit verified banking details', 'Close', {
        duration: 5000,
      });
      return;
    }

    this.showBankingForm = true;
    this.editingBanking = banking;
    this.bankingForm.patchValue(banking);
  }

  cancelBankingForm(): void {
    this.showBankingForm = false;
    this.editingBanking = null;
    this.bankingForm.reset();
  }

  saveBankingDetails(): void {
    if (this.bankingForm.invalid) {
      // Mark all fields as touched to show validation errors
      Object.keys(this.bankingForm.controls).forEach((key) => {
        this.bankingForm.get(key)?.markAsTouched();
      });

      // Show specific validation errors
      const errors = [];
      if (this.bankingForm.get('bankName')?.hasError('required')) {
        errors.push('Bank name is required');
      }
      if (this.bankingForm.get('accountHolderName')?.hasError('required')) {
        errors.push('Account holder name is required');
      }
      if (this.bankingForm.get('accountNumber')?.hasError('required')) {
        errors.push('Account number is required');
      }
      if (
        this.bankingForm.get('accountNumber')?.hasError('minlength') ||
        this.bankingForm.get('accountNumber')?.hasError('maxlength')
      ) {
        errors.push('Account number must be between 6 and 20 digits');
      }
      if (this.bankingForm.get('accountType')?.hasError('required')) {
        errors.push('Account type is required');
      }
      if (this.bankingForm.get('branchCode')?.hasError('pattern')) {
        errors.push('Branch code must be exactly 6 digits');
      }

      this.snackBar.open(
        errors.length > 0
          ? errors[0]
          : 'Please fill out all required fields correctly.',
        'Close',
        { duration: 5000 }
      );
      return;
    }

    this.savingBanking = true;
    const bankingData = this.bankingForm.getRawValue();

    if (this.editingBanking) {
      // Update existing
      bankingData.id = this.editingBanking.id;
      this.bankingService.updateBankingDetail(bankingData).subscribe({
        next: () => {
          this.savingBanking = false;
          this.snackBar.open('Banking details updated successfully!', 'Close', {
            duration: 3000,
          });
          this.cancelBankingForm();
          this.loadBankingDetails();
          this.refreshProfileCompletionSilently();
        },
        error: (error: { error: { message: any } }) => {
          this.savingBanking = false;
          console.error('Error updating banking details', error);
          this.snackBar.open(
            error.error?.message ||
              'Error updating banking details. Please try again.',
            'Close',
            { duration: 6000 }
          );
        },
      });
    } else {
      // Create new
      this.bankingService.createBankingDetail(bankingData).subscribe({
        next: () => {
          this.savingBanking = false;
          this.snackBar.open(
            'Banking details added successfully! They will be verified before payments can be processed.',
            'Close',
            { duration: 4000 }
          );
          this.cancelBankingForm();
          this.loadBankingDetails();
          this.refreshProfileCompletionSilently();
        },
        error: (error: { error: { message: any } }) => {
          this.savingBanking = false;
          console.error('Error adding banking details', error);
          this.snackBar.open(
            error.error?.message ||
              'Error adding banking details. Please try again.',
            'Close',
            { duration: 6000 }
          );
        },
      });
    }
  }

  setPrimaryBankingDetail(banking: BankingDetail): void {
    if (banking.isPrimary) return;

    this.bankingService.setPrimaryBankingDetail(banking.id).subscribe({
      next: () => {
        this.snackBar.open('Primary banking detail updated!', 'Close', {
          duration: 3000,
        });
        this.loadBankingDetails();
      },
      error: (error: { error: { message: any } }) => {
        console.error('Error setting primary banking detail', error);
        this.snackBar.open(
          error.error?.message || 'Error updating primary banking detail',
          'Close',
          { duration: 5000 }
        );
      },
    });
  }

  deleteBankingDetail(banking: BankingDetail): void {
    if (
      confirm(
        `Are you sure you want to delete banking details for ${banking.bankName}?`
      )
    ) {
      this.bankingService.deleteBankingDetail(banking.id).subscribe({
        next: () => {
          this.snackBar.open('Banking details deleted successfully', 'Close', {
            duration: 3000,
          });
          this.loadBankingDetails();
          this.userProfileService
            .getProfileCompletion()
            .subscribe((completion) => {
              this.profileCompletion = completion;
            });
        },
        error: (error: { error: { message: any } }) => {
          console.error('Error deleting banking details', error);
          this.snackBar.open(
            error.error?.message || 'Error deleting banking details',
            'Close',
            { duration: 5000 }
          );
        },
      });
    }
  }

  setupAutoSave(): void {
    // Auto-save demographics form changes
    this.demographicsForm.valueChanges.pipe(
      debounceTime(2000), // Wait 2 seconds after user stops typing
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(() => {
      if (this.demographicsForm.valid && !this.saving) {
        this.autoSaveDemographics();
      }
    });

    // Auto-save banking form changes
    this.bankingForm.valueChanges.pipe(
      debounceTime(2000),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(() => {
      if (this.bankingForm.valid && !this.saving) {
        this.autoSaveBanking();
      }
    });
  }

  autoSaveDemographics(): void {
    if (this.autoSaveStatus === 'saving') return;
    
    this.autoSaveStatus = 'saving';
    const demographicsData = this.demographicsForm.getRawValue();

    this.userProfileService.updateDemographics(demographicsData).subscribe({
      next: () => {
        this.autoSaveStatus = 'saved';
        this.lastSaveTime = new Date();
        
        // Clear saved status after 3 seconds
        setTimeout(() => {
          if (this.autoSaveStatus === 'saved') {
            this.autoSaveStatus = 'idle';
          }
        }, 3000);

        // Refresh profile completion silently
        this.userProfileService.getProfileCompletion().subscribe({
          next: (completion) => {
            this.detailedCompletion = completion as DetailedProfileCompletion;
            this.profileCompletion = completion.overallCompletionPercentage || 0;
          }
        });
      },
      error: (error: any) => {
        this.autoSaveStatus = 'error';
        console.error('Auto-save failed:', error);
      }
    });
  }

  autoSaveBanking(): void {
    // Banking auto-save disabled for now - requires different approach
    // since banking details have separate create/update endpoints
    console.log('Banking auto-save triggered but disabled - use manual save');
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // Progress calculation methods
  getBasicInfoCompletion(): number {
    const fields = ['age', 'gender', 'location', 'country', 'state'];
    const completed = fields.filter(field => {
      const control = this.demographicsForm.get(field);
      return control?.value && control?.valid;
    }).length;
    const completion = Math.round((completed / fields.length) * 100);
    
    // Track completion for celebration
    if (completion === 100 && !this.hasShownBasicInfoCelebration) {
      setTimeout(() => this.showSectionCompletionCelebration('Basic Information', completion), 500);
      this.hasShownBasicInfoCelebration = true;
    }
    
    return completion;
  }

  getEducationCompletion(): number {
    const fields = ['highestEducation', 'occupation', 'industry'];
    const completed = fields.filter(field => {
      const control = this.demographicsForm.get(field);
      return control?.value && control?.valid;
    }).length;
    const completion = Math.round((completed / fields.length) * 100);
    
    if (completion === 100 && !this.hasShownEducationCelebration) {
      setTimeout(() => this.showSectionCompletionCelebration('Education & Employment', completion), 500);
      this.hasShownEducationCelebration = true;
    }
    
    return completion;
  }

  getIncomeCompletion(): number {
    const fields = ['incomeRange', 'incomeCurrency'];
    const completed = fields.filter(field => {
      const control = this.demographicsForm.get(field);
      return control?.value && control?.valid;
    }).length;
    const completion = Math.round((completed / fields.length) * 100);
    
    if (completion === 100 && !this.hasShownIncomeCelebration) {
      setTimeout(() => this.showSectionCompletionCelebration('Income Information', completion), 500);
      this.hasShownIncomeCelebration = true;
    }
    
    return completion;
  }

  // Real-time validation helper
  getFieldValidationClass(fieldName: string): string {
    const control = this.demographicsForm.get(fieldName);
    if (!control?.touched) return '';
    return control.valid ? 'field-valid' : 'field-invalid';
  }

  // Section completion celebration
  showSectionCompletionCelebration(sectionName: string, completion: number): void {
    if (completion === 100) {
      this.snackBar.open(
        `🎉 ${sectionName} section completed! +${this.getSectionPoints(sectionName)} points earned`,
        'Close',
        {
          duration: 5000,
          panelClass: ['success-snackbar']
        }
      );
    }
  }

  getSectionPoints(sectionName: string): number {
    const pointsMap: { [key: string]: number } = {
      'Basic Information': 25,
      'Education & Employment': 15,
      'Income Information': 10
    };
    return pointsMap[sectionName] || 0;
  }

  saveDemographics(): void {
    if (this.demographicsForm.invalid) {
      this.snackBar.open(
        'Please fill out all required fields correctly.',
        'Close',
        {
          duration: 5000,
        }
      );
      return;
    }

    this.saving = true;

    const demographicsData = this.demographicsForm.getRawValue();

    this.userProfileService.updateDemographics(demographicsData).subscribe({
      next: () => {
        this.saving = false;
        this.snackBar.open('Profile updated successfully!', 'Close', {
          duration: 3000,
        });

        // Refresh profile completion
        this.userProfileService.getProfileCompletion().subscribe({
          next: (completion) => {
            this.profileCompletion = completion;
          },
        });
      },
      error: (error) => {
        this.saving = false;
        console.error('Error updating demographics', error);
        this.snackBar.open(
          error.error?.message || 'Error updating profile. Please try again.',
          'Close',
          {
            duration: 5000,
          }
        );
      },
    });
  }

  addInterest(event: MatChipInputEvent): void {
    const value = (event.value || '').trim();

    if (value) {
      // Check if interest already exists
      if (
        this.userInterests.find(
          (i) => i.interest.toLowerCase() === value.toLowerCase()
        )
      ) {
        this.snackBar.open('This interest already exists.', 'Close', {
          duration: 3000,
        });
        return;
      }

      const interestData = {
        interest: value,
        interestLevel: this.interestLevel,
      };

      this.userProfileService.addUserInterest(interestData).subscribe({
        next: (result) => {
          // Add interest locally with the returned ID
          this.userInterests.push({
            id: result.id,
            interest: value,
            interestLevel: this.interestLevel,
          });

          // Reset input
          event.chipInput!.clear();

          // Refresh profile completion
          this.userProfileService.getProfileCompletion().subscribe({
            next: (completion) => {
              this.profileCompletion = completion;
            },
          });
        },
        error: (error) => {
          console.error('Error adding interest', error);
          this.snackBar.open(
            'Error adding interest. Please try again.',
            'Close',
            {
              duration: 5000,
            }
          );
        },
      });
    }
  }

  addInterestFromDropdown(): void {
    if (!this.selectedInterest) {
      this.snackBar.open('Please select an interest first.', 'Close', {
        duration: 3000,
      });
      return;
    }

    // Check if interest already exists
    if (
      this.userInterests.find(
        (i) => i.interest.toLowerCase() === this.selectedInterest.toLowerCase()
      )
    ) {
      this.snackBar.open('This interest already exists.', 'Close', {
        duration: 3000,
      });
      return;
    }

    const interestData = {
      interest: this.selectedInterest,
      interestLevel: this.interestLevel,
    };

    // Show loading state
    const loadingSnackBar = this.snackBar.open('Adding interest...', '', {
      duration: 0, // Keep open until we close it
    });

    this.userProfileService.addUserInterest(interestData).subscribe({
      next: (result) => {
        // Close loading message
        loadingSnackBar.dismiss();

        // IMMEDIATELY update the local state (this fixes the refresh issue)
        const newInterest = {
          id: result.id || Date.now(), // Fallback ID if not returned
          interest: this.selectedInterest,
          interestLevel: this.interestLevel,
        };

        // Add to the array and trigger change detection
        this.userInterests = [...this.userInterests, newInterest];

        // Reset the form
        this.selectedInterest = '';

        this.snackBar.open('Interest added successfully!', 'Close', {
          duration: 3000,
        });

        // Refresh profile completion in background
        this.refreshProfileCompletionSilently();
      },
      error: (error) => {
        // Close loading message
        loadingSnackBar.dismiss();

        console.error('Error adding interest', error);
        this.snackBar.open(
          error.error?.message || 'Error adding interest. Please try again.',
          'Close',
          {
            duration: 5000,
          }
        );
      },
    });
  }

  selectedInterest: string = '';

  removeInterest(interestId: number): void {
    // Find the interest to remove (for rollback if needed)
    const interestToRemove = this.userInterests.find(
      (i) => i.id === interestId
    );

    if (!interestToRemove) {
      this.snackBar.open('Interest not found.', 'Close', { duration: 3000 });
      return;
    }

    // Show confirmation
    const confirmDelete = confirm(
      `Are you sure you want to remove "${interestToRemove.interest}"?`
    );
    if (!confirmDelete) {
      return;
    }

    // IMMEDIATELY remove from UI (optimistic update)
    this.userInterests = this.userInterests.filter((i) => i.id !== interestId);

    // Show loading state
    const loadingSnackBar = this.snackBar.open('Removing interest...', '', {
      duration: 0,
    });

    // Send the interest STRING to the backend, not the ID
    this.userProfileService
      .removeUserInterest(interestToRemove.interest)
      .subscribe({
        next: () => {
          // Close loading message
          loadingSnackBar.dismiss();

          this.snackBar.open('Interest removed successfully!', 'Close', {
            duration: 3000,
          });

          // Refresh profile completion in background
          this.refreshProfileCompletionSilently();
        },
        error: (error) => {
          // Close loading message
          loadingSnackBar.dismiss();

          // ROLLBACK: Add the interest back since removal failed
          this.userInterests = [...this.userInterests, interestToRemove];

          console.error('Error removing interest', error);
          this.snackBar.open(
            error.error?.message ||
              'Error removing interest. Please try again.',
            'Close',
            {
              duration: 5000,
            }
          );
        },
      });
  }

  // Helper method to refresh profile completion silently (no loading indicators)
  private refreshProfileCompletionSilently(): void {
    this.userProfileService.getProfileCompletion().subscribe({
      next: (completion) => {
        if (
          typeof completion === 'object' &&
          (completion as DetailedProfileCompletion)
            .overallCompletionPercentage !== undefined
        ) {
          this.detailedCompletion = completion as DetailedProfileCompletion;
          this.profileCompletion =
            this.detailedCompletion.overallCompletionPercentage;
        } else {
          this.profileCompletion = completion as number;
        }
      },
      error: (error) => {
        console.error('Error refreshing profile completion', error);
        // Fail silently - don't show error to user for background refresh
      },
    });
  }

  // Filter available interests (updated to work with array changes)
  getAvailableInterests(): string[] {
    const selectedInterestNames = this.userInterests.map((i) =>
      i.interest.toLowerCase()
    );
    return this.availableInterests.filter(
      (interest) => !selectedInterestNames.includes(interest.toLowerCase())
    );
  }

  onHasChildrenChange(event: any): void {
    if (!event.checked) {
      this.demographicsForm.get('numberOfChildren')?.setValue(null);
    }
  }

  getInterestLevelText(level: number): string {
    switch (level) {
      case 1:
        return 'Slightly Interested';
      case 2:
        return 'Somewhat Interested';
      case 3:
        return 'Interested';
      case 4:
        return 'Very Interested';
      case 5:
        return 'Extremely Interested';
      default:
        return 'Interested';
    }
  }

  getCompletionClass(): string {
    if (this.profileCompletion >= 80) return 'high-completion';
    if (this.profileCompletion >= 50) return 'medium-completion';
    return 'low-completion';
  }

  getDeviceTypesArray(): string[] {
    const deviceTypesStr = this.demographicsForm.get('deviceTypes')?.value;
    return deviceTypesStr ? deviceTypesStr.split(',') : [];
  }

  updateDeviceTypes(deviceType: string, isChecked: boolean): void {
    const currentDevices = this.getDeviceTypesArray();
    let updatedDevices: string[];

    if (isChecked && !currentDevices.includes(deviceType)) {
      updatedDevices = [...currentDevices, deviceType];
    } else if (!isChecked && currentDevices.includes(deviceType)) {
      updatedDevices = currentDevices.filter((d) => d !== deviceType);
    } else {
      return; // No change needed
    }

    this.demographicsForm
      .get('deviceTypes')
      ?.setValue(updatedDevices.join(','));
  }

  isDeviceSelected(deviceType: string): boolean {
    return this.getDeviceTypesArray().includes(deviceType);
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  getAcceptedFileTypes(): string {
    if (!this.selectedDocumentType) return '';
    return this.selectedDocumentType.allowedFileTypes
      .split(',')
      .map((t) => '.' + t.trim())
      .join(',');
  }

  getMaskedAccountNumber(accountNumber: string): string {
    if (!accountNumber || accountNumber.length < 4) {
      return 'N/A';
    }
    return '****' + accountNumber.slice(-4);
  }

  refreshProfileCompletion(): void {
    console.log('Manually refreshing profile completion...'); // Debug log
    this.userProfileService.getProfileCompletion().subscribe({
      next: (completion: DetailedProfileCompletion | number) => {
        console.log('Profile completion refreshed:', completion); // Debug log

        // Check if this is the detailed response
        if (
          typeof completion === 'object' &&
          (completion as DetailedProfileCompletion)
            .overallCompletionPercentage !== undefined
        ) {
          this.detailedCompletion = completion as DetailedProfileCompletion;
          this.profileCompletion =
            this.detailedCompletion.overallCompletionPercentage;
          this.snackBar.open(
            `Profile completion updated: ${this.detailedCompletion.overallCompletionPercentage}%`,
            'Close',
            { duration: 3000 }
          );
        } else {
          // If it's just a number
          this.profileCompletion = completion as number;
          this.snackBar.open(
            `Profile completion updated: ${completion}%`,
            'Close',
            { duration: 3000 }
          );
        }
      },
      error: (error) => {
        console.error('Error refreshing profile completion', error);
        this.snackBar.open('Error refreshing profile completion', 'Close', {
          duration: 5000,
        });
      },
    });
  }

  debugProfileData(): void {
    console.log('=== PROFILE DEBUG INFO ===');
    console.log('Current profile completion:', this.profileCompletion);
    console.log('User documents count:', this.userDocuments.length);
    console.log('Banking details count:', this.bankingDetails.length);
    console.log('User interests count:', this.userInterests.length);
    console.log('Demographics form data:', this.demographicsForm.value);
    console.log('=== END DEBUG INFO ===');

    // Also refresh all data
    this.loadProfileData();
    this.loadUserDocuments();
    this.loadBankingDetails();
  }

  getStepRoute(section: string): string {
    // Map section names to tab indices or routes
    switch (section.toLowerCase()) {
      case 'demographics':
        return '/profile'; // Could use fragment: '#demographics'
      case 'documents':
        return '/profile'; // Could use fragment: '#documents'
      case 'banking':
        return '/profile'; // Could use fragment: '#banking'
      case 'interests':
        return '/profile'; // Could use fragment: '#interests'
      default:
        return '/profile';
    }
  }
}
