builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IAttendanceRecordRepository, AttendanceRecordRepository>();
builder.Services.AddScoped<IDisciplinaryService, DisciplinaryService>();
builder.Services.AddScoped<ICorrectiveActionRepository, CorrectiveActionRepository>();
builder.Services.AddScoped<IExceptionRepository, ExceptionRepository>();
builder.Services.AddScoped<IExceptionService, ExceptionService>();
builder.Services.AddScoped<IShiftScheduleRepository, ShiftScheduleRepository>();
builder.Services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();

// Azure Blob Storage
builder.Services.AddSingleton(sp => 
    new Azure.Storage.Blobs.BlobServiceClient(builder.Configuration.GetConnectionString("AzureBlobStorage")));
builder.Services.AddScoped<IFileStorageService>(sp =>
{
    var blobServiceClient = sp.GetRequiredService<Azure.Storage.Blobs.BlobServiceClient>();
    var logger = sp.GetRequiredService<ILogger<AzureBlobStorageService>>();
    return new AzureBlobStorageService(blobServiceClient, "documents", logger);
});

// Corrective Action Form Service
builder.Services.AddScoped<ICorrectiveActionFormService, CorrectiveActionFormService>();
