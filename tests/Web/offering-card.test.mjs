// US-7.7: testovi komponente kartice. Pokretanje: node --test tests/Web/

import { test } from "node:test";
import assert from "node:assert/strict";
import {
  createOfferingCard,
  renderOfferingCards,
  toOfferingCardModel
} from "../../src/Api/wwwroot/js/offering-card.js";
import { FakeElement, fakeDocument } from "./fake-dom.mjs";

const ers = {
  offeringId: "33333333-0000-0000-0000-000000000001",
  courseName: "Elementi razvoja softvera",
  academicYear: "2026/27",
  role: null,
  openActivities: 1,
  links: []
};

const oib = {
  offeringId: "33333333-0000-0000-0000-000000000002",
  courseName: "Osnove informacione bezbednosti",
  academicYear: "2026/27"
};

test("kartica prikazuje naziv predmeta i akademsku godinu", () => {
  const item = createOfferingCard(ers, fakeDocument);
  const card = item.byClass("offering-card");

  assert.equal(item.tagName, "LI");
  assert.equal(card.tagName, "ARTICLE");
  assert.equal(card.getAttribute("data-offering-id"), ers.offeringId);
  assert.equal(card.byClass("offering-card__title").textContent, "Elementi razvoja softvera");
  assert.equal(card.byClass("offering-card__year").textContent, "Akademska godina 2026/27");
});

test("naslov kartice je povezan sa karticom za čitače ekrana", () => {
  const card = createOfferingCard(ers, fakeDocument).byClass("offering-card");
  const title = card.byClass("offering-card__title");

  assert.equal(title.tagName, "H3");
  assert.equal(card.getAttribute("aria-labelledby"), title.id);
  assert.equal(title.id, `offering-${ers.offeringId}-title`);
});

test("naziv sa HTML oznakama se prikazuje kao običan tekst", () => {
  const malicious = { ...ers, courseName: "<img src=x onerror=alert(1)>" };

  const title = createOfferingCard(malicious, fakeDocument).byClass("offering-card__title");

  assert.equal(title.textContent, "<img src=x onerror=alert(1)>");
  assert.equal(title.children.length, 0);
});

test("razmaci oko naziva i godine se uklanjaju", () => {
  const model = toOfferingCardModel({ ...ers, courseName: "  Baze podataka ", academicYear: " 2025/26 " });

  assert.equal(model.title, "Baze podataka");
  assert.equal(model.yearLabel, "Akademska godina 2025/26");
  assert.equal(model.ariaLabel, "Baze podataka, akademska godina 2025/26");
});

for (const field of ["offeringId", "courseName", "academicYear"]) {
  test(`kartica odbija izvođenje bez polja ${field}`, () => {
    for (const bad of [undefined, null, "", "   ", 42]) {
      assert.throws(
        () => toOfferingCardModel({ ...ers, [field]: bad }),
        { name: "TypeError", message: `Polje "${field}" je obavezno.` });
    }
  });
}

test("kartica odbija prazno izvođenje", () => {
  assert.throws(() => toOfferingCardModel(null), TypeError);
  assert.throws(() => toOfferingCardModel("ERS"), TypeError);
});

test("lista sa dva predmeta daje dve kartice istim redom kao odgovor", () => {
  const list = new FakeElement("ul");

  const count = renderOfferingCards(list, [ers, oib], fakeDocument);

  assert.equal(count, 2);
  assert.deepEqual(
    list.children.map(item => item.byClass("offering-card__title").textContent),
    ["Elementi razvoja softvera", "Osnove informacione bezbednosti"]);
});

test("ponovno prikazivanje zamenjuje stare kartice, bez duplikata", () => {
  const list = new FakeElement("ul");
  renderOfferingCards(list, [ers, oib], fakeDocument);

  renderOfferingCards(list, [oib], fakeDocument);

  assert.equal(list.children.length, 1);
  assert.equal(list.children[0].byClass("offering-card").getAttribute("data-offering-id"), oib.offeringId);
});

test("prazna lista ne prikazuje nijednu karticu", () => {
  const list = new FakeElement("ul");
  list.append(new FakeElement("li"));

  const count = renderOfferingCards(list, [], fakeDocument);

  assert.equal(count, 0);
  assert.equal(list.children.length, 0);
});

test("lista koja nije niz se odbija", () => {
  assert.throws(() => renderOfferingCards(new FakeElement("ul"), null, fakeDocument), TypeError);
});
