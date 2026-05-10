using Microsoft.Data.SqlClient;
using StudentEvalAPI;
using StudentEvalAPI.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var connString = builder.Configuration.GetConnectionString("StudentEvalDB")!;
builder.Services.AddSingleton(new DBHelper(connString));

builder.WebHost.UseUrls(
    "http://0.0.0.0:5103",
    "https://0.0.0.0:7130");

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");

// ============================================================
// AUTH
// ============================================================

app.MapPost("/api/login", (LoginRequest req, DBHelper db) =>
{
    if (string.IsNullOrEmpty(req.Username) || string.IsNullOrEmpty(req.Password))
        return Results.Ok(new LoginResponse { Success = false, Message = "Please enter username and password." });

    var sql = "SELECT UserID, FullName, Role, ISNULL(StudentNo,'') AS StudentNo, CourseID FROM Users WHERE Username = @u AND PasswordHash = @p AND IsActive = 1";
    var dt = db.GetDataTable(sql, new SqlParameter("@u", req.Username), new SqlParameter("@p", req.Password));

    if (dt.Rows.Count > 0)
    {
        var row = dt.Rows[0];
        return Results.Ok(new LoginResponse
        {
            Success = true,
            Message = "Login successful.",
            UserID = Convert.ToInt32(row["UserID"]),
            FullName = row["FullName"].ToString()!,
            Role = row["Role"].ToString()!,
            StudentNo = row["StudentNo"].ToString()!,
            CourseID = row["CourseID"] == DBNull.Value ? null : Convert.ToInt32(row["CourseID"])
        });
    }

    return Results.Ok(new LoginResponse { Success = false, Message = "Invalid username or password." });
});

// ============================================================
// DASHBOARD
// ============================================================

app.MapGet("/api/dashboard/stats", (DBHelper db) =>
{
    var stats = new DashboardStats
    {
        TotalStudents = Convert.ToInt32(db.ExecuteScalar("SELECT COUNT(*) FROM Users WHERE Role='Student' AND IsActive=1")),
        WithLackings = Convert.ToInt32(db.ExecuteScalar("SELECT COUNT(DISTINCT StudentID) FROM Lackings WHERE IsResolved=0")),
        GradCandidates = Convert.ToInt32(db.ExecuteScalar("SELECT COUNT(*) FROM GraduationApplications WHERE Status='Pending'")),
        TotalSubjects = Convert.ToInt32(db.ExecuteScalar("SELECT COUNT(*) FROM Subjects WHERE IsActive=1"))
    };
    return Results.Ok(stats);
});

app.MapGet("/api/dashboard/recent-lackings", (DBHelper db) =>
{
    var sql = "SELECT TOP 10 u.StudentNo, u.FullName, l.LackingType, l.CreatedAt FROM Lackings l INNER JOIN Users u ON l.StudentID = u.UserID WHERE l.IsResolved = 0 ORDER BY l.CreatedAt DESC";
    var dt = db.GetDataTable(sql);
    var list = new List<RecentLacking>();
    foreach (System.Data.DataRow row in dt.Rows)
        list.Add(new RecentLacking { StudentNo = row["StudentNo"].ToString()!, FullName = row["FullName"].ToString()!, LackingType = row["LackingType"].ToString()!, CreatedAt = Convert.ToDateTime(row["CreatedAt"]) });
    return Results.Ok(list);
});

app.MapGet("/api/dashboard/recent-grad-apps", (DBHelper db) =>
{
    var sql = "SELECT TOP 10 u.StudentNo, u.FullName, ga.Status, ga.AppliedAt FROM GraduationApplications ga INNER JOIN Users u ON ga.StudentID = u.UserID ORDER BY ga.AppliedAt DESC";
    var dt = db.GetDataTable(sql);
    var list = new List<RecentGradApp>();
    foreach (System.Data.DataRow row in dt.Rows)
        list.Add(new RecentGradApp { StudentNo = row["StudentNo"].ToString()!, FullName = row["FullName"].ToString()!, Status = row["Status"].ToString()!, AppliedAt = Convert.ToDateTime(row["AppliedAt"]) });
    return Results.Ok(list);
});

