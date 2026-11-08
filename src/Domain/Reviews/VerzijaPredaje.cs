using System;

namespace Studium.Domain.Reviews
{
    public class VerzijaPredaje
    {
        public Guid Id { get; private set; }
        public Guid ZadatakId { get; private set; }
        public int BrojVerzije { get; private set; }
        public DateTime VremePredaje { get; private set; }

        public VerzijaPredaje(Guid id, Guid zadatakId, int brojVerzije)
        {
            Id = id;
            ZadatakId = zadatakId;
            BrojVerzije = brojVerzije;
            VremePredaje = DateTime.UtcNow;
        }
    }
}