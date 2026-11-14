namespace Tests.Application;

using System;
using System.Collections.Generic;
using global::Application.Courses;
using NUnit.Framework;

[TestFixture]
public class SelectContentGroupsServiceTests
{
    private SelectContentGroupsService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new SelectContentGroupsService();
    }

    [Test]
    public void AssignGroupsToContent_ValidInput_ReturnsContract()
    {
        var contentId = Guid.NewGuid();
        var groupIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        var result = _service.AssignGroupsToContent(contentId, groupIds);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.ContentId, Is.EqualTo(contentId));
        Assert.That(result.TargetGroupIds.Count, Is.EqualTo(2));
    }

    [Test]
    public void AssignGroupsToContent_EmptyContentId_ThrowsArgumentException()
    {
        var groupIds = new List<Guid> { Guid.NewGuid() };

        Assert.Throws<ArgumentException>(() => _service.AssignGroupsToContent(Guid.Empty, groupIds));
    }

    [Test]
    public void AssignGroupsToContent_EmptyGroupList_ThrowsArgumentException()
    {
        var contentId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => _service.AssignGroupsToContent(contentId, new List<Guid>()));
    }

    [Test]
    public void AssignGroupsToContent_ContainsEmptyGuidInGroups_ThrowsArgumentException()
    {
        var contentId = Guid.NewGuid();
        var groupIds = new List<Guid> { Guid.NewGuid(), Guid.Empty };

        Assert.Throws<ArgumentException>(() => _service.AssignGroupsToContent(contentId, groupIds));
    }
}