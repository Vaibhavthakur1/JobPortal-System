using JobCatalogService.Models;
using JobCatalogService.Repositories;
using MassTransit;
using Shared.Contracts.Events.Job;
using Shared.Common.Cache;

namespace JobCatalogService.Services;

public class JobService(IJobRepository jobRepo, IPublishEndpoint publisher, ICacheService cache) : IJobService
{
    public async Task<JobDto> CreateJobAsync(Guid recruiterId, CreateJobRequest req)
    {
        var job = new Job
        {
            RecruiterId = recruiterId,
            Title = req.Title,
            Description = req.Description,
            Company = req.Company,
            Location = req.Location,
            JobType = req.JobType,
            Industry = req.Industry,
            SalaryMin = req.SalaryMin,
            SalaryMax = req.SalaryMax,
            ExperienceYears = req.ExperienceYears,
            RequiredSkills = req.RequiredSkills
        };

        await jobRepo.AddAsync(job);
        await publisher.Publish(new JobPostedEvent(job.Id, recruiterId, job.Title, job.Company, job.Location, job.CreatedAt));

        return MapToDto(job);
    }

    public async Task<JobDto> UpdateJobAsync(Guid jobId, Guid recruiterId, UpdateJobRequest req)
    {
        var job = await jobRepo.GetByIdAsync(jobId)
            ?? throw new KeyNotFoundException("Job not found.");

        if (job.RecruiterId != recruiterId)
            throw new UnauthorizedAccessException("Not authorized.");

        if (req.Title is not null) job.Title = req.Title;
        if (req.Description is not null) job.Description = req.Description;
        if (req.Location is not null) job.Location = req.Location;
        if (req.JobType is not null) job.JobType = req.JobType;
        if (req.SalaryMin.HasValue) job.SalaryMin = req.SalaryMin;
        if (req.SalaryMax.HasValue) job.SalaryMax = req.SalaryMax;
        if (req.ExperienceYears.HasValue) job.ExperienceYears = req.ExperienceYears.Value;
        if (req.RequiredSkills is not null) job.RequiredSkills = req.RequiredSkills;

        await jobRepo.UpdateAsync(job);
        await cache.RemoveAsync($"job:{jobId}");

        return MapToDto(job);
    }

    public async Task CloseJobAsync(Guid jobId, Guid recruiterId)
    {
        var job = await jobRepo.GetByIdAsync(jobId) ?? throw new KeyNotFoundException("Job not found.");
        if (job.RecruiterId != recruiterId) throw new UnauthorizedAccessException("Not authorized.");
        job.Status = "Closed";
        job.ClosedAt = DateTime.UtcNow;
        await jobRepo.UpdateAsync(job);
        await cache.RemoveAsync($"job:{jobId}");
    }

    public async Task ArchiveJobAsync(Guid jobId, Guid recruiterId)
    {
        var job = await jobRepo.GetByIdAsync(jobId) ?? throw new KeyNotFoundException("Job not found.");
        if (job.RecruiterId != recruiterId) throw new UnauthorizedAccessException("Not authorized.");
        job.Status = "Archived";
        await jobRepo.UpdateAsync(job);
        await cache.RemoveAsync($"job:{jobId}");
    }

    public async Task<JobDto?> GetJobAsync(Guid jobId)
    {
        var cached = await cache.GetAsync<JobDto>($"job:{jobId}");
        if (cached is not null) return cached;

        var job = await jobRepo.GetByIdAsync(jobId);
        if (job is null) return null;

        var dto = MapToDto(job);
        await cache.SetAsync($"job:{jobId}", dto, TimeSpan.FromMinutes(15));
        return dto;
    }

    public async Task<PagedResult<JobDto>> SearchJobsAsync(JobSearchRequest request)
    {
        var result = await jobRepo.SearchAsync(request);
        return new PagedResult<JobDto>(result.Items.Select(MapToDto).ToList(), result.TotalCount, result.Page, result.PageSize);
    }

    public async Task<IEnumerable<JobDto>> GetRecruiterJobsAsync(Guid recruiterId)
    {
        var jobs = await jobRepo.GetByRecruiterAsync(recruiterId);
        return jobs.Select(MapToDto);
    }

    private static JobDto MapToDto(Job j) => new(
        j.Id, j.RecruiterId, j.Title, j.Description, j.Company,
        j.Location, j.JobType, j.Industry, j.SalaryMin, j.SalaryMax,
        j.ExperienceYears, j.RequiredSkills, j.Status, j.CreatedAt);
}
