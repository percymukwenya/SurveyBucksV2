import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { forkJoin, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class AdminSurveyService {
  
  private apiUrl = `${environment.apiUrl}/api/admin/surveys`;
  private sectionsUrl = `${environment.apiUrl}/api/admin/sections`;
  private questionsUrl = `${environment.apiUrl}/api/admin/questions`;

  constructor(private http: HttpClient) {}

  getAllSurveys(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl);
  }

  getSurveyById(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  createSurvey(surveyData: any): Observable<any> {
    return this.http.post<any>(this.apiUrl, surveyData);
  }

  updateSurvey(id: number, surveyData: any): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, surveyData);
  }

  updateSurveyStatus(id: number, status: string): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}/status`, { status });
  }

  deleteSurvey(id: number): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`);
  }

  publishSurvey(id: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${id}/publish`, {});
  }

  unpublishSurvey(id: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${id}/unpublish`, {});
  }

  closeSurvey(id: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${id}/close`, {});
  }

  duplicateSurvey(id: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${id}/duplicate`, {});
  }

  getSurveyAnalytics(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}/analytics`);
  }

  // Section endpoints
  getSurveySections(surveyId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.sectionsUrl}/survey/${surveyId}`);
  }

  getSectionById(sectionId: number): Observable<any> {
    return this.http.get<any>(`${this.sectionsUrl}/${sectionId}`);
  }

  createSection(sectionData: any): Observable<any> {
    return this.http.post<any>(this.sectionsUrl, sectionData);
  }

  updateSection(sectionId: number, sectionData: any): Observable<any> {
    return this.http.put<any>(`${this.sectionsUrl}/${sectionId}`, sectionData);
  }

  deleteSection(sectionId: number): Observable<any> {
    return this.http.delete<any>(`${this.sectionsUrl}/${sectionId}`);
  }

  reorderSections(surveyId: number, sectionOrders: any[]): Observable<any> {
    return this.http.post<any>(`${this.sectionsUrl}/reorder`, {
      surveyId: surveyId,
      sectionOrders: sectionOrders,
    });
  }

  // Question endpoints
  getQuestionTypes(): Observable<any[]> {
    return this.http.get<any[]>(`${this.questionsUrl}/types`);
  }

  getSectionQuestions(sectionId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.questionsUrl}/section/${sectionId}`);
  }

  getQuestionDetails(questionId: number): Observable<any> {
    return this.http.get<any>(`${this.questionsUrl}/${questionId}`);
  }

  createQuestion(questionData: any): Observable<any> {
    return this.http.post<any>(this.questionsUrl, questionData);
  }

  updateQuestion(questionId: number, questionData: any): Observable<any> {
    return this.http.put<any>(
      `${this.questionsUrl}/${questionId}`,
      questionData
    );
  }

  deleteQuestion(questionId: number): Observable<any> {
    return this.http.delete<any>(`${this.questionsUrl}/${questionId}`);
  }

  reorderQuestions(sectionId: number, questionOrders: any[]): Observable<any> {
    return this.http.post<any>(`${this.questionsUrl}/reorder`, {
      sectionId: sectionId,
      questionOrders: questionOrders,
    });
  }

  // Question choice endpoints
  getQuestionChoices(questionId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.questionsUrl}/${questionId}/choices`);
  }

  addQuestionChoice(choiceData: any): Observable<any> {
    return this.http.post<any>(`${this.questionsUrl}/choices`, choiceData);
  }

  updateQuestionChoice(choiceId: number, choiceData: any): Observable<any> {
    return this.http.put<any>(
      `${this.questionsUrl}/choices/${choiceId}`,
      choiceData
    );
  }

  deleteQuestionChoice(choiceId: number): Observable<any> {
    return this.http.delete<any>(`${this.questionsUrl}/choices/${choiceId}`);
  }

  // Matrix operations
  getMatrixRows(questionId: number): Observable<any[]> {
    return this.http.get<any[]>(
      `${this.questionsUrl}/${questionId}/matrix/rows`
    );
  }

  getMatrixColumns(questionId: number): Observable<any[]> {
    return this.http.get<any[]>(
      `${this.questionsUrl}/${questionId}/matrix/columns`
    );
  }

  addMatrixRow(rowData: any): Observable<any> {
    return this.http.post<any>(`${this.questionsUrl}/matrix/rows`, rowData);
  }

  addMatrixColumn(columnData: any): Observable<any> {
    return this.http.post<any>(
      `${this.questionsUrl}/matrix/columns`,
      columnData
    );
  }

  updateMatrixRow(id: number, row: any): Observable<any> {
  return this.http.put<any>(`${this.questionsUrl}/matrix/rows/${id}`, row);
}

