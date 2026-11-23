// US-7.7: testovi povezivanja kartice sa GET /pocetna. Pokretanje: node --test "tests/Web/*.test.mjs"

import { test } from "node:test";
import assert from "node:assert/strict";
import { fetchHomeOverview, HOME_ENDPOINT } from "../../src/Api/wwwroot/js/home-api.js";

function fakeFetch(status, body) {
    const calls = [];
    const fetchFn = async (url, options) => {
        calls.push({ url, options });
        return {
            status,
            ok: status >= 200 && status < 300,
            json: async () => body
        };
    };
    return { fetchFn, calls };
}

test("zahtev ide na /pocetna sa tokenom samo u zaglavlju Authorization", async () => {
    const { fetchFn, calls } = fakeFetch(200, { teaching: [], enrolled: [] });

    await fetchHomeOverview("  abc123  ", fetchFn);

    assert.equal(calls.length, 1);
    assert.equal(calls[0].url, HOME_ENDPOINT);
    assert.equal(calls[0].url, "/pocetna");
    assert.equal(calls[0].options.method, "GET");
    assert.equal(calls[0].options.headers.Authorization, "Bearer abc123");
    assert.equal(calls[0].options.cache, "no-store");
    assert.doesNotMatch(calls[0].url, /token|studentId|userId/i);
});

test("vraća listu upisanih izvođenja iz odgovora", async () => {
    const enrolled = [{ offeringId: "1", courseName: "Elementi razvoja softvera", academicYear: "2026/27" }];
    const { fetchFn } = fakeFetch(200, { teaching: [], enrolled });

    const overview = await fetchHomeOverview("abc", fetchFn);

    assert.deepEqual(overview.enrolled, enrolled);
    assert.deepEqual(overview.teaching, []);
});

test("odgovor bez liste daje praznu listu umesto greške", async () => {
    const { fetchFn } = fakeFetch(200, {});

    const overview = await fetchHomeOverview("abc", fetchFn);

    assert.deepEqual(overview.enrolled, []);
    assert.deepEqual(overview.teaching, []);
});

test("bez tokena se zahtev ne šalje", async () => {
    const { fetchFn, calls } = fakeFetch(200, {});

    for (const token of [undefined, null, "", "   "]) {
        await assert.rejects(() => fetchHomeOverview(token, fetchFn), { name: "HomeApiError", status: 401 });
    }
    assert.equal(calls.length, 0);
});

test("401 sa servera znači da sesija ne važi", async () => {
    const { fetchFn } = fakeFetch(401, null);

    await assert.rejects(
        () => fetchHomeOverview("istekao", fetchFn),
        error => error.isUnauthorized === true && error.message === "Sesija ne važi. Prijavite se ponovo.");
});

test("greška servera ne vraća podatke", async () => {
    const { fetchFn } = fakeFetch(500, { enrolled: [{ courseName: "ne sme da se prikaže" }] });

    await assert.rejects(
        () => fetchHomeOverview("abc", fetchFn),
        error => error.status === 500 && error.isUnauthorized === false);
});

test("nedostupan server daje razumljivu grešku", async () => {
    const fetchFn = async () => {
        throw new TypeError("Failed to fetch");
    };

    await assert.rejects(
        () => fetchHomeOverview("abc", fetchFn),
        { name: "HomeApiError", status: 0, message: "Server nije dostupan." });
});
