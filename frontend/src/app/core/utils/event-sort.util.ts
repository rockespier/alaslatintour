/**
 * Orders events by relevance for public-facing displays: events happening in the
 * current month first (soonest first), then upcoming events (soonest first), then
 * past events last (most recent first). The events API sorts ascending by fechaInicio
 * from the earliest record in the database, which surfaces stale/past events first
 * once the circuit has been running for a while.
 */
export function sortEventsForDisplay<T extends { fechaInicio: string }>(events: T[]): T[] {
  // fechaInicio es una fecha "solo fecha" (el backend la normaliza a medianoche UTC), no un
  // instante real. Comparar sus componentes con getFullYear()/getMonth() locales corre el día
  // hacia atrás para usuarios en husos horarios negativos (toda Sudamérica): un evento del
  // 1 de agosto (UTC) se leería como 31 de julio. Se usan los getters UTC para que la
  // clasificación no dependa del huso horario del navegador.
  const now = new Date();
  const currentYear = now.getFullYear();
  const currentMonth = now.getMonth();

  const currentMonthEvents: T[] = [];
  const upcomingEvents: T[] = [];
  const pastEvents: T[] = [];

  for (const event of events) {
    const date = new Date(event.fechaInicio);
    if (date.getUTCFullYear() === currentYear && date.getUTCMonth() === currentMonth) {
      currentMonthEvents.push(event);
    } else if (date.getTime() > now.getTime()) {
      upcomingEvents.push(event);
    } else {
      pastEvents.push(event);
    }
  }

  currentMonthEvents.sort((a, b) => a.fechaInicio.localeCompare(b.fechaInicio));
  upcomingEvents.sort((a, b) => a.fechaInicio.localeCompare(b.fechaInicio));
  pastEvents.sort((a, b) => b.fechaInicio.localeCompare(a.fechaInicio));

  return [...currentMonthEvents, ...upcomingEvents, ...pastEvents];
}
