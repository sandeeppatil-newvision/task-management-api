using Microsoft.EntityFrameworkCore;
using TaskManagementApi.Data;
using TaskManagementApi.DTOs;
using TaskManagementApi.Models;
using TaskManagementApi.Validators;

namespace TaskManagementApi.Endpoints;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/tasks");
        group.MapPost("/", CreateTask);
        group.MapGet("/", GetTasks);
        group.MapGet("/{id:guid}", GetTaskById);
        group.MapPatch("/{id:guid}", UpdateTask);
        group.MapDelete("/{id:guid}", DeleteTask);
    }

    private static async Task<IResult> CreateTask(
        CreateTaskRequest request, TaskDbContext db, CancellationToken ct)
    {
        var errors = TaskValidator.ValidateCreate(request);
        if (errors.Count > 0) return UnprocessableEntity(errors);

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title!.Trim(),
            Description = request.Description,
            Status = request.Status ?? TaskItemStatus.Todo,
            Priority = request.Priority ?? TaskItemPriority.Medium,
            DueDate = request.DueDate,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/tasks/{task.Id}", ToResponse(task));
    }

    private static async Task<IResult> GetTasks(
        TaskDbContext db,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = db.Tasks.AsNoTracking();

        if (status is not null && Enum.TryParse<TaskItemStatus>(status, ignoreCase: true, out var parsed))
            query = query.Where(t => t.Status == parsed);

        var total = await query.CountAsync(ct);
        var taskItems = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Results.Ok(new PagedResult<TaskResponse>(
            taskItems.Select(ToResponse).ToList(), page, pageSize, total));
    }

    private static async Task<IResult> GetTaskById(
        Guid id, TaskDbContext db, CancellationToken ct)
    {
        var task = await db.Tasks.FindAsync([id], ct);
        return task is null ? Results.NotFound() : Results.Ok(ToResponse(task));
    }

    private static async Task<IResult> UpdateTask(
        Guid id, UpdateTaskRequest request, TaskDbContext db, CancellationToken ct)
    {
        var task = await db.Tasks.FindAsync([id], ct);
        if (task is null) return Results.NotFound();

        var errors = TaskValidator.ValidateUpdate(request);
        if (errors.Count > 0) return UnprocessableEntity(errors);

        if (request.Title is not null) task.Title = request.Title.Trim();
        if (request.Description is not null) task.Description = request.Description;
        if (request.Status.HasValue) task.Status = request.Status.Value;
        if (request.Priority.HasValue) task.Priority = request.Priority.Value;
        if (request.DueDate.HasValue) task.DueDate = request.DueDate.Value;
        task.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(ToResponse(task));
    }

    private static async Task<IResult> DeleteTask(
        Guid id, TaskDbContext db, CancellationToken ct)
    {
        var task = await db.Tasks.FindAsync([id], ct);
        if (task is null) return Results.NotFound();

        db.Tasks.Remove(task);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static IResult UnprocessableEntity(IDictionary<string, string[]> errors)
    {
        var pd = new HttpValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "One or more validation errors occurred.",
            Type = "https://tools.ietf.org/html/rfc4918#section-11.2"
        };
        return Results.Problem(pd);
    }

    private static TaskResponse ToResponse(TaskItem t) =>
        new(t.Id, t.Title, t.Description, t.Status, t.Priority, t.DueDate, t.CreatedAt, t.UpdatedAt);
}
