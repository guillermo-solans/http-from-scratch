---
id: TASK-023
title: Test E2E handshake cliente-servidor
status: todo
priority: critical
phase: fase-5
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

Validación funcional manual y/o automatizada:

### Manual (mínimo obligatorio)
1. Terminal 1: `dotnet run --project src/HttpServerApp -- --tls --port 8443`
2. Terminal 2: `dotnet run --project src/HttpClientApp`
3. En el REPL del cliente: `GET https://localhost:8443/`
4. Resultado esperado: HTML de la home (o 200 OK con cuerpo HTML)
5. Probar también: `GET https://localhost:8443/cats`, `POST https://localhost:8443/cats` con JSON
6. Verificar que sin --tls, todo sigue funcionando igual sobre HTTP plano

### Automatizado (deseable)
Crear `src/TlsLib.Tests/EndToEndHandshakeTests.cs` (proyecto xUnit) o un `TlsLib.Smoke` console app que:
- Lance un TcpListener en localhost
- Hilo servidor: `TlsServerFactory.Accept`, lee bytes, responde "PONG"
- Hilo cliente: `TlsClientFactory.Connect`, escribe "PING", lee respuesta
- Asserts: respuesta == "PONG"
- Caso negativo: cert roto debe lanzar TlsException

### Casos de prueba a verificar
- [ ] Handshake completo cliente <-> servidor en localhost
- [ ] Múltiples requests sobre la misma conexión TLS funcionan (seq num correcto)
- [ ] Cerrar el cliente envía CloseNotify, el servidor lo lee como 0
- [ ] Servidor maneja correctamente múltiples conexiones concurrentes
- [ ] Si se altera 1 byte de un record en tránsito (simulado): handshake o read falla con BadRecordMac

## Criterios de Aceptación
- [ ] Demo manual completa funciona
- [ ] Documentar en docs/tls-demo.md el comando exacto y la salida esperada

## Dependencias
- Bloqueada por: TASK-022

## Comentarios
### 2026-05-09 - project-manager
> El criterio "funciona" se demuestra aquí. Sin esto, no hay nada que entregar.
