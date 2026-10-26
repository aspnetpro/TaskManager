// Stress: rampa até 200 usuários para achar o ponto de degradação.
// Thresholds frouxos de propósito: o objetivo é DOCUMENTAR onde o p95 passa de 1 s,
// não passar. Anote o número de VUs no momento da quebra em docs/load-test-results.md.
import { sleep } from 'k6';
import { registerUsers, tokenForThisVu, userJourney } from './lib/api.js';

const USERS = 200;

export const options = {
  stages: [
    { duration: '2m', target: USERS },
    { duration: '2m', target: USERS },
    { duration: '1m', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.05'],
    // abortOnFail: não adianta martelar um sistema que já caiu.
    http_req_duration: [{ threshold: 'p(95)<3000', abortOnFail: true, delayAbortEval: '30s' }],
  },
  setupTimeout: '5m',
};

export const setup = () => ({ tokens: registerUsers(USERS) });

export default function ({ tokens }) {
  userJourney(tokenForThisVu(tokens));
  sleep(1);
}
