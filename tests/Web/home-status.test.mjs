import { test } from "node:test";
import assert from "node:assert/strict";
import { fakeDocument, FakeElement } from "./fake-dom.mjs";
import {
    DEFAULT_MESSAGES,
    HomeState,
    RETRY_LABEL,
    renderHomeStatus
} from "../../src/Api/wwwroot/js/home-status.js";

const retryButton = status => status.byClass("home-status__retry");

test("učitavanje: poruka je vidljiva, region je zauzet, nema dugmeta", () => {
    const status = new FakeElement("p");

    renderHomeStatus(status, HomeState.LOADING, { doc: fakeDocument });

    assert.equal(status.hidden, false);
    assert.equal(status.getAttribute("data-state"), "loading");
    assert.equal(status.getAttribute("aria-busy"), "true");
    assert.equal(status.textContent, DEFAULT_MESSAGES.loading);
    assert.equal(retryButton(status), null);
});

test("prazno stanje: poruka da korisnik nema izvođenja, bez dugmeta", () => {
    const status = new FakeElement("p");

    renderHomeStatus(status, HomeState.EMPTY, { doc: fakeDocument });

    assert.equal(status.getAttribute("data-state"), "empty");
    assert.equal(status.getAttribute("aria-busy"), "false");
    assert.match(status.textContent, /^Nemate nijedan predmet\./);
    assert.equal(retryButton(status), null);
});

test("greška: poruka i dugme za ponovni pokušaj koje poziva onRetry", () => {
    const status = new FakeElement("p");
    let retries = 0;

    renderHomeStatus(status, HomeState.ERROR, { message: "Server nije dostupan.", onRetry: () => retries++, doc: fakeDocument });
    const button = retryButton(status);

    assert.equal(status.getAttribute("data-state"), "error");
    assert.equal(status.byClass("home-status__text").textContent, "Server nije dostupan.");
    assert.ok(button, "nema dugmeta za ponovni pokušaj");
    assert.equal(button.tagName, "BUTTON");
    assert.equal(button.getAttribute("type"), "button");
    assert.equal(button.textContent, RETRY_LABEL);

    button.click();
    assert.equal(retries, 1);
});

test("greška bez poruke dobija podrazumevani tekst", () => {
    const status = new FakeElement("p");

    renderHomeStatus(status, HomeState.ERROR, { message: "   ", onRetry: () => {}, doc: fakeDocument });

    assert.equal(status.byClass("home-status__text").textContent, DEFAULT_MESSAGES.error);
});

test("prikazane kartice: poruka se skriva i briše", () => {
    const status = new FakeElement("p");
    renderHomeStatus(status, HomeState.ERROR, { onRetry: () => {}, doc: fakeDocument });

    renderHomeStatus(status, HomeState.READY, { doc: fakeDocument });

    assert.equal(status.hidden, true);
    assert.equal(status.children.length, 0);
    assert.equal(status.getAttribute("data-state"), "ready");
});

test("tri stanja imaju tri različite poruke", () => {
    const messages = new Set([DEFAULT_MESSAGES.loading, DEFAULT_MESSAGES.empty, DEFAULT_MESSAGES.error]);
    assert.equal(messages.size, 3);
});

test("nepoznato stanje se odbija", () => {
    assert.throws(() => renderHomeStatus(new FakeElement("p"), "nesto", { doc: fakeDocument }), TypeError);
});