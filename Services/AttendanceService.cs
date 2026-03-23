using AMS.Data;
using AMS.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace AMS.Services;

public class AttendanceService
{
    private readonly AmsDbContext _db;
    public User? CurrentUser { get; private set; }
    public event Action? OnNotify;

    public AttendanceService(AmsDbContext db) => _db = db;

    public async Task<User?> Login(string email, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.Password == password);
        if (user != null) { CurrentUser = user; OnNotify?.Invoke(); }
        return user;
    }

    public void Logout() { CurrentUser = null; OnNotify?.Invoke(); }

    public async Task<List<User>> GetAllUsers() => await _db.Users.ToListAsync();
    public async Task<List<Site>> GetSites() => await _db.Sites.ToListAsync();

    public async Task SaveUser(User user)
    {
        if (user.Id == 0) _db.Users.Add(user);
        else _db.Users.Update(user);
        await _db.SaveChangesAsync();
    }

    // Supervisor Dashboard
    public async Task<List<ActiveEmployee>> GetActiveEmployees()
    {
        var punches = await _db.PunchRecords.Include(p => p.User).Include(p => p.Site)
            .Where(p => p.PunchTime.Date == DateTime.Today).ToListAsync();

        return punches.GroupBy(p => p.UserId)
            .Select(g => g.OrderByDescending(p => p.PunchTime).First())
            .Where(p => p.IsPunchIn)
            .Select(p => new ActiveEmployee {
                Name = p.User?.FullName ?? "Unknown",
                SiteName = p.Site?.Name ?? "N/A",
                ClockedInAt = p.PunchTime.ToString("hh:mm tt")
            }).ToList();
    }

      public async Task<List<PunchRecord>> GetAllRawPunches()
    {
        return await _db.PunchRecords.Include(p => p.User).Include(p => p.Site)
            .OrderByDescending(p => p.PunchTime).ToListAsync();
    }

    public async Task<List<PunchRecord>> GetUserTodayRecords()
    {
        if (CurrentUser == null) return new();
        return await _db.PunchRecords.Include(p => p.Site)
            .Where(p => p.UserId == CurrentUser.Id && p.PunchTime.Date == DateTime.Today)
            .OrderByDescending(p => p.PunchTime).ToListAsync();
    }

    public async Task<bool> RecordPunch(int siteId, bool isPunchIn)
    {
        if (CurrentUser == null) return false;
        var record = new PunchRecord { UserId = CurrentUser.Id, SiteId = siteId, PunchTime = DateTime.Now, IsPunchIn = isPunchIn };
        _db.PunchRecords.Add(record);
        await _db.SaveChangesAsync();
        OnNotify?.Invoke();
        return true;
    }

    public async Task<List<CorrectionRequest>> GetPendingCorrections() =>
        await _db.CorrectionRequests.Include(r => r.OriginalRecord).ThenInclude(p => p!.User)
            .Where(r => r.Status == RequestStatus.Pending).ToListAsync();

    public async Task UpdateCorrectionStatus(CorrectionRequest req, RequestStatus status)
    {
        var dbReq = await _db.CorrectionRequests.FindAsync(req.Id);
        if (dbReq != null) { dbReq.Status = status; await _db.SaveChangesAsync(); }
    }

    public async Task SubmitCorrection(int punchId, DateTime requestedTime, string reason)
    {
        var request = new CorrectionRequest {
            PunchRecordId = punchId,
            RequestedTime = requestedTime,
            Reason = reason,
            Status = RequestStatus.Pending,
            RequestDate = DateTime.Now
        };
        _db.CorrectionRequests.Add(request);
        await _db.SaveChangesAsync();
    }

    public async Task<List<TimesheetEntry>> GetTimesheetReport()
    {
        return await _db.PunchRecords.Include(p => p.User).OrderByDescending(p => p.PunchTime)
            .Select(p => new TimesheetEntry { 
                EmployeeName = p.User!.FullName, 
                Date = p.PunchTime.ToString("yyyy-MM-dd HH:mm"), 
                IsPunchIn = p.IsPunchIn 
            }).ToListAsync();
    }

    public async Task<List<ExceptionEntry>> GetExceptionReport()
    {
        var punches = await _db.PunchRecords.Include(p => p.User).Include(p => p.Site)
            .Where(p => p.PunchTime.Date == DateTime.Today).ToListAsync();

        return punches.GroupBy(p => p.UserId).Where(g => g.Count() % 2 != 0)
            .Select(g => new ExceptionEntry { 
                EmployeeName = g.First().User?.FullName ?? "Unknown", 
                SiteName = g.First().Site?.Name ?? "N/A", 
                Issue = "Missing Punch Out" 
            }).ToList();
    }

    public async Task<string> GetTimesheetCsv()
    {
        var records = await _db.PunchRecords.Include(p => p.User).ToListAsync();
        var csv = new StringBuilder().AppendLine("Employee,Date,Type");
        foreach (var r in records) csv.AppendLine($"{r.User?.FullName},{r.PunchTime},{(r.IsPunchIn ? "IN" : "OUT")}");
        return csv.ToString();
    }
}