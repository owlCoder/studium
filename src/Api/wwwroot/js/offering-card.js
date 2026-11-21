// US-7.7: komponenta kartice izvođenja za studenta.
//
// Kartica prikazuje naziv predmeta i akademsku godinu jednog izvođenja iz odgovora
// GET /pocetna (polje "enrolled"). Podaci se upisuju isključivo kroz textContent,
// nikad kroz innerHTML, pa naziv predmeta ne može da ubaci HTML ili skriptu u stranicu.
//
// 
//  treba odraditi - US-7.8 oznaku uloge, US-7.9 bedž otvorenih aktivnosti, US-7.10 veze ka sadržaju.

const CARD_CLASS = "offering-card";

/**
 * Proverava podatke jednog izvođenja i priprema tekst koji kartica prikazuje.
 * Ne zavisi od DOM-a, pa se testira bez pregledača.
 *
 * @param {{ offeringId: string, courseName: string, academicYear: string }} offering
 * @returns {{ id: string, title: string, yearLabel: string, ariaLabel: string }}
 */
export function toOfferingCardModel(offering) {
    if (offering === null || typeof offering !== "object") {
        throw new TypeError("Izvođenje mora biti zadato.");
    }

    const id = requireText(offering.offeringId, "offeringId");
    const title = requireText(offering.courseName, "courseName");
    const academicYear = requireText(offering.academicYear, "academicYear");

    return {
        id,
        title,
        yearLabel: `Akademska godina ${academicYear}`,
        ariaLabel: `${title}, akademska godina ${academicYear}`
    };
}

/**
 * Pravi element kartice za jedno izvođenje.
 *
 * @param {object} offering jedan element liste "enrolled" iz GET /pocetna
 * @param {Document} doc dokument u kom se pravi element (u testovima zamenski)
 * @returns {HTMLElement} element <li> spreman za listu kartica
 */
export function createOfferingCard(offering, doc = globalThis.document) {
    const model = toOfferingCardModel(offering);
    const titleId = `offering-${model.id}-title`;

    const item = doc.createElement("li");
    item.className = `${CARD_CLASS}-item`;

    const card = doc.createElement("article");
    card.className = CARD_CLASS;
    card.setAttribute("data-offering-id", model.id);
    card.setAttribute("aria-labelledby", titleId);

    const body = doc.createElement("div");
    body.className = `${CARD_CLASS}__body`;

    const title = doc.createElement("h3");
    title.className = `${CARD_CLASS}__title`;
    title.id = titleId;
    title.textContent = model.title;

    const year = doc.createElement("p");
    year.className = `${CARD_CLASS}__year`;
    year.textContent = model.yearLabel;

    body.append(title, year);
    card.append(body);
    item.append(card);
    return item;
}

/**
 * Prikazuje kartice u listi. Postojeći sadržaj liste se zamenjuje,
 * pa ponovno učitavanje ne pravi duplikate.
 *
 * @param {HTMLElement} list element <ul> za kartice
 * @param {object[]} offerings lista "enrolled" iz GET /pocetna
 * @param {Document} doc
 * @returns {number} broj prikazanih kartica
 */
export function renderOfferingCards(list, offerings, doc = globalThis.document) {
    if (!Array.isArray(offerings)) {
        throw new TypeError("Lista izvođenja mora biti niz.");
    }

    const cards = offerings.map(offering => createOfferingCard(offering, doc));
    list.replaceChildren(...cards);
    return cards.length;
}

function requireText(value, field) {
    if (typeof value !== "string" || value.trim().length === 0) {
        throw new TypeError(`Polje "${field}" je obavezno.`);
    }
    return value.trim();
}
