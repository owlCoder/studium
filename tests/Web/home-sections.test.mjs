// US-7.8: sekcije „Predajem” i „Upisan sam”. Pokretanje: node --test "tests/Web/*.test.mjs"

import { test } from "node:test";
import assert from "node:assert/strict";
import { renderHomeSections } from "../../src/Api/wwwroot/js/home-sections.js";
import { FakeElement, fakeDocument } from "./fake-dom.mjs";

const bp = {
    offeringId: "33333333-0000-0000-0000-000000000003",
    courseName: "Baze podataka",
    academicYear: "2026/27",
    role: "Assistant",
    roleLabel: "Asistent"
};

const ers = {
    offeringId: "33333333-0000-0000-0000-000000000001",
    courseName: "Elementi razvoja softvera",
    academicYear: "2026/27",
    role: null,
    roleLabel: null
};

const oib = {
    offeringId: "33333333-0000-0000-0000-000000000002",
    courseName: "Osnove informacione bezbednosti",
    academicYear: "2026/27",
    role: null,
    roleLabel: null
};

const section = (container, id) =>
    container.children.find(child => child.getAttribute("data-section") === id) ?? null;

const titles = sectionElement =>
    sectionElement.byClass("home-section__list").children
        .map(item => item.byClass("offering-card__title").textContent);

test("sekcija Predajem je odvojena od sekcije Upisan sam", () => {
    const container = new FakeElement("div");

    const counts = renderHomeSections(container, { teaching: [bp], enrolled: [ers, oib] }, fakeDocument);

    const predajem = section(container, "predajem");
    const upisan = section(container, "upisan-sam");

    assert.deepEqual(counts, { teaching: 1, enrolled: 2 });
    assert.equal(container.children.length, 2);
    assert.equal(predajem.byClass("home-section__title").textContent, "Predajem");
    assert.equal(upisan.byClass("home-section__title").textContent, "Upisan sam");
    assert.deepEqual(titles(predajem), ["Baze podataka"]);
    assert.deepEqual(titles(upisan), ["Elementi razvoja softvera", "Osnove informacione bezbednosti"]);
});

test("Predajem je prva sekcija, Upisan sam druga", () => {
    const container = new FakeElement("div");

    renderHomeSections(container, { teaching: [bp], enrolled: [ers] }, fakeDocument);

    assert.deepEqual(
        container.children.map(child => child.getAttribute("data-section")),
        ["predajem", "upisan-sam"]);
});

test("asistent u sekciji Predajem vidi oznaku Asistent, a upisani predmeti nemaju oznaku", () => {
    const container = new FakeElement("div");

    renderHomeSections(container, { teaching: [bp], enrolled: [ers] }, fakeDocument);

    assert.equal(section(container, "predajem").byClass("offering-card__role").textContent, "Asistent");
    assert.equal(section(container, "upisan-sam").byClass("offering-card__role"), null);
});

test("student koji nigde ne predaje ne vidi sekciju Predajem", () => {
    const container = new FakeElement("div");

    renderHomeSections(container, { teaching: [], enrolled: [ers] }, fakeDocument);

    assert.equal(section(container, "predajem"), null);
    assert.notEqual(section(container, "upisan-sam"), null);
});

test("nastavnik koji nije upisan ni na jedan predmet ne vidi sekciju Upisan sam", () => {
    const container = new FakeElement("div");

    renderHomeSections(container, { teaching: [bp], enrolled: [] }, fakeDocument);

    assert.notEqual(section(container, "predajem"), null);
    assert.equal(section(container, "upisan-sam"), null);
});

test("naslov sekcije je povezan sa sekcijom za čitače ekrana", () => {
    const container = new FakeElement("div");

    renderHomeSections(container, { teaching: [bp], enrolled: [] }, fakeDocument);

    const predajem = section(container, "predajem");
    assert.equal(predajem.tagName, "SECTION");
    assert.equal(predajem.getAttribute("aria-labelledby"), predajem.byClass("home-section__title").id);
});

test("ponovno prikazivanje zamenjuje stare sekcije, bez duplikata", () => {
    const container = new FakeElement("div");
    renderHomeSections(container, { teaching: [bp], enrolled: [ers] }, fakeDocument);

    renderHomeSections(container, { teaching: [], enrolled: [oib] }, fakeDocument);

    assert.equal(container.children.length, 1);
    assert.deepEqual(titles(section(container, "upisan-sam")), ["Osnove informacione bezbednosti"]);
});

test("bez ijednog izvođenja nema nijedne sekcije", () => {
    const container = new FakeElement("div");

    const counts = renderHomeSections(container, { teaching: [], enrolled: [] }, fakeDocument);

    assert.deepEqual(counts, { teaching: 0, enrolled: 0 });
    assert.equal(container.children.length, 0);
});

test("neispravan odgovor se odbija", () => {
    const container = new FakeElement("div");

    assert.throws(() => renderHomeSections(container, null, fakeDocument), TypeError);
    assert.throws(() => renderHomeSections(container, { teaching: "BP" }, fakeDocument), TypeError);
});
