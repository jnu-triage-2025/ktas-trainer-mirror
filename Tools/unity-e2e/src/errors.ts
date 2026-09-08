export class E2EError extends Error {
  code: string;
  constructor(code: string, message = code) { super(message); this.code = code; }
}
