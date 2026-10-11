using TaskManagementApi.Models;

namespace TaskManagementApi.DTOs;

public record CreateTaskRequest(
    string? Title,
    string? Description,
    TaskItemStatus? Status,
    TaskItemPriority? Priority,
    DateTimeOffset? DueDate);