updateMatrixColumn(id: number, column: any): Observable<any> {
  return this.http.put<any>(`${this.questionsUrl}/matrix/columns/${id}`, column);
}

  deleteMatrixRow(rowId: number): Observable<any> {
    return this.http.delete<any>(`${this.questionsUrl}/matrix/rows/${rowId}`);
  }

  deleteMatrixColumn(columnId: number): Observable<any> {
    return this.http.delete<any>(
      `${this.questionsUrl}/matrix/columns/${columnId}`
    );
  }

  getQuestionLogicRules(questionId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.questionsUrl}/${questionId}/logic`);
  }

  // Add a new logic rule
  addQuestionLogicRule(logicRule: any): Observable<any> {
    return this.http.post<any>(`${this.questionsUrl}/logic`, logicRule);
  }

  // Update an existing logic rule
  updateQuestionLogicRule(
    logicRuleId: number,
    logicRule: any
  ): Observable<any> {
    return this.http.put<any>(
      `${this.questionsUrl}/logic/${logicRuleId}`,
      logicRule
    );
  }

  // Delete a logic rule
  deleteQuestionLogicRule(logicRuleId: number): Observable<any> {
    return this.http.delete<any>(`${this.questionsUrl}/logic/${logicRuleId}`);
  }

  getSurveyResults(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}/results`);
  }

  exportSurveyResults(id: number, format: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}/export?format=${format}`, {
      responseType: 'blob' as 'json',
    });
  }

  // Age Range Targets
  getSurveyAgeRangeTargets(surveyId: number): Observable<any[]> {
    return this.http.get<any[]>(
      `${this.apiUrl}/${surveyId}/targets/age-ranges`
    );
  }

  addSurveyAgeRangeTarget(target: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/targets/age-ranges`, target);
  }

  deleteSurveyAgeRangeTarget(targetId: number): Observable<any> {
    return this.http.delete<any>(
      `${this.apiUrl}/targets/age-ranges/${targetId}`
    );
  }

  // Gender Targets
  getSurveyGenderTargets(surveyId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${surveyId}/targets/genders`);
  }

  addSurveyGenderTarget(target: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/targets/genders`, target);
  }

  deleteSurveyGenderTarget(targetId: number): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/targets/genders/${targetId}`);
  }

  // Education Targets
  getSurveyEducationTargets(surveyId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${surveyId}/targets/education`);
  }

  addSurveyEducationTarget(target: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/targets/education`, target);
  }

  deleteSurveyEducationTarget(targetId: number): Observable<any> {
    return this.http.delete<any>(
      `${this.apiUrl}/targets/education/${targetId}`
    );
  }

  // Income Range Targets
  getSurveyIncomeRangeTargets(surveyId: number): Observable<any[]> {
    return this.http.get<any[]>(
      `${this.apiUrl}/${surveyId}/targets/income-ranges`
    );
  }

  addSurveyIncomeRangeTarget(target: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/targets/income-ranges`, target);
  }

  deleteSurveyIncomeRangeTarget(targetId: number): Observable<any> {
    return this.http.delete<any>(
      `${this.apiUrl}/targets/income-ranges/${targetId}`
    );
  }

  // Location/Country/State Targets
  getSurveyLocationTargets(surveyId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${surveyId}/targets/locations`);
  }

  addSurveyLocationTarget(target: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/targets/locations`, target);
  }

  deleteSurveyLocationTarget(targetId: number): Observable<any> {
    return this.http.delete<any>(
      `${this.apiUrl}/targets/locations/${targetId}`
    );
  }

  getSurveyCountryTargets(surveyId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${surveyId}/targets/countries`);
  }

  addSurveyCountryTarget(target: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/targets/countries`, target);
  }

  deleteSurveyCountryTarget(targetId: number): Observable<any> {
    return this.http.delete<any>(
      `${this.apiUrl}/targets/countries/${targetId}`
    );
  }

  getSurveyStateTargets(surveyId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${surveyId}/targets/states`);
  }

  addSurveyStateTarget(target: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/targets/states`, target);
  }

  deleteSurveyStateTarget(targetId: number): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/targets/states/${targetId}`);
  }

  // Industry Targets
  getSurveyIndustryTargets(surveyId: number): Observable<any[]> {
    return this.http.get<any[]>(
      `${this.apiUrl}/${surveyId}/targets/industries`
    );
  }

  addSurveyIndustryTarget(target: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/targets/industries`, target);
  }

  deleteSurveyIndustryTarget(targetId: number): Observable<any> {
    return this.http.delete<any>(
      `${this.apiUrl}/targets/industries/${targetId}`
    );
  }

  // Household Size Targets
  getSurveyHouseholdSizeTargets(surveyId: number): Observable<any[]> {
    return this.http.get<any[]>(
      `${this.apiUrl}/${surveyId}/targets/household-sizes`
    );
  }

  addSurveyHouseholdSizeTarget(target: any): Observable<any> {
    return this.http.post<any>(
      `${this.apiUrl}/targets/household-sizes`,
      target
    );
  }

  deleteSurveyHouseholdSizeTarget(targetId: number): Observable<any> {
    return this.http.delete<any>(
      `${this.apiUrl}/targets/household-sizes/${targetId}`
    );
  }

  // Parental Status Targets
  getSurveyParentalStatusTargets(surveyId: number): Observable<any[]> {
    return this.http.get<any[]>(
      `${this.apiUrl}/${surveyId}/targets/parental-status`
    );
  }

  addSurveyParentalStatusTarget(target: any): Observable<any> {
    return this.http.post<any>(
      `${this.apiUrl}/targets/parental-status`,
      target
    );
  }

  deleteSurveyParentalStatusTarget(targetId: number): Observable<any> {
    return this.http.delete<any>(
      `${this.apiUrl}/targets/parental-status/${targetId}`
    );
  }

  // Interest Targets
  getSurveyInterestTargets(surveyId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${surveyId}/targets/interests`);
  }

  addSurveyInterestTarget(target: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/targets/interests`, target);
  }

  deleteSurveyInterestTarget(targetId: number): Observable<any> {
    return this.http.delete<any>(
      `${this.apiUrl}/targets/interests/${targetId}`
    );
  }

  // Occupation Targets
  getSurveyOccupationTargets(surveyId: number): Observable<any[]> {
    return this.http.get<any[]>(
      `${this.apiUrl}/${surveyId}/targets/occupations`
    );
  }

  addSurveyOccupationTarget(target: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/targets/occupations`, target);
  }

  deleteSurveyOccupationTarget(targetId: number): Observable<any> {
    return this.http.delete<any>(
      `${this.apiUrl}/targets/occupations/${targetId}`
    );
  }

  // Marital Status Targets
  getSurveyMaritalStatusTargets(surveyId: number): Observable<any[]> {
    return this.http.get<any[]>(
      `${this.apiUrl}/${surveyId}/targets/marital-status`
    );
  }

  addSurveyMaritalStatusTarget(target: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/targets/marital-status`, target);
  }

  deleteSurveyMaritalStatusTarget(targetId: number): Observable<any> {
    return this.http.delete<any>(
      `${this.apiUrl}/targets/marital-status/${targetId}`
    );
  }

  // Convenience method to get all targets
  getAllSurveyTargets(surveyId: number): Observable<any> {
    return forkJoin({
      ageRanges: this.getSurveyAgeRangeTargets(surveyId),
      genders: this.getSurveyGenderTargets(surveyId),
      education: this.getSurveyEducationTargets(surveyId),
      incomeRanges: this.getSurveyIncomeRangeTargets(surveyId),
      locations: this.getSurveyLocationTargets(surveyId),
      countries: this.getSurveyCountryTargets(surveyId),
      states: this.getSurveyStateTargets(surveyId),
      industries: this.getSurveyIndustryTargets(surveyId),
      householdSizes: this.getSurveyHouseholdSizeTargets(surveyId),
      parentalStatus: this.getSurveyParentalStatusTargets(surveyId),
      interests: this.getSurveyInterestTargets(surveyId),
      occupations: this.getSurveyOccupationTargets(surveyId),
      maritalStatus: this.getSurveyMaritalStatusTargets(surveyId),
    });
  }
}
