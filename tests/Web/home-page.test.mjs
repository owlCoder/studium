process.env.TZ = "Europe/Sarajevo";

import { test } from "node:test";
import assert from "node:assert/strict";
import { fakeDocument, FakeElement } from "./fake-dom.mjs";
import { fetchHomeOverview } from "../../src/Api/wwwroot/js/home-api.js";
import { createHomePage } from "../../src/Api/wwwroot/js/home-page.js";
import { DEFAULT_MESSAGES, HomeState } from "../../src/Api/wwwroot/js/home-status.js";

const ONE_OFFERING = {
    teaching: [],
    enrolled: [{ offeringId: "o-1", courseName: "Elementi razvoja softvera", academicYear: "2026/27" }]
};

function response(status, body) {
    return { status, ok: status >= 200 && status < 300, json: async () => body };
}

function scriptedFetch(...answers) {
    const calls = [];
    const fetchFn = async url => {
        calls.push(url);
        const answer = answers[Math.min(calls.length, answers.length) - 1];
        if (answer === "throw") {
            throw new Error("network");
        }
        return typeof answer === "function" ? answer() : answer;
    };
    return { fetchFn, calls };
}

function setup(fetchFn, { token = "tok-1" } = {}) {
    const dom = {
        container: new FakeElement("div"),
        status: new FakeElement("p"),
        updated: new FakeElement("p")
    };
    let stored = token;
    const page = createHomePage({
        ...dom,
        fetchOverview: t => fetchHomeOverview(t, fetchFn),
        tokenStore: { get: () => stored, clear: () => { stored = null; } },
        now: () => new Date("2026-11-22T15:32:00Z"),
        doc: fakeDocument
    });
    return { ...dom, page, token: () => stored };
}

const cards = container => {
    const found = [];
    const walk = el => el.children.forEach(child => {
        if (child.className === "offering-card") found.push(child);
        walk(child);
    });
    walk(container);
    return found;
};

test("bez tokena: traži se prijava, server se ne poziva", async () => {
    const { fetchFn, calls } = scriptedFetch(response(200, ONE_OFFERING));
    const view = setup(fetchFn, { token: null });

    const state = await view.page.load();

    assert.equal(state, HomeState.SIGNED_OUT);
    assert.equal(calls.length, 0);
    assert.equal(view.status.textContent, DEFAULT_MESSAGES[HomeState.SIGNED_OUT]);
});

test("stanje 1, učitavanje: dok odgovor ne stigne vidi se poruka, a ne prazna lista ni greška", async () => {
    let release;
    const pending = new Promise(resolve => { release = resolve; });
    const { fetchFn } = scriptedFetch(() => pending);
    const view = setup(fetchFn);

    const loading = view.page.load();

    assert.equal(view.status.getAttribute("data-state"), HomeState.LOADING);
    assert.equal(view.status.textContent, "Učitavanje predmeta…");
    assert.equal(view.status.hidden, false);
    assert.equal(view.container.getAttribute("aria-busy"), "true");
    assert.equal(view.status.byClass("home-status__retry"), null);

    release(response(200, ONE_OFFERING));
    assert.equal(await loading, HomeState.READY);
    assert.equal(view.container.getAttribute("aria-busy"), "false");
});

test("stanje 2, prazno: korisnik bez izvođenja dobija poruku, bez kartica i bez dugmeta", async () => {
    const { fetchFn } = scriptedFetch(response(200, { teaching: [], enrolled: [] }));
    const view = setup(fetchFn);

    const state = await view.page.load();

    assert.equal(state, HomeState.EMPTY);
    assert.equal(view.status.getAttribute("data-state"), "empty");
    assert.match(view.status.textContent, /^Nemate nijedan predmet\./);
    assert.equal(view.status.byClass("home-status__retry"), null);
    assert.equal(cards(view.container).length, 0);
    assert.equal(view.updated.hidden, false);
    assert.equal(view.updated.textContent, "Ažurirano: 22.11.2026. 16:32");
});

test("stanje 3, greška: server vrati 500, prikazuje se greška sa dugmetom, a ne prazna lista", async () => {
    const { fetchFn } = scriptedFetch(response(500, null));
    const view = setup(fetchFn);

    const state = await view.page.load();

    assert.equal(state, HomeState.ERROR);
    assert.equal(view.status.getAttribute("data-state"), "error");
    assert.equal(view.status.byClass("home-status__text").textContent, "Početni pregled trenutno nije dostupan.");
    assert.ok(view.status.byClass("home-status__retry"));
    assert.doesNotMatch(view.status.textContent, /Nemate nijedan predmet/);
    assert.equal(view.updated.hidden, true);
});

test("greška pa ponovni pokušaj: klik šalje novi zahtev i prikazuje kartice", async () => {
    const { fetchFn, calls } = scriptedFetch("throw", response(200, ONE_OFFERING));
    const view = setup(fetchFn);

    assert.equal(await view.page.load(), HomeState.ERROR);
    assert.equal(view.status.byClass("home-status__text").textContent, "Server nije dostupan.");

    view.status.byClass("home-status__retry").click();
    await new Promise(resolve => setImmediate(resolve));

    assert.equal(calls.length, 2);
    assert.equal(view.status.getAttribute("data-state"), HomeState.READY);
    assert.equal(view.status.hidden, true);
    assert.equal(cards(view.container).length, 1);
});

test("odgovor koji nije JSON je greška sa razumljivom porukom, ne prazna lista", async () => {
    const notJson = { status: 200, ok: true, json: async () => { throw new SyntaxError("Unexpected token <"); } };
    const { fetchFn } = scriptedFetch(notJson);
    const view = setup(fetchFn);

    assert.equal(await view.page.load(), HomeState.ERROR);
    assert.equal(view.status.byClass("home-status__text").textContent, DEFAULT_MESSAGES.error);
    assert.doesNotMatch(view.status.textContent, /Unexpected token/);
    assert.ok(view.status.byClass("home-status__retry"));
});

test("istekla sesija (401): token se briše i traži nova prijava, bez dugmeta za ponovni pokušaj", async () => {
    const { fetchFn } = scriptedFetch(response(401, null));
    const view = setup(fetchFn);

    const state = await view.page.load();

    assert.equal(state, HomeState.SIGNED_OUT);
    assert.equal(view.token(), null);
    assert.equal(view.status.textContent, "Sesija ne važi. Prijavite se ponovo.");
    assert.equal(view.status.byClass("home-status__retry"), null);
});

test("zakasneli odgovor starijeg zahteva ne prepisuje novije stanje", async () => {
    let releaseOld;
    const old = new Promise(resolve => { releaseOld = resolve; });
    const { fetchFn } = scriptedFetch(() => old, response(200, { teaching: [], enrolled: [] }));
    const view = setup(fetchFn);

    const first = view.page.load();
    const second = await view.page.load();
    releaseOld(response(200, ONE_OFFERING));
    await first;

    assert.equal(second, HomeState.EMPTY);
    assert.equal(view.status.getAttribute("data-state"), HomeState.EMPTY);
    assert.equal(cards(view.container).length, 0);
});