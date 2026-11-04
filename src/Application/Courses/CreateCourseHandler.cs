using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.Courses;

namespace Application.Courses;

public sealed class CreateCourseHandler
{
    private readonly ICourseRepository _repository;

    public CreateCourseHandler(ICourseRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> HandleAsync(
        CreateCourseCommand command,
        CancellationToken cancellationToken = default)
    {
        var course = new Course(
            Guid.NewGuid(),
            command.Code,
            command.Name);

        var codeExists = await _repository.ExistsByCodeAsync(
            course.Code,
            cancellationToken);

        if (codeExists)
        {
            throw new InvalidOperationException(
                $"Predmet sa oznakom '{course.Code}' već postoji.");
        }

        await _repository.AddAsync(course, cancellationToken);

        return course.Id;
    }
}
