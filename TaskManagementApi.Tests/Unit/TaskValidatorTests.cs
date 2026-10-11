using FluentAssertions;
using TaskManagementApi.DTOs;
using TaskManagementApi.Validators;

namespace TaskManagementApi.Tests.Unit;

public class TaskValidatorTests
{
    [Fact]
    public void ValidateCreate_ValidRequest_NoErrors()
    {
        var request = new CreateTaskRequest("Valid Task", null, null, null, null);
        TaskValidator.ValidateCreate(request).Should().BeEmpty();
    }

    // AC-03: title > 200 chars → error on title field
    [Fact]
    public void ValidateCreate_TitleTooLong_ReturnsError()
    {
        var request = new CreateTaskRequest(new string('a', 201), null, null, null, null);
        TaskValidator.ValidateCreate(request).Should().ContainKey("title");
    }

    [Fact]
    public void ValidateCreate_TitleExactlyMaxLength_NoErrors()
    {
        var request = new CreateTaskRequest(new string('a', 200), null, null, null, null);
        TaskValidator.ValidateCreate(request).Should().BeEmpty();
    }

    // AC-14: empty string, whitespace-only, null → error on title field
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ValidateCreate_InvalidTitle_ReturnsError(string? title)
    {
        var request = new CreateTaskRequest(title!, null, null, null, null);
        TaskValidator.ValidateCreate(request).Should().ContainKey("title");
    }

    [Fact]
    public void ValidateCreate_DescriptionTooLong_ReturnsError()
    {
        var request = new CreateTaskRequest("Valid", new string('x', 2001), null, null, null);
        TaskValidator.ValidateCreate(request).Should().ContainKey("description");
    }

    [Fact]
    public void ValidateCreate_DescriptionExactlyMaxLength_NoErrors()
    {
        var request = new CreateTaskRequest("Valid", new string('x', 2000), null, null, null);
        TaskValidator.ValidateCreate(request).Should().BeEmpty();
    }

    [Fact]
    public void ValidateUpdate_NullTitle_NoErrors()
    {
        var request = new UpdateTaskRequest(null, null, null, null, null);
        TaskValidator.ValidateUpdate(request).Should().BeEmpty();
    }

    [Fact]
    public void ValidateUpdate_EmptyTitle_ReturnsError()
    {
        var request = new UpdateTaskRequest("", null, null, null, null);
        TaskValidator.ValidateUpdate(request).Should().ContainKey("title");
    }

    [Fact]
    public void ValidateUpdate_ValidTitle_NoErrors()
    {
        var request = new UpdateTaskRequest("Updated Title", null, null, null, null);
        TaskValidator.ValidateUpdate(request).Should().BeEmpty();
    }
}
