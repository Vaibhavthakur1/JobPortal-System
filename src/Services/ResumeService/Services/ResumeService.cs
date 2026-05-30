using ResumeService.Models;
using ResumeService.Repositories;
using System.Text;

namespace ResumeService.Services;

public class ResumeService(IResumeRepository repo) : IResumeService
{
    public async Task<ResumeDto> CreateAsync(Guid userId, CreateResumeRequest req)
    {
        var existing = await repo.GetByUserAsync(userId);
        var isFirst = !existing.Any();

        var resume = new Resume
        {
            UserId = userId,
            Title = req.Title,
            Template = req.Template,
            IsDefault = isFirst,
            Skills = req.Skills,
            Personal = MapPersonal(Guid.Empty, req.Personal),
            Educations = req.Educations.Select(e => MapEducation(Guid.Empty, e)).ToList(),
            Experiences = req.Experiences.Select(e => MapExperience(Guid.Empty, e)).ToList(),
            Projects = req.Projects.Select(p => MapProject(Guid.Empty, p)).ToList()
        };

        // Fix foreign keys after Id is set
        resume.Personal.ResumeId = resume.Id;
        resume.Educations.ForEach(e => e.ResumeId = resume.Id);
        resume.Experiences.ForEach(e => e.ResumeId = resume.Id);
        resume.Projects.ForEach(p => p.ResumeId = resume.Id);

        await repo.AddAsync(resume);
        return MapToDto(resume);
    }

