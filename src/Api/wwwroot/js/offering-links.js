import { formatLocalDateTime } from "./date-format.js";
import { HomeApiError } from "./home-api.js";

export const PAGE_LINK_LABELS = {
    izvodjenje: "Otvori predmet",
    announcements: "Obaveštenja",
    materials: "Materijali",
    assignments: "Zadaci"
};

const TASK_LABELS = {
    assignment: "Zadatak"
};


export function isSafeRelativeHref(href) {
    return typeof href === "string" && /^\/(?![\/\\])[^\s\\]*$/.test(href);
}


export function toLinkModels(offering) {
    if (offering === null || typeof offering !== "object") {
        throw new TypeError("Izvođenje mora biti zadato.");
    }

    const seen = new Set();

    const pageLinks = [];
    for (const link of asArray(offering.links)) {
        const label = PAGE_LINK_LABELS[link?.rel];
        if (label === undefined || !isSafeRelativeHref(link.href) || seen.has(link.href)) {
            continue; 
        }
        seen.add(link.href);
        pageLinks.push({ rel: link.rel, label, href: link.href });
    }

    const taskLinks = [];
    for (const task of asArray(offering.openTasks)) {
        if (!isSafeRelativeHref(task?.href) || seen.has(task.href)) {
            continue;
        }
        seen.add(task.href);

        const name = TASK_LABELS[task.activityType] ?? "Obaveza";
        const deadline = deadlineLabel(task.deadline);
        const label = deadline === null ? name : `${name}, rok ${deadline}`;
        taskLinks.push({ label, href: task.href, ariaLabel: `Otvori: ${label}` });
    }

    return { pageLinks, taskLinks };
}


export function createOfferingLinks(offering, doc = globalThis.document) {
    const { pageLinks, taskLinks } = toLinkModels(offering);
    if (pageLinks.length === 0 && taskLinks.length === 0) {
        return null;
    }

    const nav = doc.createElement("nav");
    nav.className = "offering-card__links";
    nav.setAttribute("aria-label", `Veze: ${offering.courseName ?? "predmet"}`);

    if (pageLinks.length > 0) {
        const list = doc.createElement("ul");
        list.className = "offering-card__link-list";
        list.append(...pageLinks.map(link => createLinkItem(doc, link.label, link.href, link.label)));
        nav.append(list);
    }

    if (taskLinks.length > 0) {
        const heading = doc.createElement("p");
        heading.className = "offering-card__tasks-title";
        heading.textContent = "Otvoreni zadaci";

        const list = doc.createElement("ul");
        list.className = "offering-card__task-list";
        list.append(...taskLinks.map(link => createLinkItem(doc, link.label, link.href, link.ariaLabel)));

        nav.append(heading, list);
    }

    return nav;
}


export async function openContentLink(href, sessionToken, fetchFn = globalThis.fetch) {
    if (!isSafeRelativeHref(href)) {
        throw new HomeApiError("Veza nije ispravna.", 400);
    }
    if (typeof sessionToken !== "string" || sessionToken.trim().length === 0) {
        throw new HomeApiError("Niste prijavljeni.", 401);
    }

    let response;
    try {
        response = await fetchFn(href, {
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
    if (response.status === 403 || response.status === 404) {
        throw new HomeApiError("Ovaj sadržaj nije dostupan ili nemate pristup.", response.status);
    }
    if (!response.ok) {
        throw new HomeApiError("Sadržaj trenutno nije dostupan.", response.status);
    }

    return response.json();
}

export function attachContentLinks({ container, output, getToken, onUnauthorized = () => {}, fetchFn = globalThis.fetch }) {
    function show(message) {
        output.textContent = message;
        output.hidden = message.length === 0;
    }

    async function open(href) {
        try {
            const content = await openContentLink(href, getToken(), fetchFn);
            show(describeOpenedContent(content));
        } catch (error) {
            if (error?.isUnauthorized) {
                onUnauthorized();
            }
            show(error?.message ?? "Sadržaj trenutno nije dostupan.");
        }
    }

    container.addEventListener("click", event => {
        const link = event.target?.closest?.("a[data-content-link]");
        if (!link) {
            return undefined;
        }
        event.preventDefault();
        return open(link.getAttribute("href"));
    });

    return { open };
}


export function describeOpenedContent(content) {
    if (content?.activityId) {
        const deadline = deadlineLabel(content.deadline);
        return deadline === null ? "Otvoren zadatak." : `Otvoren zadatak, rok ${deadline}.`;
    }
        if (content?.area && content?.courseName) {
        const label = PAGE_LINK_LABELS[content.area] ?? content.area;
        return `Otvoreno: ${label}, ${content.courseName} (${content.academicYear}).`;
    }

    if (content?.courseName) {
        return `Otvoren predmet: ${content.courseName} (${content.academicYear}).`;
    }
    return "Sadržaj je otvoren.";
}

function createLinkItem(doc, text, href, ariaLabel) {
    const item = doc.createElement("li");
    const anchor = doc.createElement("a");
    anchor.className = "offering-card__link";
    anchor.setAttribute("href", href);
    anchor.setAttribute("data-content-link", "true");
    anchor.setAttribute("aria-label", ariaLabel);
    anchor.textContent = text;
    item.append(anchor);
    return item;
}

function deadlineLabel(value) {
    try {
        return formatLocalDateTime(value);
    } catch {
        return null;
    }
}

function asArray(value) {
    return Array.isArray(value) ? value : [];
}
