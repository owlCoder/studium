using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;
using Domain.Models;
using NUnit.Framework;

namespace Tests.Domain;

public class ContributionOperationsTests
{
    private ContributionOperations operations=null!;

    [SetUp]
    public void Setup()
    {
        operations = new ContributionOperations();
    }

    private Contribution CreateContribution()
    {
        return new Contribution( 1,
                                 1,
                                 10,
                                 1,
                                 "Pocetni opis doprinosa",
                                  new List<Evidence>());
    }

    [Test]
    public void Update_WhenSubmitted_ReturnsFalse()
    {
        var contribution = CreateContribution();

        bool result = operations.UpdateContribution( contribution,
                                                     10,
                                                     "Novi opis",
                                                     new List<Evidence>(), out string error);

        Assert.That(result, Is.False);
        Assert.That(contribution.Status, Is.EqualTo(ContributionStatus.Podnet));
    }

    [Test]
    public void Update_WhenReturned_ReturnsTrue()
    {
        var contribution = CreateContribution();

        contribution.ReturnForSupplement(  "Dopuniti opis", out _);

        bool result = operations.UpdateContribution( contribution,
                                                     10,
                                                     "Dopunjen opis",
                                                     new List<Evidence>(), out string error);

        Assert.That(result, Is.True);
        Assert.That(contribution.CurrentRevision.Description, Is.EqualTo("Dopunjen opis"));
        Assert.That(contribution.Status, Is.EqualTo(ContributionStatus.Podnet));
    }

    [Test]
    public void Update_ByAnotherStudent_ReturnsFalse()
    {
        var contribution = CreateContribution();

        contribution.ReturnForSupplement( "Potrebna dopuna", out _);

        bool result = operations.UpdateContribution(  contribution,
                                                      99,
                                                      "Tudji opis",
                                                      new List<Evidence>(), out string error);

        Assert.That(result, Is.False);
    }

    [Test]
    public void Delete_WhenSubmitted_ReturnsFalse()
    {
        var contribution = CreateContribution();
        var list = new List<Contribution> { contribution };

        bool result = operations.DeleteContribution(  list,
                                                      contribution,
                                                      10,
                                                      out string error);

        Assert.That(result, Is.False);
        Assert.That(list.Count, Is.EqualTo(1));
    }

    [Test]
    public void Delete_WhenReturned_ReturnsTrue()
    {
        var contribution = CreateContribution();
        var list = new List<Contribution> { contribution };

        contribution.ReturnForSupplement( "Potrebna dopuna", out _);

        bool result = operations.DeleteContribution( list,
                                                     contribution,
                                                     10,
                                                     out string error);

        Assert.That(result, Is.True);
        Assert.That(list.Count, Is.EqualTo(0));
    }

    [Test]
    public void Delete_ByAnotherStudent_ReturnsFalse()
    {
        var contribution = CreateContribution();
        var list = new List<Contribution> { contribution };

        contribution.ReturnForSupplement( "Potrebna dopuna", out _);

        bool result = operations.DeleteContribution( list,
                                                     contribution,
                                                     99,
                                                     out string error);

        Assert.That(result, Is.False);
        Assert.That(list.Count, Is.EqualTo(1));
    }

    [Test]
    public void Delete_WhenNotInList_ReturnsFalse()
    {
        var contribution = CreateContribution();
        var list = new List<Contribution>();

        contribution.ReturnForSupplement( "Potrebna dopuna", out _);

        bool result = operations.DeleteContribution( list,
                                                     contribution,
                                                     10,
                                                     out string error);

        Assert.That(result, Is.False);
    }
}
