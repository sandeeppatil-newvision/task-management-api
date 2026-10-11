using TaskManagementApi.DTOs;

namespace TaskManagementApi.Validators;

public static class TaskValidator
{
    public static Dictionary<string, string[]> ValidateCreate(CreateTaskRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        var titleErrors = ValidateTitle(request.Title);
        if (titleErrors.Length > 0) errors["title"] = titleErrors;

        if (request.Description?.Length > 2000)
            errors["description"] = ["Description must be 2000 characters or fewer."];

        return errors;
    }

    public static Dictionary<string, string[]> ValidateUpdate(UpdateTaskRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Title is not null)
        {
            var titleErrors = ValidateTitle(request.Title);
            if (titleErrors.Length > 0) errors["title"] = titleErrors;
        }

        if (request.Description?.Length > 2000)
            errors["description"] = ["Description must be 2000 characters or fewer."];

        return errors;
    }

    private static string[] ValidateTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return ["Title is required and cannot be empty or whitespace."];

        if (title.Length > 200)
            return ["Title must be 200 characters or fewer."];

        return [];
    }
}
