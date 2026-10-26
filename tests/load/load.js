// Load: a carga esperada. 50 usuários simultâneos por 3 minutos, com rampa.
// O alvo vem do PRD: p95 < 200 ms. Se falhar aqui, não vai para produção.
import { sleep } from 'k6';
import { registerUsers, tokenForThisVu, userJourney } from './lib/api.js';

const USERS = 50;

export const options = {
  stages: [
    { duration: '1m', target: USERS },
    { duration: '3m', target: USERS },
    { duration: '1m', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.001'],
    http_req_duration: ['p(95)<200', 'p(99)<500'],
    'http_req_duration{name:POST /tasks}': ['p(95)<300'],
    checks: ['rate>0.999'],
  },
  setupTimeout: '2m',
};

export const setup = () => ({ tokens: registerUsers(USERS) });

export default function ({ tokens }) {
  userJourney(tokenForThisVu(tokens));
  sleep(1);
}
