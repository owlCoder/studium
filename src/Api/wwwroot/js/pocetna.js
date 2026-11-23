import { fetchHomeOverview } from "./home-api.js";
import { createHomePage } from "./home-page.js";

const TOKEN_KEY = "studium.sessionToken";

const tokenForm = document.getElementById("session-form");
const tokenInput = document.getElementById("session-token");

const homePage = createHomePage({
    container: document.getElementById("pocetna"),
    status: document.getElementById("home-status"),
    updated: document.getElementById("home-updated"),
    fetchOverview: token => fetchHomeOverview(token),
    tokenStore: {
        get: () => sessionStorage.getItem(TOKEN_KEY),
        clear: () => sessionStorage.removeItem(TOKEN_KEY)
    }
});

tokenForm.addEventListener("submit", event => {
    event.preventDefault();
    const token = tokenInput.value.trim();
    if (token.length === 0) {
        return;
    }

    sessionStorage.setItem(TOKEN_KEY, token);
    tokenInput.value = "";
    homePage.load();
});

homePage.load();
