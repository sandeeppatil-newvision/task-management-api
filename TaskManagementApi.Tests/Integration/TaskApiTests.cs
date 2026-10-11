using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using TaskManagementApi.DTOs;
using TaskManagementApi.Models;

namespace TaskManagementApi.Tests.Integration;

public class TaskApiTests : IClassFixture<TaskApiFactory>
{
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public TaskApiTests(TaskApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // AC-01: POST /tasks valid → 201 + task with generated Id, status = Todo
    [Fact]
    public async Task CreateTask_Valid_Returns201WithDefaults()
    {
        var response = await _client.PostAsJsonAsync("/tasks", new { title = "AC-01 Task" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>(JsonOptions);
        task.Should().NotBeNull();
        task!.Id.Should().NotBeEmpty();
        task.Title.Should().Be("AC-01 Task");
        task.Status.Should().Be(TaskItemStatus.Todo);
        task.Priority.Should().Be(TaskItemPriority.Medium);
    }

    // AC-02: POST /tasks missing title → 422
    [Fact]
    public async Task CreateTask_MissingTitle_Returns422()
    {
        var response = await _client.PostAsJsonAsync("/tasks", new { description = "No title" });

        ((int)response.StatusCode).Should().Be(422);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("title");
    }

    // AC-03: POST /tasks title > 200 chars → 422
    [Fact]
    public async Task CreateTask_TitleTooLong_Returns422()
    {
        var response = await _client.PostAsJsonAsync("/tasks", new { title = new string('a', 201) });

        ((int)response.StatusCode).Should().Be(422);
    }

    // AC-04: GET /tasks/{id} existing → 200 + task
    [Fact]
    public async Task GetTask_Existing_Returns200()
    {
        var id = await CreateTaskAndGetId("AC-04 Task");

        var response = await _client.GetAsync($"/tasks/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>(JsonOptions);
        task.Should().NotBeNull();
        task!.Id.Should().Be(id);
        task.Title.Should().Be("AC-04 Task");
    }

    // AC-05: GET /tasks/{id} missing → 404
    [Fact]
    public async Task GetTask_Missing_Returns404()
    {
        var response = await _client.GetAsync($"/tasks/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // AC-06: GET /tasks default → 200 + paginated list
    [Fact]
    public async Task GetTasks_Default_Returns200PagedResult()
    {
        var response = await _client.GetAsync("/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TaskResponse>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.Items.Should().NotBeNull();
    }

    // AC-07: GET /tasks?status=InProgress → filtered list
    [Fact]
    public async Task GetTasks_StatusFilter_ReturnsOnlyMatchingTasks()
    {
        await CreateTaskAndGetId("AC-07 Todo Task");
        await CreateTaskWithStatus("AC-07 InProgress Task", "InProgress");

        var response = await _client.GetAsync("/tasks?status=InProgress");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TaskResponse>>(JsonOptions);
        result!.Items.Should().NotBeEmpty()
            .And.AllSatisfy(t => t.Status.Should().Be(TaskItemStatus.InProgress));
    }

    // AC-08: PATCH /tasks/{id} valid → 200 + only changed fields differ
    [Fact]
    public async Task UpdateTask_Valid_Returns200WithChangedFields()
    {
        var id = await CreateTaskAndGetId("AC-08 Original");
        var beforeUpdate = DateTimeOffset.UtcNow.AddSeconds(-1);

        var response = await _client.PatchAsJsonAsync($"/tasks/{id}",
            new { title = "AC-08 Updated", status = "InProgress" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>(JsonOptions);
        task!.Title.Should().Be("AC-08 Updated");
        task.Status.Should().Be(TaskItemStatus.InProgress);
        task.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
    }

    // AC-09: PATCH /tasks/{id} Done → Todo → 200 (reversible)
    [Fact]
    public async Task UpdateTask_DoneToTodo_Returns200()
    {
        var id = await CreateTaskWithStatus("AC-09 Done Task", "Done");

        var response = await _client.PatchAsJsonAsync($"/tasks/{id}", new { status = "Todo" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>(JsonOptions);
        task!.Status.Should().Be(TaskItemStatus.Todo);
    }

    // AC-10: PATCH /tasks/{id} missing → 404
    [Fact]
    public async Task UpdateTask_Missing_Returns404()
    {
        var response = await _client.PatchAsJsonAsync($"/tasks/{Guid.NewGuid()}",
            new { title = "Anything" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // AC-11: DELETE /tasks/{id} existing → 204
    [Fact]
    public async Task DeleteTask_Existing_Returns204()
    {
        var id = await CreateTaskAndGetId("AC-11 Delete Me");

        var response = await _client.DeleteAsync($"/tasks/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // AC-12: DELETE /tasks/{id} missing → 404
    [Fact]
    public async Task DeleteTask_Missing_Returns404()
    {
        var response = await _client.DeleteAsync($"/tasks/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // AC-13: Tasks persist across requests (in-process persistence validation)
    [Fact]
    public async Task Tasks_PersistAcrossSubsequentRequests()
    {
        var id = await CreateTaskAndGetId("AC-13 Persistence Task");

        var response = await _client.GetAsync($"/tasks/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>(JsonOptions);
        task!.Id.Should().Be(id);
        task.Title.Should().Be("AC-13 Persistence Task");
    }

    private async Task<Guid> CreateTaskAndGetId(string title)
    {
        var response = await _client.PostAsJsonAsync("/tasks", new { title });
        response.EnsureSuccessStatusCode();
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>(JsonOptions);
        return task!.Id;
    }

    private async Task<Guid> CreateTaskWithStatus(string title, string status)
    {
        var response = await _client.PostAsJsonAsync("/tasks", new { title, status });
        response.EnsureSuccessStatusCode();
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>(JsonOptions);
        return task!.Id;
    }
}
