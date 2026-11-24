process.env.TZ = "Europe/Sarajevo";

import { test } from "node:test";
import assert from "node:assert/strict";
import { formatLocalDateTime } from "../../src/Api/wwwroot/js/date-format.js";

test("datum je u formatu dd.MM.yyyy. HH:mm sa vodećim nulama", () => {
    assert.equal(formatLocalDateTime(new Date(2026, 0, 5, 9, 7)), "05.01.2026. 09:07");
});

test("vreme sa servera u UTC prikazuje se u lokalnoj zoni (zima, UTC+1)", () => {
    assert.equal(formatLocalDateTime("2026-11-22T15:30:00Z"), "22.11.2026. 16:30");
});

test("letnje računanje vremena se poštuje (leto, UTC+2)", () => {
    assert.equal(formatLocalDateTime("2026-07-01T10:00:00Z"), "01.07.2026. 12:00");
});

test("prelaz preko ponoći menja i datum", () => {
    assert.equal(formatLocalDateTime("2026-12-31T23:30:00Z"), "01.01.2027. 00:30");
});

test("neispravan datum se odbija umesto da se prikaže NaN", () => {
    for (const value of ["nije datum", "", null, undefined, Number.NaN]) {
        assert.throws(() => formatLocalDateTime(value), TypeError);
    }
});