// ============================================================
// STUDENT - GRADES
// ============================================================

app.MapGet("/api/student/{studentId}/grades", (int studentId, DBHelper db) =>
{
    var sql = "SELECT e.EvaluationID, sub.SubjectCode, sub.SubjectTitle, sub.Units, sem.SemNumber, sem.SchoolYear, e.Grade, ISNULL(e.Remarks,'') AS Remarks, e.IsComplete FROM Evaluations e INNER JOIN Subjects sub ON e.SubjectID = sub.SubjectID INNER JOIN Semesters sem ON e.SemesterID = sem.SemesterID WHERE e.StudentID = @sid ORDER BY sem.SchoolYear, sem.SemNumber, sub.SubjectCode";
    var dt = db.GetDataTable(sql, new SqlParameter("@sid", studentId));
    var list = new List<SubjectGrade>();
    foreach (System.Data.DataRow row in dt.Rows)
        list.Add(new SubjectGrade { EvaluationID = Convert.ToInt32(row["EvaluationID"]), SubjectCode = row["SubjectCode"].ToString()!, SubjectTitle = row["SubjectTitle"].ToString()!, Units = Convert.ToInt32(row["Units"]), SemNumber = Convert.ToInt32(row["SemNumber"]), SchoolYear = row["SchoolYear"].ToString()!, Grade = row["Grade"] == DBNull.Value ? null : Convert.ToDecimal(row["Grade"]), Remarks = row["Remarks"].ToString()!, IsComplete = Convert.ToBoolean(row["IsComplete"]) });
    return Results.Ok(list);
});

// ============================================================
// STUDENT - LACKINGS
// ============================================================

app.MapGet("/api/student/{studentId}/lackings", (int studentId, DBHelper db) =>
{
    var sql = "SELECT LackingID, LackingType, ISNULL(Description,'') AS Description, IsResolved, CreatedAt FROM Lackings WHERE StudentID = @sid ORDER BY IsResolved, CreatedAt DESC";
    var dt = db.GetDataTable(sql, new SqlParameter("@sid", studentId));
    var list = new List<Lacking>();
    foreach (System.Data.DataRow row in dt.Rows)
        list.Add(new Lacking { LackingID = Convert.ToInt32(row["LackingID"]), LackingType = row["LackingType"].ToString()!, Description = row["Description"].ToString()!, IsResolved = Convert.ToBoolean(row["IsResolved"]), CreatedAt = Convert.ToDateTime(row["CreatedAt"]) });
    return Results.Ok(list);
});

// ============================================================
// STUDENT - DOCUMENTS
// ============================================================

app.MapGet("/api/student/{studentId}/documents", (int studentId, DBHelper db) =>
{
    var sql = "SELECT DocumentID, DocumentType, FileName, IsVerified, UploadedAt FROM Documents WHERE StudentID = @sid ORDER BY UploadedAt DESC";
    var dt = db.GetDataTable(sql, new SqlParameter("@sid", studentId));
    var list = new List<Document>();
    foreach (System.Data.DataRow row in dt.Rows)
        list.Add(new Document { DocumentID = Convert.ToInt32(row["DocumentID"]), DocumentType = row["DocumentType"].ToString()!, FileName = row["FileName"].ToString()!, IsVerified = Convert.ToBoolean(row["IsVerified"]), UploadedAt = Convert.ToDateTime(row["UploadedAt"]) });
    return Results.Ok(list);
});

