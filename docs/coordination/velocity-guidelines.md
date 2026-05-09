# Guía de velocidad — Directriz del team-lead 2026-05-09

> "Prioriza velocidad de implementación. El código lo generamos rápido, no hace falta sobredimensionar."

Esta guía establece QUÉ recortar si vamos justos de tiempo y QUÉ no negociar nunca.

## NO NEGOCIABLE (sin esto no hay entrega)

- Cipher suite **único**: 0xC02F. No implementar más. Si el cliente no lo ofrece, fallar con HandshakeFailure.
- Curva **única**: secp256r1. No implementar más.
- Versión **única**: TLS 1.2 (0x0303). Rechazar todo lo demás.
- Cert autofirmado RSA-2048 generado en arranque. No cargar de disco, no PFX, no cadenas.
- Handshake completo + AppData cifrado round-trip funcional.
- TASK-023 (E2E demo) debe pasar.
- TASK-024 (captura Wireshark) básica.

## SIMPLIFICACIONES PERMITIDAS si vamos justos

### TASK-009 ClientHello — extensions
- SNI: opcional, omitir si genera bugs. Solo afecta si quisiéramos hablar con servidores reales (no es nuestro caso).
- signature_algorithms: enviar solo `0x0401` (rsa_pkcs1_sha256). NO listar más.
- ec_point_formats: enviar solo `[0x00]` (uncompressed).
- supported_groups: enviar solo `[0x0017]` (secp256r1).
- Si el parseo de extensions del peer da problemas, **ignorarlas todas** en el servidor (asumir que el cliente está bien configurado, dado que es el nuestro).

### TASK-010 ServerHello — extensions
- Emitir `Extensions` con length 0 (vector vacío) o **directamente omitir el bloque**. Ambos son válidos en TLS 1.2.

### TASK-018 TlsStream — fragmentación
- Documentar que NO fragmentamos en escritura si payload < 16384. Si > 16384, partir simple en chunks de 16384. No optimizar.
- En lectura, si llegan records pequeños del peer, los servimos uno a uno. No buffer agresivo.

### TASK-018 — CloseNotify
- Implementar SOLO el envío en Dispose. La detección al leer (devolver 0 EOF) está bien, pero si el peer cierra el TCP sin CloseNotify, tratar como `0` también (no abortar).
- No implementar reintentos ni timeouts complejos.

### TASK-022 CLI — Programs
- Parsing de args con `args.Contains` y un helper `ParseIntFlag` ad-hoc. NO añadir System.CommandLine ni librerías de CLI.

### TASK-023 — Tests
- Solo manual obligatorio. El smoke automatizado (proyecto xUnit) es DESEABLE pero recortable.
- Caso negativo (alterar 1 byte en wire) es recortable — basta argumentar en defensa que la integridad la da AES-GCM tag.

### TASK-024 — Wireshark
- Mínimo: 1 captura `.pcapng` y screenshots de los mensajes principales. Anotación textual en el README puede sustituir un walkthrough separado.
- Si el tiempo aprieta, fusionar TASK-024 y TASK-025 en un único `docs/tls-demo.md`.

### Logging
- Logger inyectado vía options.Logger (ya está en el contrato). Implementación en TlsLib: una sola línea por mensaje del handshake con su tipo y tamaño.
- No JSON, no estructurado, no niveles. `Console.WriteLine($"[tls/client] -> ClientHello (32B random)")` es suficiente.

## ANTIPATRONES A EVITAR (sobredimensionar)

- ❌ Tests unitarios para cada tipo de extension parseable
- ❌ Refactor de HttpClient/HttpServer más allá del parámetro nuevo
- ❌ Métodos `*Async` cuando no aporta (el servidor existente sí es async, mantener async; el cliente existente es sync, no convertir)
- ❌ Inyección de dependencias / DI containers en TlsLib
- ❌ Soporte de TLS 1.0/1.1 "por si acaso"
- ❌ Más cipher suites de los necesarios
- ❌ Renegotiation, session tickets, false start, OCSP stapling, ALPN — NADA de esto entra
- ❌ Validación de cadena de certificados (es autofirmado siempre)
- ❌ Pooling de buffers, ArrayPool, optimizaciones prematuras
- ❌ Documentación XML doc en cada método (solo en API pública)

## REGLA DE ORO

**"¿Esto bloquea la demo del 13 de mayo?"**
- Sí → hazlo
- No → siguiente
