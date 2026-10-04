#Genera el Plan
Claude Analiza los siguientes problemas, explora el código, y produzce un plan explícito paso a paso (arquitectura, archivos a tocar, orden de cambios, criterios de aceptación). No edites nada todavía.

#Ejecuta el plan
/codex:rescue Implementa lo que falta del plan exactamente como está descrito en la fase 3
No te desvíes del plan sin antes reportar por qué. Para tareas largas, fuerza background: /codex:rescue --background <tarea>

#cuestiona si el approach de Codex fue el correcto
/codex:adversarial-review