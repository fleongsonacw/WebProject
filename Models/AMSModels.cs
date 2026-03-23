namespace AMS.Models;

public enum UserRole 
{ 
    Cleaner,
    Supervisor, 
    Manager, 
    Admin, 
    SystemAdmin 
}

public enum RequestStatus { Pending, Approved, Rejected }

// Helper Classes for Reports/Dashboard
public class TimesheetEntry 
{ 
    public string EmployeeName { get; set; } = string.Empty; 
    public string Date { get; set; } = string.Empty; 
    public bool IsPunchIn { get; set; } 
}

public class ExceptionEntry 
{ 
    public string EmployeeName { get; set; } = string.Empty; 
    public string SiteName { get; set; } = string.Empty; 
    public string Issue { get; set; } = string.Empty; 
}

public class ActiveEmployee 
{
    public string Name { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public string ClockedInAt { get; set; } = string.Empty;
}

// Database Entities
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;      // Added to fix Index.razor errors
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty; // ADDED THIS TO FIX CS1061
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Site
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;   // Added to fix DbContext errors
}

public class PunchRecord
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int SiteId { get; set; }
    public Site? Site { get; set; }
    public DateTime PunchTime { get; set; }
    public bool IsPunchIn { get; set; }
}

public class CorrectionRequest
{
    public int Id { get; set; }
    public int? PunchRecordId { get; set; }
    public PunchRecord? OriginalRecord { get; set; }
    public DateTime RequestedTime { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public DateTime RequestDate { get; set; } = DateTime.Now;
}

public class AuditLog 
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}