import { Injectable } from "@angular/core";
import { QuestionDetail } from "./survey.service";

@Injectable({
  providedIn: 'root'
})
export class SurveyValidationService {
    validateQuestion(question: QuestionDetail, value: any): string[] {
    const errors: string[] = [];
    
    // Required validation
    if (question.isMandatory && this.isEmpty(value)) {
      errors.push('This question is required');
      return errors;
    }
    
    if (this.isEmpty(value)) {
      return errors; // No further validation for empty optional fields
    }
    
    // Type-specific validation
    switch (question.questionTypeName) {
      case 'Email':
        if (!this.isValidEmail(value)) {
          errors.push('Please enter a valid email address');
        }
        break;
        
      case 'Phone':
        if (!this.isValidPhone(value)) {
          errors.push('Please enter a valid phone number');
        }
        break;
        
      case 'NumberInput':
        if (!this.isValidNumber(value)) {
          errors.push('Please enter a valid number');
        } else {
          const numValue = parseFloat(value);
          if (question.minValue !== undefined && numValue < question.minValue) {
            errors.push(`Value must be at least ${question.minValue}`);
          }
          if (question.maxValue !== undefined && numValue > question.maxValue) {
            errors.push(`Value must be at most ${question.maxValue}`);
          }
        }
        break;
        
      case 'MultipleChoice':
        if (Array.isArray(value)) {
          // Check for exclusive options
          const exclusiveOptions = question.responseChoices
            .filter(choice => choice.isExclusiveOption)
            .map(choice => choice.value);
          
          const selectedExclusive = value.filter(v => exclusiveOptions.includes(v));
          if (selectedExclusive.length > 0 && value.length > 1) {
            errors.push('Cannot select other options when an exclusive option is selected');
          }
        }
        break;
        
      case 'Matrix':
        if (question.isMandatory && typeof value === 'object') {
          const answeredRows = Object.keys(value).filter(key => value[key] !== null && value[key] !== undefined);
          if (answeredRows.length < question.matrixRows.length) {
            errors.push('Please answer all matrix rows');
          }
        }
        break;
    }
    
    return errors;
  }

  private isEmpty(value: any): boolean {
    if (value === null || value === undefined || value === '') {
      return true;
    }
    if (Array.isArray(value)) {
      return value.length === 0;
    }
    if (typeof value === 'object') {
      return Object.keys(value).length === 0;
    }
    return false;
  }
  
  private isValidEmail(email: string): boolean {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }
  
  private isValidPhone(phone: string): boolean {
    const phoneRegex = /^\+?[\d\s\-\(\)]{10,}$/;
    return phoneRegex.test(phone);
  }
  
  private isValidNumber(value: string): boolean {
    return !isNaN(parseFloat(value)) && isFinite(parseFloat(value));
  }
}