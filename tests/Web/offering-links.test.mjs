// US-7.10: testovi veza ka sadržaju i otvorenim obavezama. Pokretanje: node --test "tests/Web/*.test.mjs"

import { test } from "node:test";
import assert from "node:assert/strict";
import {
  attachContentLinks,
  createOfferingLinks,
  describeOpenedContent,
  isSafeRelativeHref,
  openContentLink,
  toLinkModels
} from "../../src/Api/wwwroot/js/offering-links.js";
import { createOfferingCard } from "../../src/Api/wwwroot/js/offering-card.js";
import { fakeDocument } from "./fake-dom.mjs";

const ERS = "33333333-0000-0000-0000-000000000001";
const TASK = "66666666-0000-0000-0000-000000000001";

const ers = {
  offeringId: ERS,
  courseName: "Elementi razvoja softvera",
  academicYear: "2026/27",
  role: null,
  openActivities: 1,
  links: [
    { rel: "izvodjenje", href: `/offerings/${ERS}` },
    { rel: "assignments", href: `/offerings/${ERS}/assignments` }
  ],
  openTasks: [
    {
      activityId: TASK,
      activityType: "assignment",
      deadline: "2026-12-01T22:59:00Z",
      href: `/offerings/${ERS}/assignments/${TASK}`
    }
  ]
};

function anchors(element) {
  const result = [];
  const walk = node => {
    if (node.tagName === "A") {
      result.push(node);
    }
    node.children.forEach(walk);
  };
  walk(element);
  return result;
}

test("kartica ima vezu ka stranici izvođenja i ka svakom otvorenom zadatku", () => {
  const card = createOfferingCard(ers, fakeDocument);
  const hrefs = anchors(card).map(a => a.getAttribute("href"));

  assert.deepEqual(hrefs, [
    `/offerings/${ERS}`,
    `/offerings/${ERS}/assignments`,
    `/offerings/${ERS}/assignments/${TASK}`
  ]);
  assert.equal(anchors(card)[0].textContent, "Otvori predmet");
  assert.ok(anchors(card).every(a => a.getAttribute("data-content-link") === "true"));
});

test("veza ka zadatku nosi rok u formatu dd.MM.yyyy. HH:mm", () => {
  const { taskLinks } = toLinkModels(ers);
  const expected = new Date("2026-12-01T22:59:00Z");
  const pad = n => String(n).padStart(2, "0");
  const local = `${pad(expected.getDate())}.${pad(expected.getMonth() + 1)}.${expected.getFullYear()}. ` +
    `${pad(expected.getHours())}:${pad(expected.getMinutes())}`;

  assert.equal(taskLinks[0].label, `Zadatak, rok ${local}`);
});

test("veze ka nepoznatim funkcijama i spoljnim adresama se ne prikazuju", () => {
  const { pageLinks, taskLinks } = toLinkModels({
    ...ers,
    links: [
      { rel: "quiz", href: `/offerings/${ERS}/quizzes` },
      { rel: "izvodjenje", href: "https://zli-sajt.example" },
      { rel: "assignments", href: "//zli-sajt.example/x" },
      { rel: "materials", href: "javascript:alert(1)" }
    ],
    openTasks: [{ activityType: "assignment", href: "http://zli-sajt.example", deadline: null }]
  });

  assert.deepEqual(pageLinks, []);
  assert.deepEqual(taskLinks, []);
});

test("kartica bez veza nema blok sa vezama", () => {
  assert.equal(createOfferingLinks({ ...ers, links: [], openTasks: [] }, fakeDocument), null);
  assert.equal(createOfferingLinks({ courseName: "Baze podataka" }, fakeDocument), null);
});

test("nacrt koji server ne pošalje ne pojavljuje se na kartici", () => {
  const card = createOfferingCard({ ...ers, openTasks: [] }, fakeDocument);
  const hrefs = anchors(card).map(a => a.getAttribute("href"));

  assert.equal(hrefs.some(h => h.includes(TASK)), false);
  assert.equal(card.byClass("offering-card__task-list"), null);
});

