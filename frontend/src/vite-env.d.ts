/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Origem da Api quando ela não está atrás do mesmo host (ex.: https://api.taskmanager.dev). */
  readonly VITE_API_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
