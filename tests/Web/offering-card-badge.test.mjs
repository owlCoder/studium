// US-7.9: testovi bedža otvorenih aktivnosti. Pokretanje: node --test tests/Web/

import { test } from "node:test";
import assert from "node:assert/strict";
import {
    createOfferingCard,
    openActivitiesLabel,
    toOfferingCardModel
} from "../../src/Api/wwwroot/js/offering-card.js";
import { fakeDocument } from "./fake-dom.mjs";

const ers = {
    offeringId: "33333333-0000-0000-0000-000000000001",
    courseName: "Elementi razvoja softvera",
    academicYear: "2026/27",
    role: null,
    openActivities: 3,
    links: []
};

test("kartica prikazuje bedž sa brojem otvorenih obaveza", () => {
    const card = createOfferingCard(ers, fakeDocument).byClass("offering-card");
    const badge = card.byClass("offering-card__badge");

    assert.notEqual(badge, null);
    assert.equal(badge.tagName, "SPAN");
    assert.equal(badge.textContent, "3");
    assert.equal(badge.getAttribute("aria-label"), "3 otvorene obaveze");
    assert.equal(badge.getAttribute("title"), "3 otvorene obaveze");
    assert.equal(card.getAttribute("data-open-activities"), "3");
});

test("bedž se ne prikazuje kada je broj 0", () => {
    const card = createOfferingCard({ ...ers, openActivities: 0 }, fakeDocument).byClass("offering-card");

    assert.equal(card.byClass("offering-card__badge"), null);
    assert.equal(card.getAttribute("data-open-activities"), null);
});

test("bedž se ne prikazuje kada broja nema (izvođenje koje predajem)", () => {
    for (const value of [null, undefined]) {
        const card = createOfferingCard({ ...ers, openActivities: value }, fakeDocument).byClass("offering-card");
        assert.equal(card.byClass("offering-card__badge"), null);
    }
});

test("neispravan broj ne pravi bedž", () => {
    for (const value of [-1, 1.5, "2", NaN]) {
        assert.equal(toOfferingCardModel({ ...ers, openActivities: value }).openActivities, null);
    }
});

test("tekst bedža ima ispravan oblik reči", () => {
    assert.equal(openActivitiesLabel(1), "1 otvorena obaveza");
    assert.equal(openActivitiesLabel(2), "2 otvorene obaveze");
    assert.equal(openActivitiesLabel(4), "4 otvorene obaveze");
    assert.equal(openActivitiesLabel(5), "5 otvorenih obaveza");
    assert.equal(openActivitiesLabel(11), "11 otvorenih obaveza");
    assert.equal(openActivitiesLabel(12), "12 otvorenih obaveza");
    assert.equal(openActivitiesLabel(21), "21 otvorena obaveza");
    assert.equal(openActivitiesLabel(22), "22 otvorene obaveze");
});