test("isSafeRelativeHref prihvata samo putanje unutar aplikacije", () => {
  assert.equal(isSafeRelativeHref(`/offerings/${ERS}`), true);
  for (const bad of ["", "offerings/1", "//x", "/\\x", "https://x", "javascript:x", "/a b", null, 5]) {
    assert.equal(isSafeRelativeHref(bad), false, String(bad));
  }
});

test("otvaranje veze šalje token u zaglavlju i vraća podatke", async () => {
  let seen;
  const fetchFn = async (url, options) => {
    seen = { url, options };
    return { ok: true, status: 200, json: async () => ({ courseName: "Elementi razvoja softvera", academicYear: "2026/27" }) };
  };

  const content = await openContentLink(`/offerings/${ERS}`, " tok ", fetchFn);

  assert.equal(seen.url, `/offerings/${ERS}`);
  assert.equal(seen.options.headers.Authorization, "Bearer tok");
  assert.equal(describeOpenedContent(content), "Otvoren predmet: Elementi razvoja softvera (2026/27).");
});

test("server proverava ovlašćenje pri otvaranju: 404 daje poruku o pristupu", async () => {
  const fetchFn = async () => ({ ok: false, status: 404 });

  await assert.rejects(
    openContentLink(`/offerings/${ERS}/assignments/${TASK}`, "tok", fetchFn),
    error => error.status === 404 && /nemate pristup/.test(error.message));
});

test("bez sesije ili sa istekom sesije veza se ne otvara", async () => {
  await assert.rejects(openContentLink(`/offerings/${ERS}`, "", async () => ({})), error => error.isUnauthorized);
  await assert.rejects(
    openContentLink(`/offerings/${ERS}`, "tok", async () => ({ ok: false, status: 401 })),
    error => error.isUnauthorized);
});

test("neispravna veza se ne šalje serveru", async () => {
  let called = false;
  await assert.rejects(
    openContentLink("https://zli-sajt.example", "tok", async () => { called = true; }),
    error => error.status === 400);
  assert.equal(called, false);
});

test("jedan klik na vezu otvara sadržaj i ispisuje rezultat", async () => {
  let handler;
  const container = { addEventListener: (type, fn) => { if (type === "click") handler = fn; } };
  const output = { textContent: "", hidden: true };
  const link = { getAttribute: name => (name === "href" ? `/offerings/${ERS}/assignments/${TASK}` : null) };
  let prevented = false;
  const fetchFn = async () => ({ ok: true, status: 200, json: async () => ({ activityId: TASK, deadline: null }) });

  attachContentLinks({ container, output, getToken: () => "tok", fetchFn });
  await handler({ target: { closest: () => link }, preventDefault: () => { prevented = true; } });

  assert.equal(prevented, true);
  assert.equal(output.textContent, "Otvoren zadatak.");
  assert.equal(output.hidden, false);
});

test("klik van veze se ne obrađuje", async () => {
  let handler;
  const container = { addEventListener: (type, fn) => { handler = fn; } };
  const output = { textContent: "", hidden: true };
  let called = false;

  attachContentLinks({ container, output, getToken: () => "tok", fetchFn: async () => { called = true; } });
  const result = handler({ target: { closest: () => null }, preventDefault: () => assert.fail("ne sme") });

  assert.equal(result, undefined);
  assert.equal(called, false);
  assert.equal(output.hidden, true);
});

test("istekla sesija pri otvaranju veze javlja se stranici", async () => {
  const container = { addEventListener: () => {} };
  const output = { textContent: "", hidden: true };
  let signedOut = false;

  const links = attachContentLinks({
    container, output, getToken: () => "tok",
    onUnauthorized: () => { signedOut = true; },
    fetchFn: async () => ({ ok: false, status: 401 })
  });
  await links.open(`/offerings/${ERS}`);

  assert.equal(signedOut, true);
  assert.match(output.textContent, /Sesija ne važi/);
});

test("otvoren deo predmeta (Zadaci) ima svoj opis", () => {
  assert.equal(
    describeOpenedContent({ area: "assignments", courseName: "Elementi razvoja softvera", academicYear: "2026/27" }),
    "Otvoreno: Zadaci, Elementi razvoja softvera (2026/27).");
});
