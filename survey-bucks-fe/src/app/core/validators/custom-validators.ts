import { ValidatorFn, AbstractControl, ValidationErrors } from "@angular/forms";


export class CustomValidators {
    static passwordMatch(passwordControlName: string, confirmPasswordControlName: string): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const password = control.get(passwordControlName);
      const confirmPassword = control.get(confirmPasswordControlName);

      if (!password || !confirmPassword) {
        return null;
      }

      if (password.value !== confirmPassword.value) {
        confirmPassword.setErrors({ passwordMismatch: true });
        return { passwordMismatch: true };
      } else {
        // Remove passwordMismatch error but keep other errors
        const errors = confirmPassword.errors;
        if (errors) {
          delete errors['passwordMismatch'];
          confirmPassword.setErrors(Object.keys(errors).length ? errors : null);
        }
        return null;
      }
    };
  }

  static strongPassword(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const value = control.value;
      if (!value) {
        return null;
      }

      const hasUpperCase = /[A-Z]/.test(value);
      const hasLowerCase = /[a-z]/.test(value);
      const hasNumeric = /[0-9]/.test(value);
      const hasSpecial = /[!@#$%^&*(),.?":{}|<>]/.test(value);
      const isLengthValid = value.length >= 8;

      const passwordValid = hasUpperCase && hasLowerCase && hasNumeric && hasSpecial && isLengthValid;

      return passwordValid ? null : {
        strongPassword: {
          hasUpperCase,
          hasLowerCase,
          hasNumeric,
          hasSpecial,
          isLengthValid
        }
      };
    };
  }
}