// POST /api/student/upload-document
app.MapPost("/api/student/upload-document", async (HttpRequest request, DBHelper db) =>
{
    var form = await request.ReadFormAsync();
    var file = form.Files.GetFile("file");
    var studentId = form["studentId"].ToString();
    var documentType = form["documentType"].ToString();

    if (file == null || string.IsNullOrEmpty(studentId))
        return Results.BadRequest("Missing file or studentId.");

    // I-change sa actual path ng web project uploads folder
    var uploadFolder = @"C:\MobileApp\StudentEvaluation\StudentEvaluation\Uploads\Documents";
    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

    var uniqueName = $"{studentId}_{DateTime.Now:yyyyMMddHHmmss}_{file.FileName}";
    var fullPath = Path.Combine(uploadFolder, uniqueName);
    var relativePath = $"~/Uploads/Documents/{uniqueName}";

    using (var stream = new FileStream(fullPath, FileMode.Create))
        await file.CopyToAsync(stream);

    db.ExecuteNonQuery("INSERT INTO Documents (StudentID, DocumentType, FilePath, FileName, FileSize) VALUES (@sid, @dtype, @fpath, @fname, @fsize)",
        new SqlParameter("@sid", int.Parse(studentId)),
        new SqlParameter("@dtype", documentType),
        new SqlParameter("@fpath", relativePath),
        new SqlParameter("@fname", file.FileName),
        new SqlParameter("@fsize", file.Length));

    return Results.Ok(new { message = "Uploaded successfully." });
});
// ============================================================
// STUDENT - GRADUATION
// ============================================================

app.MapGet("/api/student/{studentId}/graduation", (int studentId, DBHelper db) =>
{
    var sql = "SELECT ga.AppID, 'Sem ' + CAST(s.SemNumber AS NVARCHAR) + ' - ' + s.SchoolYear AS SemDisplay, ga.Status, ISNULL(ga.Remarks,'') AS Remarks, ga.AppliedAt FROM GraduationApplications ga INNER JOIN Semesters s ON ga.SemesterID = s.SemesterID WHERE ga.StudentID = @sid ORDER BY ga.AppliedAt DESC";
    var dt = db.GetDataTable(sql, new SqlParameter("@sid", studentId));
    var list = new List<GraduationApp>();
    foreach (System.Data.DataRow row in dt.Rows)
        list.Add(new GraduationApp { AppID = Convert.ToInt32(row["AppID"]), SemDisplay = row["SemDisplay"].ToString()!, Status = row["Status"].ToString()!, Remarks = row["Remarks"].ToString()!, AppliedAt = Convert.ToDateTime(row["AppliedAt"]) });
    return Results.Ok(list);
});

// GET /api/semesters
app.MapGet("/api/semesters", (DBHelper db) =>
{
    var dt = db.GetDataTable("SELECT SemesterID, 'Sem ' + CAST(SemNumber AS NVARCHAR) + ' - ' + SchoolYear AS Display FROM Semesters ORDER BY SchoolYear DESC, SemNumber");
    var list = new List<object>();
    foreach (System.Data.DataRow row in dt.Rows)
        list.Add(new { SemesterID = Convert.ToInt32(row["SemesterID"]), Display = row["Display"].ToString() });
    return Results.Ok(list);
});

// POST /api/student/apply-graduation
app.MapPost("/api/student/apply-graduation", (GraduationRequest req, DBHelper db) =>
{
    var exists = db.ExecuteScalar("SELECT COUNT(*) FROM GraduationApplications WHERE StudentID=@sid AND SemesterID=@semid",
        new SqlParameter("@sid", req.StudentId),
        new SqlParameter("@semid", req.SemesterId));

    if (Convert.ToInt32(exists) > 0)
        return Results.BadRequest("already applied");

    db.ExecuteNonQuery("INSERT INTO GraduationApplications (StudentID, SemesterID, Status, Remarks) VALUES (@sid, @semid, 'Pending', @rem)",
        new SqlParameter("@sid", req.StudentId),
        new SqlParameter("@semid", req.SemesterId),
        new SqlParameter("@rem", req.Remarks ?? ""));

    return Results.Ok(new { message = "Applied successfully." });
});

app.Run();