using Microsoft.EntityFrameworkCore.Storage;
using WACAMS.Core.Entities;
using WACAMS.Core.Interfaces;
using WACAMS.Infrastructure.Data;

namespace WACAMS.Infrastructure.Repositories;

/// <summary>
/// Implementation of IUnitOfWork with DbContext management.
/// Wraps database operations in a transaction scope for atomicity.
/// Provides access to all repository instances through a single context.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly WacamContext _context;
    private bool _disposed = false;
    private IDbContextTransaction? _transaction;

    // Repository instances — lazy-initialized on first use
    private IRepository<Employee>? _employeeRepository;
    private IRepository<AttendanceRecord>? _attendanceRecordRepository;
    private IRepository<AttendanceException>? _attendanceExceptionRepository;
    private IRepository<WarningLetter>? _warningLetterRepository;
    private IRepository<Escalation>? _escalationRepository;
    private IRepository<AuditLog>? _auditLogRepository;
    private IRepository<Department>? _departmentRepository;
    private IRepository<ShiftSchedule>? _shiftScheduleRepository;
    private IRepository<User>? _userRepository;

    public UnitOfWork(WacamContext context)
    {
        _context = context;
    }

    #region Repository Accessors

    public IRepository<Employee> Employees => _employeeRepository ??= new Repository<Employee>(_context);
    public IRepository<AttendanceRecord> AttendanceRecords => _attendanceRecordRepository ??= new Repository<AttendanceRecord>(_context);
    public IRepository<AttendanceException> AttendanceExceptions => _attendanceExceptionRepository ??= new Repository<AttendanceException>(_context);
    public IRepository<WarningLetter> WarningLetters => _warningLetterRepository ??= new Repository<WarningLetter>(_context);
    public IRepository<Escalation> Escalations => _escalationRepository ??= new Repository<Escalation>(_context);
    public IRepository<AuditLog> AuditLogs => _auditLogRepository ??= new Repository<AuditLog>(_context);
    public IRepository<Department> Departments => _departmentRepository ??= new Repository<Department>(_context);
    public IRepository<ShiftSchedule> ShiftSchedules => _shiftScheduleRepository ??= new Repository<ShiftSchedule>(_context);
    public IRepository<User> Users => _userRepository ??= new Repository<User>(_context);

    #endregion

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void BeginTransaction()
    {
        if (_transaction != null)
            throw new InvalidOperationException("A transaction is already active.");

        _transaction = _context.Database.BeginTransaction();
    }

    /// <inheritdoc />
    public async Task CommitAsync()
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction is active. Call BeginTransaction() first.");

        try
        {
            await _context.SaveChangesAsync();
            await _transaction.CommitAsync();
        }
        catch
        {
            await RollbackAsync();
            throw;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    /// <inheritdoc />
    public async Task RollbackAsync()
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction is active.");

        await _transaction.RollbackAsync();
        _transaction.Dispose();
        _transaction = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _context.Dispose();
            _transaction?.Dispose();
            _disposed = true;
        }
    }

}
