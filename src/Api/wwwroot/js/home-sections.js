// US-7.8: početni pregled deljen na sekcije „Predajem” i „Upisan sam”.
//
// Odgovor GET /pocetna već ima dve odvojene liste: "teaching", izvođenja na kojima je
// korisnik angažovan, sa oznakom uloge i "enrolled", izvođenja na koja je upisan.
// Svaka lista dobija svoju sekciju sa naslovom; sekcija bez izvođenja se ne prikazuje,
// pa student koji nigde ne predaje ne vidi praznu sekciju „Predajem”.
// Kartice pravi komponenta iz US-7.7 (offering-card.js).

import { renderOfferingCards } from "./offering-card.js";

export const SECTIONS = [
    { key: "teaching", id: "predajem", title: "Predajem" },
    { key: "enrolled", id: "upisan-sam", title: "Upisan sam" }
];

/**
 * Pravi jednu sekciju sa naslovom i listom kartica.
 *
 * @param {{ key: string, id: string, title: string }} section
 * @param {object[]} offerings izvođenja te sekcije iz GET /pocetna
 * @param {Document} doc
 * @returns {HTMLElement} element <section>
 */
export function createHomeSection(section, offerings, doc = globalThis.document) {
    const headingId = `sekcija-${section.id}`;

    const element = doc.createElement("section");
    element.className = "home-section";
    element.setAttribute("data-section", section.id);
    element.setAttribute("aria-labelledby", headingId);

    const heading = doc.createElement("h2");
    heading.className = "home-section__title";
    heading.id = headingId;
    heading.textContent = section.title;

    const list = doc.createElement("ul");
    list.className = "home-section__list";
    renderOfferingCards(list, offerings, doc);

    element.append(heading, list);
    return element;
}

/**
 * Prikazuje sekcije „Predajem” i „Upisan sam” u zadatom kontejneru.
 * Postojeći sadržaj kontejnera se zamenjuje, pa ponovno učitavanje ne pravi duplikate.
 *
 * @param {HTMLElement} container element u koji idu sekcije
 * @param {{ teaching?: object[], enrolled?: object[] }} overview odgovor GET /pocetna
 * @param {Document} doc
 * @returns {{ teaching: number, enrolled: number }} broj kartica po sekciji
 */
export function renderHomeSections(container, overview, doc = globalThis.document) {
    if (overview === null || typeof overview !== "object") {
        throw new TypeError("Početni pregled mora biti zadat.");
    }

    const counts = {};
    const sections = [];

    for (const section of SECTIONS) {
        const offerings = overview[section.key] ?? [];
        if (!Array.isArray(offerings)) {
            throw new TypeError(`Polje "${section.key}" mora biti niz.`);
        }

        counts[section.key] = offerings.length;
        if (offerings.length > 0) {
            sections.push(createHomeSection(section, offerings, doc));
        }
    }

    container.replaceChildren(...sections);
    return counts;
}
