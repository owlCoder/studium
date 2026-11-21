// Minimalan zamenski DOM za testove kartice, bez pregledača i bez paketa.
// Postavljanje innerHTML baca grešku, pa test pada ako kartica ikad upiše HTML.

export class FakeElement {
  constructor(tagName) {
    this.tagName = tagName.toUpperCase();
    this.className = "";
    this.id = "";
    this.children = [];
    this.attributes = new Map();
    this._text = null;
  }

  setAttribute(name, value) {
    this.attributes.set(name, String(value));
  }

  getAttribute(name) {
    return this.attributes.has(name) ? this.attributes.get(name) : null;
  }

  append(...nodes) {
    this.children.push(...nodes);
  }

  replaceChildren(...nodes) {
    this.children = [...nodes];
  }

  get textContent() {
    return this._text ?? this.children.map(child => child.textContent).join("");
  }

  set textContent(value) {
    this._text = String(value);
    this.children = [];
  }

  set innerHTML(_) {
    throw new Error("Kartica ne sme da koristi innerHTML.");
  }

  find(predicate) {
    for (const child of this.children) {
      if (predicate(child)) {
        return child;
      }
      const nested = child.find(predicate);
      if (nested) {
        return nested;
      }
    }
    return null;
  }

  byClass(className) {
    return this.find(element => element.className === className);
  }
}

export const fakeDocument = {
  createElement: tagName => new FakeElement(tagName)
};
