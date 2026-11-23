export const HomeState = Object.freeze({
    SIGNED_OUT: "signed-out",
    LOADING: "loading",
    EMPTY: "empty",
    ERROR: "error",
    READY: "ready"
});

export const DEFAULT_MESSAGES = Object.freeze({
    [HomeState.SIGNED_OUT]: "Unesite token sesije da biste videli svoje predmete.",
    [HomeState.LOADING]: "Učitavanje predmeta…",
    [HomeState.EMPTY]: "Nemate nijedan predmet. Kada budete upisani na izvođenje ili angažovani na njemu, pojaviće se ovde.",
    [HomeState.ERROR]: "Početni pregled trenutno nije dostupan."
});

export const RETRY_LABEL = "Pokušaj ponovo";

const STATUS_CLASS = "home-status";

export function renderHomeStatus(status, state, { message, onRetry, doc = globalThis.document } = {}) {
    if (!Object.values(HomeState).includes(state)) {
        throw new TypeError(`Nepoznato stanje početnog pregleda: "${state}".`);
    }

    status.setAttribute("data-state", state);
    status.setAttribute("aria-busy", state === HomeState.LOADING ? "true" : "false");

    if (state === HomeState.READY) {
        status.replaceChildren();
        status.hidden = true;
        return;
    }

    const text = doc.createElement("span");
    text.className = `${STATUS_CLASS}__text`;
    text.textContent = typeof message === "string" && message.trim().length > 0
        ? message.trim()
        : DEFAULT_MESSAGES[state];

    const children = [text];
    if (state === HomeState.ERROR && typeof onRetry === "function") {
        const retry = doc.createElement("button");
        retry.className = `${STATUS_CLASS}__retry`;
        retry.setAttribute("type", "button");
        retry.textContent = RETRY_LABEL;
        retry.addEventListener("click", () => onRetry());
        children.push(retry);
    }

    status.replaceChildren(...children);
    status.hidden = false;
}
