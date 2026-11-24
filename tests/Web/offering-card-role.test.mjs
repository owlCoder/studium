// US-7.8: oznaka uloge na kartici izvođenja. Pokretanje: node --test "tests/Web/*.test.mjs"

import { test } from "node:test";
import assert from "node:assert/strict";
import { createOfferingCard, toOfferingCardModel } from "../../src/Api/wwwroot/js/offering-card.js";
import { fakeDocument } from "./fake-dom.mjs";

const bpAsistent = {
    offeringId: "33333333-0000-0000-0000-000000000003",
    courseName: "Baze podataka",
    academicYear: "2026/27",
    role: "Assistant",
    roleLabel: "Asistent",
    openActivities: null,
    links: []
};

const ersUpisan = {
    offeringId: "33333333-0000-0000-0000-000000000001",
    courseName: "Elementi razvoja softvera",
    academicYear: "2026/27",
    role: null,
    roleLabel: null,
    openActivities: 1,
    links: []
};

test("asistent vidi oznaku Asistent na kartici", () => {
    const card = createOfferingCard(bpAsistent, fakeDocument).byClass("offering-card");
    const role = card.byClass("offering-card__role");

    assert.notEqual(role, null);
    assert.equal(role.textContent, "Asistent");
    assert.equal(card.getAttribute("data-role"), "Asistent");
});

test("svaka uloga se prikazuje onako kako je stigla iz /pocetna", () => {
    for (const label of ["Odgovorni nastavnik", "Nastavnik", "Asistent"]) {
        const card = createOfferingCard({ ...bpAsistent, roleLabel: label }, fakeDocument);

        assert.equal(card.byClass("offering-card__role").textContent, label);
    }
});

test("kartica izvođenja na koje je korisnik upisan nema oznaku uloge", () => {
    const card = createOfferingCard(ersUpisan, fakeDocument).byClass("offering-card");

    assert.equal(card.byClass("offering-card__role"), null);
    assert.equal(card.getAttribute("data-role"), null);
});

test("prazna ili nedostajuća oznaka se ne prikazuje", () => {
    for (const bad of [undefined, null, "", "   ", 42]) {
        assert.equal(toOfferingCardModel({ ...bpAsistent, roleLabel: bad }).roleLabel, null);
    }
});

test("oznaka uloge sa HTML oznakama se prikazuje kao običan tekst", () => {
    const card = createOfferingCard({ ...bpAsistent, roleLabel: "<b>Asistent</b>" }, fakeDocument);
    const role = card.byClass("offering-card__role");

    assert.equal(role.textContent, "<b>Asistent</b>");
    assert.equal(role.children.length, 0);
});
