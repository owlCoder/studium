// US-7.7: provera početne strane i prikaza za uzak ekran. Pokretanje: node --test "tests/Web/*.test.mjs"


import { test } from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";

const wwwroot = new URL("../../src/Api/wwwroot/", import.meta.url);
const html = readFileSync(new URL("index.html", wwwroot), "utf8");
const css = readFileSync(new URL("css/pocetna.css", wwwroot), "utf8");

// Tijelo pravila za zadati selektor, bez obzira na uvlačenje i razmake.
function ruleBody(source, selector) {
    const escaped = selector.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    const match = source.match(new RegExp(`(?:^|[\\s,}])${escaped}\\s*\\{([^}]*)\\}`));
    return match ? match[1] : null;
}

// Sadržaj media upita (uključujući ugnježdena pravila).
function mediaBlock(source, query) {
    const start = source.indexOf(`@media ${query}`);
    if (start < 0) {
        return null;
    }
    const open = source.indexOf("{", start);
    let depth = 0;
    for (let i = open; i < source.length; i++) {
        if (source[i] === "{") {
            depth++;
        } else if (source[i] === "}" && --depth === 0) {
            return source.slice(open + 1, i);
        }
    }
    return null;
}

const compact = text => text.replace(/\s+/g, " ").trim();

test("stranica ima viewport za mobilne uređaje", () => {
    assert.match(html, /<meta\s+name="viewport"\s+content="width=device-width,\s*initial-scale=1"\s*>/);
});

test("stranica ima kontejner za sekcije i polje za poruke", () => {
    assert.match(html, /<div\s+id="pocetna"[^>]*>\s*<\/div>/);
    assert.match(html, /<p\s+id="home-status"[^>]*role="status"/);
});

test("stranica učitava stilove i skriptu kao modul", () => {
    assert.match(html, /<link\s+rel="stylesheet"\s+href="css\/pocetna\.css"\s*>/);
    assert.match(html, /<script\s+type="module"\s+src="js\/pocetna\.js"\s*><\/script>/);
});

test("stranica sama ne sadrži podatke o predmetima", () => {
    for (const course of ["Elementi razvoja softvera", "Osnove informacione bezbednosti", "2026/27"]) {
        assert.doesNotMatch(html, new RegExp(course));
    }
});

test("lista kartica je mreža koja sama prilagođava broj kolona", () => {
    const list = ruleBody(css, ".home-section__list");

    assert.ok(list, "nema pravila za .home-section__list");
    assert.match(compact(list), /display: grid;/);
    assert.match(compact(list), /grid-template-columns: repeat\(auto-fill, minmax\(min\(100%, 16rem\), 1fr\)\);/);
});

test("na uskom ekranu kartice idu u jednu kolonu", () => {
    const narrow = mediaBlock(css, "(max-width: 30rem)");
    assert.ok(narrow, "nema media upita za uzak ekran");

    const list = ruleBody(narrow, ".home-section__list");
    assert.ok(list, "media upit ne menja listu kartica");
    assert.match(compact(list), /grid-template-columns: 1fr;/);
});

test("na uskom ekranu formular za token zauzima celu širinu", () => {
    const narrow = compact(mediaBlock(css, "(max-width: 30rem)") ?? "");

    assert.match(narrow, /\.session-form, \.session-form input \{ width: 100%; \}/);
});

test("dugačak naziv predmeta se prelama i ne izlazi iz kartice", () => {
    assert.match(compact(ruleBody(css, ".offering-card__title") ?? ""), /overflow-wrap: anywhere;/);
    assert.match(compact(ruleBody(css, ".offering-card-item") ?? ""), /min-width: 0;/);
});

test("svaka klasa kartice i sekcije ima stil", () => {
    const classes = [
        "home-section__title",
        "home-section__list",
        "offering-card-item",
        "offering-card",
        "offering-card__body",
        "offering-card__title",
        "offering-card__year"
    ];

    for (const className of classes) {
        assert.ok(ruleBody(css, `.${className}`), `nema stila za .${className}`);
    }
});
