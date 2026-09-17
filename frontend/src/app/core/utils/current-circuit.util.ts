interface CircuitLike {
  estado?: string;
  lastSyncAt?: string | null;
  updatedAt?: string | null;
  nombre?: string;
}

/** Elige el circuito "vigente" de una lista: Activo > Próximo > otro, luego más recientemente sincronizado/actualizado, luego nombre. */
export function pickCurrentCircuit<T extends CircuitLike>(circuits: T[]): T | null {
  if (!circuits.length) return null;

  const statusPriority = (estado?: string) => estado === 'Activo' ? 0 : estado === 'Próximo' ? 1 : 2;

  return [...circuits].sort((a, b) => {
    const byStatus = statusPriority(a.estado) - statusPriority(b.estado);
    if (byStatus !== 0) return byStatus;
    const aDate = new Date(a.lastSyncAt ?? a.updatedAt ?? 0).getTime();
    const bDate = new Date(b.lastSyncAt ?? b.updatedAt ?? 0).getTime();
    if (aDate !== bDate) return bDate - aDate;
    return (a.nombre ?? '').localeCompare(b.nombre ?? '');
  })[0];
}
