using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Models;
using NUnit.Framework;

namespace Tests.Domain;

public sealed class EvidenceTests
{
    [Test]
    public void IsLink_WhenLinkIsSet_ReturnsTrue()
    {
        var evidence = new Evidence { Link = "https://github.com/tim/projekat/commit/1" };

        Assert.That(evidence.IsLink, Is.True);
    }

    [Test]
    public void IsLink_WhenOnlyFilePathIsSet_ReturnFalse()
    {
        var evidence = new Evidence { FilePath = "dokazi/screenshot.png" };

        Assert.That(evidence.IsLink, Is.False);

    }
}