    public async Task<ResumeDto> UpdateAsync(Guid resumeId, Guid userId, UpdateResumeRequest req)
    {
        var resume = await repo.GetByIdAsync(resumeId)
            ?? throw new KeyNotFoundException("Resume not found.");

        if (resume.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized.");

        if (req.Title is not null) resume.Title = req.Title;
        if (req.Template is not null) resume.Template = req.Template;
        if (req.Skills is not null) resume.Skills = req.Skills;

        if (req.Personal is not null)
        {
            resume.Personal.FullName = req.Personal.FullName;
            resume.Personal.Email = req.Personal.Email;
            resume.Personal.Phone = req.Personal.Phone;
            resume.Personal.Location = req.Personal.Location;
            resume.Personal.LinkedInUrl = req.Personal.LinkedInUrl;
            resume.Personal.GitHubUrl = req.Personal.GitHubUrl;
            resume.Personal.Summary = req.Personal.Summary;
        }

        if (req.Educations is not null)
        {
            resume.Educations.Clear();
            resume.Educations.AddRange(req.Educations.Select(e => MapEducation(resume.Id, e)));
        }

        if (req.Experiences is not null)
        {
            resume.Experiences.Clear();
            resume.Experiences.AddRange(req.Experiences.Select(e => MapExperience(resume.Id, e)));
        }

        if (req.Projects is not null)
        {
            resume.Projects.Clear();
            resume.Projects.AddRange(req.Projects.Select(p => MapProject(resume.Id, p)));
        }

        await repo.UpdateAsync(resume);
        return MapToDto(resume);
    }

    public async Task DeleteAsync(Guid resumeId, Guid userId)
    {
        var resume = await repo.GetByIdAsync(resumeId)
            ?? throw new KeyNotFoundException("Resume not found.");
        if (resume.UserId != userId) throw new UnauthorizedAccessException("Not authorized.");
        await repo.DeleteAsync(resumeId);
    }

    public async Task SetDefaultAsync(Guid resumeId, Guid userId)
    {
        var resumes = await repo.GetByUserAsync(userId);
        foreach (var r in resumes)
        {
            r.IsDefault = r.Id == resumeId;
            await repo.UpdateAsync(r);
        }
    }

    public async Task<ResumeDto?> GetByIdAsync(Guid resumeId)
    {
        var resume = await repo.GetByIdAsync(resumeId);
        return resume is null ? null : MapToDto(resume);
    }

    public async Task<IEnumerable<ResumeDto>> GetMyResumesAsync(Guid userId)
    {
        var resumes = await repo.GetByUserAsync(userId);
        return resumes.Select(MapToDto);
    }

    public async Task<byte[]> ExportPdfAsync(Guid resumeId, Guid userId)
    {
        var resume = await repo.GetByIdAsync(resumeId)
            ?? throw new KeyNotFoundException("Resume not found.");
        if (resume.UserId != userId) throw new UnauthorizedAccessException("Not authorized.");

        // Basic HTML-to-text PDF placeholder — replace with QuestPDF or iTextSharp in production
        var sb = new StringBuilder();
        sb.AppendLine($"RESUME - {resume.Personal.FullName}");
        sb.AppendLine($"Email: {resume.Personal.Email} | Phone: {resume.Personal.Phone}");
        sb.AppendLine($"Location: {resume.Personal.Location}");
        if (!string.IsNullOrEmpty(resume.Personal.Summary))
            sb.AppendLine($"\nSUMMARY\n{resume.Personal.Summary}");

        sb.AppendLine("\nEDUCATION");
        foreach (var ed in resume.Educations)
            sb.AppendLine($"  {ed.Degree} in {ed.FieldOfStudy} - {ed.Institution} ({ed.StartDate:yyyy} - {(ed.IsCurrent ? "Present" : ed.EndDate?.ToString("yyyy"))})");

        sb.AppendLine("\nEXPERIENCE");
        foreach (var ex in resume.Experiences)
            sb.AppendLine($"  {ex.JobTitle} at {ex.Company} ({ex.StartDate:yyyy} - {(ex.IsCurrent ? "Present" : ex.EndDate?.ToString("yyyy"))})\n  {ex.Description}");

        sb.AppendLine($"\nSKILLS\n  {string.Join(", ", resume.Skills)}");

        sb.AppendLine("\nPROJECTS");
        foreach (var p in resume.Projects)
            sb.AppendLine($"  {p.Name}: {p.Description}\n  Tech: {string.Join(", ", p.Technologies)}");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<ResumeDto> UploadAsync(Guid userId, string title, IFormFile file)
    {
        // Validate file type — PDF only
        var allowedExtensions = new[] { ".pdf" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            throw new InvalidOperationException("Only PDF files are allowed.");

        if (file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("File size must not exceed 5 MB.");

        // Save file to uploads folder
        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "resumes", userId.ToString());
        Directory.CreateDirectory(uploadsDir);

        var safeFileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, safeFileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var existing = await repo.GetByUserAsync(userId);
        var isFirst = !existing.Any();

        var resume = new Resume
        {
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(title) ? Path.GetFileNameWithoutExtension(file.FileName) : title,
            Template = "Uploaded",
            ResumeType = "Uploaded",
            UploadedFilePath = filePath,
            UploadedFileName = file.FileName,
            IsDefault = isFirst,
            Skills = [],
            Personal = new PersonalInfo { FullName = string.Empty, Email = string.Empty, Phone = string.Empty, Location = string.Empty }
        };
        resume.Personal.ResumeId = resume.Id;

        await repo.AddAsync(resume);
        return MapToDto(resume);
    }

    public async Task<(byte[] Data, string ContentType, string FileName)> DownloadUploadedAsync(Guid resumeId, Guid userId)
    {
        var resume = await repo.GetByIdAsync(resumeId)
            ?? throw new KeyNotFoundException("Resume not found.");
        if (resume.UserId != userId) throw new UnauthorizedAccessException("Not authorized.");
        return await ReadUploadedFile(resume);
    }

    public async Task<(byte[] Data, string ContentType, string FileName)> DownloadUploadedInternalAsync(Guid resumeId)
    {
        var resume = await repo.GetByIdAsync(resumeId)
            ?? throw new KeyNotFoundException("Resume not found.");
        return await ReadUploadedFile(resume);
    }

    private static async Task<(byte[] Data, string ContentType, string FileName)> ReadUploadedFile(Resume resume)
    {
        if (resume.ResumeType != "Uploaded" || string.IsNullOrEmpty(resume.UploadedFilePath))
            throw new InvalidOperationException("This resume has no uploaded file.");
        if (!File.Exists(resume.UploadedFilePath))
            throw new FileNotFoundException("Uploaded file not found on server.");

        var bytes = await File.ReadAllBytesAsync(resume.UploadedFilePath);
        var ext = Path.GetExtension(resume.UploadedFilePath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".pdf"  => "application/pdf",
            ".doc"  => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _       => "application/octet-stream"
        };
        return (bytes, contentType, resume.UploadedFileName ?? Path.GetFileName(resume.UploadedFilePath));
    }

    // Mappers
    private static PersonalInfo MapPersonal(Guid resumeId, PersonalInfoDto d) => new()
    {
        ResumeId = resumeId, FullName = d.FullName, Email = d.Email,
        Phone = d.Phone, Location = d.Location, LinkedInUrl = d.LinkedInUrl,
        GitHubUrl = d.GitHubUrl, Summary = d.Summary
    };

    private static Education MapEducation(Guid resumeId, EducationDto d) => new()
    {
        ResumeId = resumeId, Institution = d.Institution, Degree = d.Degree,
        FieldOfStudy = d.FieldOfStudy, StartDate = d.StartDate, EndDate = d.EndDate,
        IsCurrent = d.IsCurrent, Grade = d.Grade
    };

    private static Experience MapExperience(Guid resumeId, ExperienceDto d) => new()
    {
        ResumeId = resumeId, Company = d.Company, JobTitle = d.JobTitle,
        Location = d.Location, StartDate = d.StartDate, EndDate = d.EndDate,
        IsCurrent = d.IsCurrent, Description = d.Description
    };

    private static Project MapProject(Guid resumeId, ProjectDto d) => new()
    {
        ResumeId = resumeId, Name = d.Name, Description = d.Description,
        Url = d.Url, Technologies = d.Technologies
    };

    private static ResumeDto MapToDto(Resume r) => new(
        r.Id, r.UserId, r.Title, r.Template, r.IsDefault,
        r.ResumeType, r.UploadedFileName,
        new PersonalInfoDto(r.Personal.FullName, r.Personal.Email, r.Personal.Phone,
            r.Personal.Location, r.Personal.LinkedInUrl, r.Personal.GitHubUrl, r.Personal.Summary),
        r.Educations.Select(e => new EducationDto(e.Id, e.Institution, e.Degree, e.FieldOfStudy,
            e.StartDate, e.EndDate, e.IsCurrent, e.Grade)).ToList(),
        r.Experiences.Select(e => new ExperienceDto(e.Id, e.Company, e.JobTitle, e.Location,
            e.StartDate, e.EndDate, e.IsCurrent, e.Description)).ToList(),
        r.Skills,
        r.Projects.Select(p => new ProjectDto(p.Id, p.Name, p.Description, p.Url, p.Technologies)).ToList(),
        r.CreatedAt);
}
