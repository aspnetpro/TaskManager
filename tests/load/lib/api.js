// Cliente mínimo da Api para os cenários k6. Passa pelo nginx (como em produção):
// BASE_URL padrão = frontend do compose. Rode a stack com os limites de rate limit
// altos (ver README) — senão o teste mede o rate limiter, não a Api.
import http from 'k6/http';
import { check, fail } from 'k6';
import exec from 'k6/execution';

export const BASE_URL = __ENV.BASE_URL || 'http://localhost:3000/api/v1';

const PRIORITIES = ['Low', 'Medium', 'High', 'Critical'];

function json(token) {
  const headers = { 'Content-Type': 'application/json' };
  if (token) headers.Authorization = `Bearer ${token}`;
  return { headers };
}

// Um usuário (e um workspace) por VU: o cache e o filtro de tenant trabalham como em produção,
// e nenhum VU disputa a mesma tarefa com outro (sem 409 artificial de concorrência).
export function registerUsers(count) {
  const run = Date.now().toString(36);
  const tokens = [];
  for (let i = 0; i < count; i++) {
    const res = http.post(`${BASE_URL}/auth/register`, JSON.stringify({
      email: `k6-${run}-${i}@load.test`,
      password: 'load-test-password-123',
      fullName: `Load User ${i}`,
      workspaceName: `k6 ${run} ${i}`,
    }), json());

    if (res.status === 429)
      fail('Rate limit no registro: suba a stack com AUTH_PERMIT_LIMIT/GLOBAL_PERMIT_LIMIT altos (tests/load/README.md).');
    if (res.status !== 201 && res.status !== 200)
      fail(`Registro falhou: ${res.status} ${res.body}`);

    tokens.push(res.json('accessToken'));
  }
  return tokens;
}

export function tokenForThisVu(tokens) {
  return tokens[(exec.vu.idInTest - 1) % tokens.length];
}

// Jornada típica: cria, lista, abre o detalhe e às vezes leva a tarefa até Done.
export function userJourney(token) {
  const title = `k6 ${exec.vu.idInTest}-${exec.vu.iterationInScenario} ${Math.random().toString(36).slice(2, 8)}`;
  const created = http.post(`${BASE_URL}/tasks`, JSON.stringify({
    title,
    description: 'Gerada pelo teste de carga',
    priority: PRIORITIES[Math.floor(Math.random() * PRIORITIES.length)],
  }), { ...json(token), tags: { name: 'POST /tasks' } });
  if (!check(created, { 'create 201': r => r.status === 201 })) return;

  const id = created.json('id');

  const list = http.get(`${BASE_URL}/tasks?page=1&pageSize=20`, { ...json(token), tags: { name: 'GET /tasks' } });
  check(list, { 'list 200': r => r.status === 200 });

  const detail = http.get(`${BASE_URL}/tasks/${id}`, { ...json(token), tags: { name: 'GET /tasks/{id}' } });
  check(detail, { 'get 200': r => r.status === 200 });

  if (Math.random() < 0.3) {
    for (const status of ['InProgress', 'Done']) {
      const moved = http.patch(`${BASE_URL}/tasks/${id}/status`, JSON.stringify({ status }),
        { ...json(token), tags: { name: 'PATCH /tasks/{id}/status' } });
      check(moved, { 'transition 200': r => r.status === 200 });
    }
  }
}
