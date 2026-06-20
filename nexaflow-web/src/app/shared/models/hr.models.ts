export type EmployeeStatus = 'Active' | 'OnLeave' | 'Terminated';

export interface EmployeeDto {
  id: string;
  fullName: string;
  firstName: string;
  lastName: string;
  email?: string;
  phone?: string;
  department: string;
  position: string;
  hireDate: string;
  baseSalary: number;
  allowances: number;
  branchId?: string;
  status: EmployeeStatus;
  createdAt: string;
}

export interface CreateEmployeeDto {
  firstName: string;
  lastName: string;
  email?: string;
  phone?: string;
  nationalId?: string;
  department: string;
  position: string;
  hireDate: string;
  baseSalary: number;
  allowances: number;
  branchId?: string;
}

export interface UpdateEmployeeDto {
  firstName: string;
  lastName: string;
  email?: string;
  phone?: string;
  department: string;
  position: string;
  baseSalary: number;
  allowances: number;
  branchId?: string;
  status: EmployeeStatus;
}

// ----- Attendance -----

export type AttendanceStatus = 'Present' | 'Absent' | 'OnLeave' | 'Late';

export interface AttendanceDto {
  id: string;
  employeeId: string;
  employeeName: string;
  date: string; // 'YYYY-MM-DD'
  checkIn?: string; // 'HH:mm'
  checkOut?: string; // 'HH:mm'
  status: AttendanceStatus;
  workingHours?: string; // 'HH:mm'
}

export interface AttendanceSummaryDto {
  date: string;
  present: number;
  absent: number;
  late: number;
  onLeave: number;
}

// ----- Leave requests -----

export type LeaveType = 'Annual' | 'Sick' | 'Emergency' | 'Unpaid';
export type LeaveStatus = 'Pending' | 'Approved' | 'Rejected';

export interface LeaveRequestDto {
  id: string;
  employeeId: string;
  employeeName: string;
  type: LeaveType;
  startDate: string;
  endDate: string;
  reason: string;
  status: LeaveStatus;
  totalDays: number;
  reviewNote?: string;
  reviewedByName?: string;
}

export interface CreateLeaveRequestDto {
  type: LeaveType;
  startDate: string;
  endDate: string;
  reason: string;
}

export interface ReviewLeaveDto {
  approved: boolean;
  reviewNote?: string;
}

// ----- Payroll -----

export interface PayslipDto {
  employeeId: string;
  employeeFullName: string;
  department: string;
  position: string;
  month: string;
  baseSalary: number;
  allowances: number;
  grossSalary: number;
  workingDays: number;
  presentDays: number;
  leaveDays: number;
  absentDays: number;
  dailyRate: number;
  absenceDeduction: number;
  netSalary: number;
}
