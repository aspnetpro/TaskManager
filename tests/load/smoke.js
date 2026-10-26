// Smoke: 1 usuário, 30 s. Não mede capacidade — prova que a jornada funciona
// antes de gastar 5 minutos num teste de carga que falharia no primeiro request.
import { sleep } from 'k6';
import { registerUsers, tokenForThisVu, userJourney } from './lib/api.js';

export const options = {
  vus: 1,
  duration: '30s',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500', 'p(99)<1000'],
    checks: ['rate>0.99'],
  },
};

export const setup = () => ({ tokens: registerUsers(1) });

export default function ({ tokens }) {
  userJourney(tokenForThisVu(tokens));
  sleep(1);
}
