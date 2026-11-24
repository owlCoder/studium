// US-7.7: klijent za GET /pocetna (US-7.4).
//
// Token sesije ide samo u zaglavlje Authorization, kako zahteva US-7.5.
// ID studenta se nikad ne šalje: server ga uzima iz sesije.

export const HOME_ENDPOINT = "/pocetna";

export class HomeApiError extends Error {
    /**
     * @param {string} message
     * @param {number} status HTTP status odgovora (0 ako odgovora nije bilo)
     */
    constructor(message, status) {
        super(message);
        this.name = "HomeApiError";
        this.status = status;
    }

    get isUnauthorized() {
        return this.status === 401;
    }
}

/**
 * Učitava početni pregled prijavljenog korisnika.
 *
 * @param {string} sessionToken token sesije iz US-7.5
 * @param {typeof fetch} fetchFn zamenjiv u testovima
 * @returns {Promise<{ teaching: object[], enrolled: object[] }>}
 */
export async function fetchHomeOverview(sessionToken, fetchFn = globalThis.fetch) {
    if (typeof sessionToken !== "string" || sessionToken.trim().length === 0) {
        throw new HomeApiError("Niste prijavljeni.", 401);
    }

    let response;
    try {
        response = await fetchFn(HOME_ENDPOINT, {
            method: "GET",
            headers: {
                Accept: "application/json",
                Authorization: `Bearer ${sessionToken.trim()}`
            },
            cache: "no-store"
        });
    } catch {
        throw new HomeApiError("Server nije dostupan.", 0);
    }

    if (response.status === 401) {
        throw new HomeApiError("Sesija ne važi. Prijavite se ponovo.", 401);
    }

    if (!response.ok) {
        throw new HomeApiError("Početni pregled trenutno nije dostupan.", response.status);
    }

    const overview = await response.json();
    return {
        teaching: Array.isArray(overview?.teaching) ? overview.teaching : [],
        enrolled: Array.isArray(overview?.enrolled) ? overview.enrolled : []
    };
}
