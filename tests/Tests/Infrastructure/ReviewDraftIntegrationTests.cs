using System;
using Xunit;
using Studium.Application;
using Studium.Domain.Models;
using Studium.Infrastructure;

namespace Studium.Tests.Infrastructure
{
    public class ReviewDraftIntegrationTests
    {
        [Fact]
        public void FullFlow_SaveDraft_UpdateByOwner_ReadUpdatedFromRepository()
        {
            // Arrange: pravo in-memory skladište i pravi handler
            var repository = new InMemoryReviewRepository();
            var handler = new SaveReviewDraftHandler(repository);

            var radId = Guid.NewGuid();
            var ownerAutorId = "nastavnik-1";
            var strangerAutorId = "nastavnik-2";

            // Act 1: Vlasnik čuva početni nacrt
            var initialCommand = new SaveReviewDraftCommand
            {
                RadId = radId,
                Komentar = "Početni komentar",
                AutorId = ownerAutorId,
                Outcome = ReviewOutcome.PotrebnaDorada
            };
            var created = handler.Handle(initialCommand);
            Assert.True(created);

            // Act & Assert 2: Neovlašćeni nastavnik ne može da izmeni tuđi nacrt
            var unauthorizedCommand = new SaveReviewDraftCommand
            {
                RadId = radId,
                Komentar = "Hakovani komentar",
                AutorId = strangerAutorId,
                Outcome = ReviewOutcome.Prihvacen
            };
            var unauthorizedResult = handler.Handle(unauthorizedCommand);
            Assert.False(unauthorizedResult);

            // Act 3: Vlasnik nacrta vrši izmenu
            var updateCommand = new SaveReviewDraftCommand
            {
                RadId = radId,
                Komentar = "Ispravljen komentar",
                AutorId = ownerAutorId,
                Outcome = ReviewOutcome.Prihvacen
            };
            var updated = handler.Handle(updateCommand);
            Assert.True(updated);

            // Assert 3: Čitanje nazad iz repozitorijuma potvrđuje da su izmene sačuvane i da neovlašćena izmena nije prošla
            var savedDraft = repository.GetByRadId(radId);
            Assert.NotNull(savedDraft);
            Assert.Equal("Ispravljen komentar", savedDraft!.Komentar);
            Assert.Equal(ReviewOutcome.Prihvacen, savedDraft.Outcome);
            Assert.Equal(ownerAutorId, savedDraft.AutorId);
        }
    }
}