using System;
using Studium.Domain.Models; 

namespace Studium.Application
{
    public class SaveReviewDraftCommand
    {
        public Guid RadId { get; set; }
        public string? Komentar { get; set; }
        public string? AutorId { get; set; }// Autor pregleda se beleži
        public ReviewOutcome? Outcome { get; set; }
    }
}