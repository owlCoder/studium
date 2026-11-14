using System;
using System.Collections.Generic;
using System.Linq;
using Studium.Application;
using Studium.Domain.Models;

namespace Studium.Infrastructure
{
    public class InMemoryReviewRepository : IReviewRepository
    {
        private readonly List<PregledRada> _pregledi = new();

        public void Save(PregledRada pregled)
        {
            var postojeci = GetByRadId(pregled.RadId);
            if (postojeci != null)
            {
                _pregledi.Remove(postojeci);
            }
            _pregledi.Add(pregled);
        }

        public PregledRada? GetByRadId(Guid radId)
        {
            return _pregledi.FirstOrDefault(p => p.RadId == radId);
        }
    }
}