using System;
using NUnit.Framework;
using Studium.Domain.Courses;

namespace Studium.Tests.Domain
{
    [TestFixture]
    public class AngazovanjeNastavnikaTests
    {
        private static readonly DateTime Pocetak = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

        private static AngazovanjeNastavnika Novo(
            Guid izvodjenje, UlogaNaPredmetu uloga = UlogaNaPredmetu.OdgovorniNastavnik)
            => AngazovanjeNastavnika.Kreiraj(izvodjenje, Guid.NewGuid(), uloga, Pocetak);

        // --- Kreiranje ---

        [Test]
        public void Kreiraj_ValidniPodaci_VracaAktivnoAngazovanje()
        {
            var izvodjenje = Guid.NewGuid();
            var korisnik = Guid.NewGuid();

            var a = AngazovanjeNastavnika.Kreiraj(izvodjenje, korisnik, UlogaNaPredmetu.Asistent, Pocetak);

            Assert.That(a.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(a.IzvodjenjeId, Is.EqualTo(izvodjenje));
            Assert.That(a.KorisnikId, Is.EqualTo(korisnik));
            Assert.That(a.UlogaNaPredmetu, Is.EqualTo(UlogaNaPredmetu.Asistent));
            Assert.That(a.Aktivno, Is.True);
        }

        [Test]
        public void Kreiraj_PrazanIzvodjenjeId_Odbija()
        {
            Assert.Throws<ArgumentException>(() =>
                AngazovanjeNastavnika.Kreiraj(Guid.Empty, Guid.NewGuid(), UlogaNaPredmetu.Nastavnik, Pocetak));
        }

        [Test]
        public void Kreiraj_PrazanKorisnikId_Odbija()
        {
            Assert.Throws<ArgumentException>(() =>
                AngazovanjeNastavnika.Kreiraj(Guid.NewGuid(), Guid.Empty, UlogaNaPredmetu.Nastavnik, Pocetak));
        }

        [Test]
        public void Kreiraj_NepoznataUloga_Odbija()
        {
            Assert.Throws<ArgumentException>(() =>
                AngazovanjeNastavnika.Kreiraj(Guid.NewGuid(), Guid.NewGuid(), (UlogaNaPredmetu)99, Pocetak));
        }

        [Test]
        public void Kreiraj_PodrazumevanoVreme_Odbija()
        {
            Assert.Throws<ArgumentException>(() =>
                AngazovanjeNastavnika.Kreiraj(Guid.NewGuid(), Guid.NewGuid(), UlogaNaPredmetu.Nastavnik, default));
        }

        // --- Prekid angažovanja ---

        [Test]
        public void Prekini_Aktivno_PostavljaKrajIOstavljaIdentitet()
        {
            var a = Novo(Guid.NewGuid(), UlogaNaPredmetu.Asistent);
            var id = a.Id;
            var korisnik = a.KorisnikId;

            a.Prekini(Pocetak.AddDays(10));

            Assert.That(a.Aktivno, Is.False);
            Assert.That(a.KrajUtc, Is.EqualTo(Pocetak.AddDays(10)));
            Assert.That(a.Id, Is.EqualTo(id));
            Assert.That(a.KorisnikId, Is.EqualTo(korisnik));
        }

        [Test]
        public void Prekini_VecPrekinuto_Odbija()
        {
            var a = Novo(Guid.NewGuid(), UlogaNaPredmetu.Asistent);
            a.Prekini(Pocetak.AddDays(1));

            Assert.Throws<InvalidOperationException>(() => a.Prekini(Pocetak.AddDays(2)));
        }

        [Test]
        public void Prekini_KrajPrePocetka_OdbijaIOstavljaAktivnim()
        {
            var a = Novo(Guid.NewGuid(), UlogaNaPredmetu.Asistent);

            Assert.Throws<ArgumentOutOfRangeException>(() => a.Prekini(Pocetak.AddDays(-1)));
            Assert.That(a.Aktivno, Is.True);
        }

        // --- Pravilo dodele ---

        [Test]
        public void ProveriDodjelu_KorisnikVecAktivanNaIzvodjenju_Odbija()
        {
            var izvodjenje = Guid.NewGuid();
            var postojece = Novo(izvodjenje, UlogaNaPredmetu.Nastavnik);

            Assert.Throws<InvalidOperationException>(() =>
                OdgovorniNastavnikPravila.ProveriDodjelu(new[] { postojece }, izvodjenje, postojece.KorisnikId));
        }

        [Test]
        public void ProveriDodjelu_IstiKorisnikNaDrugomIzvodjenju_Dozvoljava()
        {
            var postojece = Novo(Guid.NewGuid(), UlogaNaPredmetu.Nastavnik);

            Assert.DoesNotThrow(() =>
                OdgovorniNastavnikPravila.ProveriDodjelu(new[] { postojece }, Guid.NewGuid(), postojece.KorisnikId));
        }

        [Test]
        public void ProveriDodjelu_PrethodnoAngazovanjePrekinuto_Dozvoljava()
        {
            var izvodjenje = Guid.NewGuid();
            var staro = Novo(izvodjenje, UlogaNaPredmetu.Nastavnik);
            staro.Prekini(Pocetak.AddDays(5));

            Assert.DoesNotThrow(() =>
                OdgovorniNastavnikPravila.ProveriDodjelu(new[] { staro }, izvodjenje, staro.KorisnikId));
        }

        // --- Pravilo prekida: bar jedan odgovorni ---

        [Test]
        public void ProveriPrekid_JedinOdgovorniNaAktivnomIzvodjenju_Odbija()
        {
            var izvodjenje = Guid.NewGuid();
            var odgovorni = Novo(izvodjenje);
            var asistent = Novo(izvodjenje, UlogaNaPredmetu.Asistent);

            Assert.Throws<InvalidOperationException>(() =>
                OdgovorniNastavnikPravila.ProveriPrekid(new[] { odgovorni, asistent }, odgovorni, true));
        }

        [Test]
        public void ProveriPrekid_PostojiDrugiAktivniOdgovorni_Dozvoljava()
        {
            var izvodjenje = Guid.NewGuid();
            var prvi = Novo(izvodjenje);
            var drugi = Novo(izvodjenje);

            Assert.DoesNotThrow(() =>
                OdgovorniNastavnikPravila.ProveriPrekid(new[] { prvi, drugi }, prvi, true));
        }

        [Test]
        public void ProveriPrekid_DrugiOdgovorniVecPrekinut_Odbija()
        {
            var izvodjenje = Guid.NewGuid();
            var prvi = Novo(izvodjenje);
            var drugi = Novo(izvodjenje);
            drugi.Prekini(Pocetak.AddDays(1));

            Assert.Throws<InvalidOperationException>(() =>
                OdgovorniNastavnikPravila.ProveriPrekid(new[] { prvi, drugi }, prvi, true));
        }

        [Test]
        public void ProveriPrekid_OdgovorniNaDrugomIzvodjenju_NeBrojiSe()
        {
            var prvi = Novo(Guid.NewGuid());
            var tudji = Novo(Guid.NewGuid());

            Assert.Throws<InvalidOperationException>(() =>
                OdgovorniNastavnikPravila.ProveriPrekid(new[] { prvi, tudji }, prvi, true));
        }

        [Test]
        public void ProveriPrekid_Asistent_UvekDozvoljava()
        {
            var izvodjenje = Guid.NewGuid();
            var odgovorni = Novo(izvodjenje);
            var asistent = Novo(izvodjenje, UlogaNaPredmetu.Asistent);

            Assert.DoesNotThrow(() =>
                OdgovorniNastavnikPravila.ProveriPrekid(new[] { odgovorni, asistent }, asistent, true));
        }

        [Test]
        public void ProveriPrekid_IzvodjenjeNijeAktivno_DozvoljavaPrekidJedinogOdgovornog()
        {
            var odgovorni = Novo(Guid.NewGuid());

            Assert.DoesNotThrow(() =>
                OdgovorniNastavnikPravila.ProveriPrekid(new[] { odgovorni }, odgovorni, false));
        }

        [Test]
        public void ProveriPrekid_VecPrekinutoAngazovanje_Odbija()
        {
            var izvodjenje = Guid.NewGuid();
            var a = Novo(izvodjenje, UlogaNaPredmetu.Asistent);
            a.Prekini(Pocetak.AddDays(1));

            Assert.Throws<InvalidOperationException>(() =>
                OdgovorniNastavnikPravila.ProveriPrekid(new[] { a }, a, true));
        }
    }
}
