using TaskManagementApi.Models;

namespace TaskManagementApi.DTOs;

public record UpdateTaskRequest(
    string? Title,
    string? Description,
    TaskItemStatus? Status,
    TaskItemPriority? Priority,
    DateTimeOffset? DueDate);
