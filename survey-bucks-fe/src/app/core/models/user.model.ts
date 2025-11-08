export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  token?: string;
  phoneNumber?: string;
  registrationDate?: Date;
  emailConfirmed?: boolean;
}

export enum UserRole {
  Admin = 'Admin',
  Client = 'Client',
}
