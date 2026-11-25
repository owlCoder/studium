import { renderHomeSections } from "./home-sections.js";
import { HomeState, renderHomeStatus } from "./home-status.js";
import { formatLocalDateTime } from "./date-format.js";

export function createHomePage({ container, status, updated, fetchOverview, tokenStore, now = () => new Date(), doc = globalThis.document }) {
    let latestRequest = 0;

    function show(state, options = {}) {
        renderHomeStatus(status, state, { ...options, doc });
        container.setAttribute("aria-busy", state === HomeState.LOADING ? "true" : "false");
        return state;
    }

    function hideUpdated() {
        updated.textContent = "";
        updated.hidden = true;
    }

    function showUpdated() {
        updated.textContent = `Ažurirano: ${formatLocalDateTime(now())}`;
        updated.hidden = false;
    }

    async function load() {
        const token = tokenStore.get();
        if (!token) {
            latestRequest++;
            container.replaceChildren();
            hideUpdated();
            return show(HomeState.SIGNED_OUT);
        }

        const request = ++latestRequest;
        show(HomeState.LOADING);

        let counts;
        try {
            const overview = await fetchOverview(token);
            if (request !== latestRequest) {
                return HomeState.LOADING;
            }
            counts = renderHomeSections(container, overview, doc);
        } catch (error) {
            if (request !== latestRequest) {
                return HomeState.LOADING;
            }

            container.replaceChildren();
            hideUpdated();

            if (error?.isUnauthorized) {
                tokenStore.clear();
                return show(HomeState.SIGNED_OUT, { message: error.message });
            }

            return show(HomeState.ERROR, {
                message: error?.name === "HomeApiError" ? error.message : undefined,
                onRetry: () => load()
            });
        }

        showUpdated();
        return counts.teaching + counts.enrolled === 0
            ? show(HomeState.EMPTY)
            : show(HomeState.READY);
    }

    return { load };
}
