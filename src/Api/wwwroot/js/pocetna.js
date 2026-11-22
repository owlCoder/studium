// US-7.7: povezuje početnu stranu sa GET /pocetna i prikazuje kartice izvođenja.
//
// Sekcije „Predajem” i „Upisan sam” crta renderHomeSections (home-sections.js),
// a svaku karticu u njima komponenta iz offering-card.js.
// Stanja učitavanja, prazne liste i greške detaljno uređuje US-7.11; ovde se samo
// ispisuje kratka poruka u #home-status, da stranica nikad ne ostane bez povratne informacije.

import { fetchHomeOverview } from "./home-api.js";
import { renderHomeSections } from "./home-sections.js";

// Do integracije prijave (ST-002) token sesije unosi se ručno i čuva samo
// za trajanje kartice pregledača (sessionStorage), nikad trajno.
const TOKEN_KEY = "studium.sessionToken";

const tokenForm = document.getElementById("session-form");
const tokenInput = document.getElementById("session-token");
const container = document.getElementById("pocetna");
const status = document.getElementById("home-status");

tokenForm.addEventListener("submit", event => {
    event.preventDefault();
    const token = tokenInput.value.trim();
    if (token.length === 0) {
        return;
    }

    sessionStorage.setItem(TOKEN_KEY, token);
    tokenInput.value = "";
    loadHome();
});

loadHome();

async function loadHome() {
    const token = sessionStorage.getItem(TOKEN_KEY);
    if (!token) {
        container.replaceChildren();
        showStatus("Unesite token sesije da biste videli svoje predmete.");
        return;
    }

    showStatus("Učitavanje predmeta…");

    try {
        const overview = await fetchHomeOverview(token);
        const counts = renderHomeSections(container, overview);
        const total = counts.teaching + counts.enrolled;
        showStatus(total === 0 ? "Nemate nijedan predmet." : "");
    } catch (error) {
        container.replaceChildren();
        if (error.isUnauthorized) {
            sessionStorage.removeItem(TOKEN_KEY);
        }
        showStatus(error.message ?? "Početni pregled trenutno nije dostupan.");
    }
}

function showStatus(message) {
    status.textContent = message;
    status.hidden = message.length === 0;
}
