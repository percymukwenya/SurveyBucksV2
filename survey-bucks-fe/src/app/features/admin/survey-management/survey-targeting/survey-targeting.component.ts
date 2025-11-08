import { ENTER, COMMA } from '@angular/cdk/keycodes';
import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import {
  FormArray,
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipInputEvent, MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { AdminSurveyService } from '../../../../core/services/admin-survey.service';
import { catchError, finalize, forkJoin, of } from 'rxjs';
import { MatExpansionModule } from '@angular/material/expansion';

@Component({
  selector: 'app-survey-targeting',
  imports: [
    CommonModule,
    MatExpansionModule,
    FormsModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatSnackBarModule,
    MatTabsModule,
  ],
  templateUrl: './survey-targeting.component.html',
  styleUrl: './survey-targeting.component.scss',
})
export class SurveyTargetingComponent {
  @Input() surveyId!: number;

  loading = true;
  saving = false;

  // Demographic data
  genders = ['Male', 'Female', 'Non-binary', 'Other', 'Prefer not to say'];
  educationLevels = [
    'No formal education',
    'Primary education',
    'Secondary education',
    'High school diploma',
    'Some college',
    "Bachelor's degree",
    "Master's degree",
    'Doctorate degree',
    'Professional degree',
    'Vocational training',
  ];
  maritalStatuses = [
    'Single',
    'Married',
    'Divorced',
    'Widowed',
    'Separated',
    'Domestic partnership',
  ];
  industries = [
    'Technology',
    'Healthcare',
    'Finance',
    'Education',
    'Retail',
    'Manufacturing',
    'Entertainment',
    'Hospitality',
    'Transportation',
    'Agriculture',
    'Construction',
    'Energy',
    'Media',
    'Real Estate',
    'Legal Services',
    'Government',
    'Non-profit',
    'Other',
  ];

  // Target forms
  targetingForm!: FormGroup;

  // Chip list settings
  readonly separatorKeysCodes = [ENTER, COMMA] as const;

  constructor(
    private fb: FormBuilder,
    private surveyService: AdminSurveyService,
    private snackBar: MatSnackBar
  ) {
    this.targetingForm = this.createTargetingForm();
  }

  ngOnInit(): void {
    this.loadTargetingData();
  }

  createTargetingForm(): FormGroup {
    return this.fb.group({
      // Age ranges
      ageRanges: this.fb.array([]),

      // Gender
      genderTargets: this.fb.array([]),

      // Education
      educationTargets: this.fb.array([]),

      // Income ranges
      incomeRanges: this.fb.array([]),

      // Location
      countries: this.fb.array([]),
      states: this.fb.array([]),

      // Household
      householdSizes: this.fb.array([]),

      // Parental status
      parentalStatuses: this.fb.array([]),

      // Industry
      industries: this.fb.array([]),

      // Occupation
      occupations: this.fb.array([]),

      // Marital status
      maritalStatuses: this.fb.array([]),

      // Interests
      interests: this.fb.array([]),
    });
  }

  loadTargetingData(): void {
    this.loading = true;

    this.surveyService
      .getAllSurveyTargets(this.surveyId)
      .pipe(
        catchError((error) => {
          console.error('Error loading targeting data', error);
          this.snackBar.open(
            'Error loading targeting data. Please try again.',
            'Close',
            {
              duration: 5000,
            }
          );
          return of({
            ageRanges: [],
            genders: [],
            education: [],
            incomeRanges: [],
            locations: [],
            countries: [],
            states: [],
            industries: [],
            householdSizes: [],
            parentalStatus: [],
            interests: [],
            occupations: [],
            maritalStatus: [],
          });
        }),
        finalize(() => {
          this.loading = false;
        })
      )
      .subscribe((data) => {
        // Clear existing arrays
        this.clearFormArrays();

        // Populate form arrays
        this.populateAgeRanges(data.ageRanges);
        this.populateGenderTargets(data.genders);
        this.populateEducationTargets(data.education);
        this.populateIncomeRanges(data.incomeRanges);
        this.populateCountries(data.countries);
        this.populateStates(data.states);
        this.populateHouseholdSizes(data.householdSizes);
        this.populateParentalStatuses(data.parentalStatus);
        this.populateIndustries(data.industries);
        this.populateOccupations(data.occupations);
        this.populateMaritalStatuses(data.maritalStatus);
        this.populateInterests(data.interests);
      });
  }

  clearFormArrays(): void {
    const formArrays = [
      'ageRanges',
      'genderTargets',
      'educationTargets',
      'incomeRanges',
      'countries',
      'states',
      'householdSizes',
      'parentalStatuses',
      'industries',
      'occupations',
      'maritalStatuses',
      'interests',
    ];

    formArrays.forEach((arrayName) => {
      const formArray = this.targetingForm.get(arrayName) as FormArray;
      while (formArray.length) {
        formArray.removeAt(0);
      }
    });
  }

  // Age ranges
  get ageRangesArray(): FormArray {
    return this.targetingForm.get('ageRanges') as FormArray;
  }

  createAgeRangeGroup(data?: any): FormGroup {
    return this.fb.group({
      id: [data?.id || 0],
      surveyId: [this.surveyId],
      minAge: [data?.minAge || 18],
      maxAge: [data?.maxAge || 65],
    });
  }

  populateAgeRanges(ageRanges: any[]): void {
    ageRanges.forEach((range) => {
      this.ageRangesArray.push(this.createAgeRangeGroup(range));
    });
  }

  addAgeRange(): void {
    this.ageRangesArray.push(this.createAgeRangeGroup());
  }

  removeAgeRange(index: number): void {
    const ageRange = this.ageRangesArray.at(index).value;

    if (ageRange.id > 0) {
      // Delete from database if it exists
      this.surveyService.deleteSurveyAgeRangeTarget(ageRange.id).subscribe({
        error: (error) => {
          console.error('Error deleting age range target', error);
        },
      });
    }

    this.ageRangesArray.removeAt(index);
  }

  // Gender targets
  get genderTargetsArray(): FormArray {
    return this.targetingForm.get('genderTargets') as FormArray;
  }

  createGenderTargetGroup(data?: any): FormGroup {
    return this.fb.group({
      id: [data?.id || 0],
      surveyId: [this.surveyId],
      gender: [data?.gender || ''],
    });
  }

  populateGenderTargets(genderTargets: any[]): void {
    genderTargets.forEach((target) => {
      this.genderTargetsArray.push(this.createGenderTargetGroup(target));
    });
  }

  addGenderTarget(): void {
    this.genderTargetsArray.push(this.createGenderTargetGroup());
  }

  removeGenderTarget(index: number): void {
    const genderTarget = this.genderTargetsArray.at(index).value;

    if (genderTarget.id > 0) {
      this.surveyService.deleteSurveyGenderTarget(genderTarget.id).subscribe({
        error: (error) => {
          console.error('Error deleting gender target', error);
        },
      });
    }

    this.genderTargetsArray.removeAt(index);
  }

  // Education targets
  get educationTargetsArray(): FormArray {
    return this.targetingForm.get('educationTargets') as FormArray;
  }

  createEducationTargetGroup(data?: any): FormGroup {
    return this.fb.group({
      id: [data?.id || 0],
      surveyId: [this.surveyId],
      education: [data?.education || ''],
    });
  }

  populateEducationTargets(educationTargets: any[]): void {
    educationTargets.forEach((target) => {
      this.educationTargetsArray.push(this.createEducationTargetGroup(target));
    });
  }

  addEducationTarget(): void {
    this.educationTargetsArray.push(this.createEducationTargetGroup());
  }

  removeEducationTarget(index: number): void {
    const educationTarget = this.educationTargetsArray.at(index).value;

    if (educationTarget.id > 0) {
      this.surveyService
        .deleteSurveyEducationTarget(educationTarget.id)
        .subscribe({
          error: (error) => {
            console.error('Error deleting education target', error);
          },
        });
    }

    this.educationTargetsArray.removeAt(index);
  }

  // Income ranges
  get incomeRangesArray(): FormArray {
    return this.targetingForm.get('incomeRanges') as FormArray;
  }

  createIncomeRangeGroup(data?: any): FormGroup {
    return this.fb.group({
      id: [data?.id || 0],
      surveyId: [this.surveyId],
      minIncome: [data?.minIncome || 0],
      maxIncome: [data?.maxIncome || 100000],
    });
  }

  populateIncomeRanges(incomeRanges: any[]): void {
    incomeRanges.forEach((range) => {
      this.incomeRangesArray.push(this.createIncomeRangeGroup(range));
    });
  }

  addIncomeRange(): void {
    this.incomeRangesArray.push(this.createIncomeRangeGroup());
  }

  removeIncomeRange(index: number): void {
    const incomeRange = this.incomeRangesArray.at(index).value;

    if (incomeRange.id > 0) {
      this.surveyService
        .deleteSurveyIncomeRangeTarget(incomeRange.id)
        .subscribe({
          error: (error) => {
            console.error('Error deleting income range target', error);
          },
        });
    }

    this.incomeRangesArray.removeAt(index);
  }

  // Countries
  get countriesArray(): FormArray {
    return this.targetingForm.get('countries') as FormArray;
  }

  createCountryGroup(data?: any): FormGroup {
    return this.fb.group({
      id: [data?.id || 0],
      surveyId: [this.surveyId],
      country: [data?.country || ''],
    });
  }

  populateCountries(countries: any[]): void {
    countries.forEach((country) => {
      this.countriesArray.push(this.createCountryGroup(country));
    });
  }

  addCountry(event: MatChipInputEvent): void {
    const value = (event.value || '').trim();

    if (value) {
      const countryGroup = this.createCountryGroup({
        country: value,
      });

      this.countriesArray.push(countryGroup);
    }

    // Clear the input value
    event.chipInput!.clear();
  }

  removeCountry(index: number): void {
    const country = this.countriesArray.at(index).value;

    if (country.id > 0) {
      this.surveyService.deleteSurveyCountryTarget(country.id).subscribe({
        error: (error) => {
          console.error('Error deleting country target', error);
        },
      });
    }

    this.countriesArray.removeAt(index);
  }

  // States
  get statesArray(): FormArray {
    return this.targetingForm.get('states') as FormArray;
  }

  createStateGroup(data?: any): FormGroup {
    return this.fb.group({
      id: [data?.id || 0],
      surveyId: [this.surveyId],
      state: [data?.state || ''],
      countryId: [data?.countryId || null],
    });
  }

  populateStates(states: any[]): void {
    states.forEach((state) => {
      this.statesArray.push(this.createStateGroup(state));
    });
  }

  addState(event: MatChipInputEvent): void {
    const value = (event.value || '').trim();

    if (value) {
      const stateGroup = this.createStateGroup({
        state: value,
      });

      this.statesArray.push(stateGroup);
    }

    // Clear the input value
    event.chipInput!.clear();
  }

  removeState(index: number): void {
    const state = this.statesArray.at(index).value;

    if (state.id > 0) {
      this.surveyService.deleteSurveyStateTarget(state.id).subscribe({
        error: (error) => {
          console.error('Error deleting state target', error);
        },
      });
    }

    this.statesArray.removeAt(index);
  }

  // Household sizes
  get householdSizesArray(): FormArray {
    return this.targetingForm.get('householdSizes') as FormArray;
  }

  createHouseholdSizeGroup(data?: any): FormGroup {
    return this.fb.group({
      id: [data?.id || 0],
      surveyId: [this.surveyId],
      minSize: [data?.minSize || 1],
      maxSize: [data?.maxSize || 10],
    });
  }

  populateHouseholdSizes(householdSizes: any[]): void {
    householdSizes.forEach((size) => {
      this.householdSizesArray.push(this.createHouseholdSizeGroup(size));
    });
  }

  addHouseholdSize(): void {
    this.householdSizesArray.push(this.createHouseholdSizeGroup());
  }

  removeHouseholdSize(index: number): void {
    const householdSize = this.householdSizesArray.at(index).value;

    if (householdSize.id > 0) {
      this.surveyService
        .deleteSurveyHouseholdSizeTarget(householdSize.id)
        .subscribe({
          error: (error) => {
            console.error('Error deleting household size target', error);
          },
        });
    }

    this.householdSizesArray.removeAt(index);
  }

  // Parental statuses
  get parentalStatusesArray(): FormArray {
    return this.targetingForm.get('parentalStatuses') as FormArray;
  }

  createParentalStatusGroup(data?: any): FormGroup {
    return this.fb.group({
      id: [data?.id || 0],
      surveyId: [this.surveyId],
      hasChildren: [data?.hasChildren === undefined ? true : data.hasChildren],
    });
  }

  populateParentalStatuses(parentalStatuses: any[]): void {
    parentalStatuses.forEach((status) => {
      this.parentalStatusesArray.push(this.createParentalStatusGroup(status));
    });
  }

  addParentalStatus(): void {
    this.parentalStatusesArray.push(this.createParentalStatusGroup());
  }

  removeParentalStatus(index: number): void {
    const status = this.parentalStatusesArray.at(index).value;

    if (status.id > 0) {
      this.surveyService.deleteSurveyParentalStatusTarget(status.id).subscribe({
        error: (error) => {
          console.error('Error deleting parental status target', error);
        },
      });
    }

    this.parentalStatusesArray.removeAt(index);
  }

  // Industries
  get industriesArray(): FormArray {
    return this.targetingForm.get('industries') as FormArray;
  }

  createIndustryGroup(data?: any): FormGroup {
    return this.fb.group({
      id: [data?.id || 0],
      surveyId: [this.surveyId],
      industry: [data?.industry || ''],
    });
  }

  populateIndustries(industries: any[]): void {
    industries.forEach((industry) => {
      this.industriesArray.push(this.createIndustryGroup(industry));
    });
  }

  addIndustry(event: MatChipInputEvent): void {
    const value = (event.value || '').trim();

    if (value) {
      const industryGroup = this.createIndustryGroup({
        industry: value,
      });

      this.industriesArray.push(industryGroup);
    }

    // Clear the input value
    event.chipInput!.clear();
  }

  removeIndustry(index: number): void {
    const industry = this.industriesArray.at(index).value;

    if (industry.id > 0) {
      this.surveyService.deleteSurveyIndustryTarget(industry.id).subscribe({
        error: (error) => {
          console.error('Error deleting industry target', error);
        },
      });
    }

    this.industriesArray.removeAt(index);
  }

  // Occupations
  get occupationsArray(): FormArray {
    return this.targetingForm.get('occupations') as FormArray;
  }

  createOccupationGroup(data?: any): FormGroup {
    return this.fb.group({
      id: [data?.id || 0],
      surveyId: [this.surveyId],
      occupation: [data?.occupation || ''],
    });
  }

  populateOccupations(occupations: any[]): void {
    occupations.forEach((occupation) => {
      this.occupationsArray.push(this.createOccupationGroup(occupation));
    });
  }

  addOccupation(event: MatChipInputEvent): void {
    const value = (event.value || '').trim();

    if (value) {
      const occupationGroup = this.createOccupationGroup({
        occupation: value,
      });

      this.occupationsArray.push(occupationGroup);
    }

    // Clear the input value
    event.chipInput!.clear();
  }

  removeOccupation(index: number): void {
    const occupation = this.occupationsArray.at(index).value;

    if (occupation.id > 0) {
      this.surveyService.deleteSurveyOccupationTarget(occupation.id).subscribe({
        error: (error) => {
          console.error('Error deleting occupation target', error);
        },
      });
    }

    this.occupationsArray.removeAt(index);
  }

  // Marital statuses
  get maritalStatusesArray(): FormArray {
    return this.targetingForm.get('maritalStatuses') as FormArray;
  }

  createMaritalStatusGroup(data?: any): FormGroup {
    return this.fb.group({
      id: [data?.id || 0],
      surveyId: [this.surveyId],
      maritalStatus: [data?.maritalStatus || ''],
    });
  }

  populateMaritalStatuses(maritalStatuses: any[]): void {
    maritalStatuses.forEach((status) => {
      this.maritalStatusesArray.push(this.createMaritalStatusGroup(status));
    });
  }

  addMaritalStatus(): void {
    this.maritalStatusesArray.push(this.createMaritalStatusGroup());
  }

  removeMaritalStatus(index: number): void {
    const status = this.maritalStatusesArray.at(index).value;

    if (status.id > 0) {
      this.surveyService.deleteSurveyMaritalStatusTarget(status.id).subscribe({
        error: (error) => {
          console.error('Error deleting marital status target', error);
        },
      });
    }

    this.maritalStatusesArray.removeAt(index);
  }

  // Interests
  get interestsArray(): FormArray {
    return this.targetingForm.get('interests') as FormArray;
  }

  createInterestGroup(data?: any): FormGroup {
    return this.fb.group({
      id: [data?.id || 0],
      surveyId: [this.surveyId],
      interest: [data?.interest || ''],
      minInterestLevel: [data?.minInterestLevel || 1],
    });
  }

  populateInterests(interests: any[]): void {
    interests.forEach((interest) => {
      this.interestsArray.push(this.createInterestGroup(interest));
    });
  }

  addInterest(event: MatChipInputEvent): void {
    const value = (event.value || '').trim();

    if (value) {
      const interestGroup = this.createInterestGroup({
        interest: value,
      });

      this.interestsArray.push(interestGroup);
    }

    // Clear the input value
    event.chipInput!.clear();
  }

  removeInterest(index: number): void {
    const interest = this.interestsArray.at(index).value;

    if (interest.id > 0) {
      this.surveyService.deleteSurveyInterestTarget(interest.id).subscribe({
        error: (error) => {
          console.error('Error deleting interest target', error);
        },
      });
    }

    this.interestsArray.removeAt(index);
  }

  // Save all targeting data

  saveTargeting(): void {
    this.saving = true;

    // Create observables for each type of target
    const saveOperations: any[] = [];

    // Age ranges
    this.ageRangesArray.controls.forEach((control) => {
      const ageRange = control.value;
      if (ageRange.id === 0) {
        // Create new
        saveOperations.push(
          this.surveyService.addSurveyAgeRangeTarget(ageRange)
        );
      }
      // For existing items, we don't need to update as they're deleted and recreated
    });

    // Gender targets
    this.genderTargetsArray.controls.forEach((control) => {
      const genderTarget = control.value;
      if (genderTarget.id === 0) {
        saveOperations.push(
          this.surveyService.addSurveyGenderTarget(genderTarget)
        );
      }
    });

    // Education targets
    this.educationTargetsArray.controls.forEach((control) => {
      const educationTarget = control.value;
      if (educationTarget.id === 0) {
        saveOperations.push(
          this.surveyService.addSurveyEducationTarget(educationTarget)
        );
      }
    });

    // Income ranges
    this.incomeRangesArray.controls.forEach((control) => {
      const incomeRange = control.value;
      if (incomeRange.id === 0) {
        saveOperations.push(
          this.surveyService.addSurveyIncomeRangeTarget(incomeRange)
        );
      }
    });

    // Countries
    this.countriesArray.controls.forEach((control) => {
      const country = control.value;
      if (country.id === 0) {
        saveOperations.push(this.surveyService.addSurveyCountryTarget(country));
      }
    });

    // States
    this.statesArray.controls.forEach((control) => {
      const state = control.value;
      if (state.id === 0) {
        saveOperations.push(this.surveyService.addSurveyStateTarget(state));
      }
    });

    // Household sizes
    this.householdSizesArray.controls.forEach((control) => {
      const householdSize = control.value;
      if (householdSize.id === 0) {
        saveOperations.push(
          this.surveyService.addSurveyHouseholdSizeTarget(householdSize)
        );
      }
    });

    // Parental statuses
    this.parentalStatusesArray.controls.forEach((control) => {
      const parentalStatus = control.value;
      if (parentalStatus.id === 0) {
        saveOperations.push(
          this.surveyService.addSurveyParentalStatusTarget(parentalStatus)
        );
      }
    });

    // Industries
    this.industriesArray.controls.forEach((control) => {
      const industry = control.value;
      if (industry.id === 0) {
        saveOperations.push(
          this.surveyService.addSurveyIndustryTarget(industry)
        );
      }
    });

    // Occupations
    this.occupationsArray.controls.forEach((control) => {
      const occupation = control.value;
      if (occupation.id === 0) {
        saveOperations.push(
          this.surveyService.addSurveyOccupationTarget(occupation)
        );
      }
    });

    // Marital statuses
    this.maritalStatusesArray.controls.forEach((control) => {
      const maritalStatus = control.value;
      if (maritalStatus.id === 0) {
        saveOperations.push(
          this.surveyService.addSurveyMaritalStatusTarget(maritalStatus)
        );
      }
    });

    // Interests
    this.interestsArray.controls.forEach((control) => {
      const interest = control.value;
      if (interest.id === 0) {
        saveOperations.push(
          this.surveyService.addSurveyInterestTarget(interest)
        );
      }
    });

    // Dispatch all save operations
    if (saveOperations.length > 0) {
      forkJoin(saveOperations)
        .pipe(
          catchError((error) => {
            console.error('Error saving targeting data', error);
            this.snackBar.open(
              'Error saving targeting data. Please try again.',
              'Close',
              {
                duration: 5000,
              }
            );
            return of(null);
          }),
          finalize(() => {
            this.saving = false;
            // Reload the data to get the new IDs
            this.loadTargetingData();
          })
        )
        .subscribe(() => {
          this.snackBar.open(
            'Targeting criteria saved successfully!',
            'Close',
            {
              duration: 3000,
            }
          );
        });
    } else {
      this.saving = false;
      this.snackBar.open('No changes to save.', 'Close', {
        duration: 3000,
      });
    }
  }
}
