---
id: TASK-025
title: README de TlsLib + diagrama de secuencia + instrucciones de demo
status: todo
priority: high
phase: fase-5
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

Crear `src/TlsLib/README.md` con:

### Estructura
1. **Resumen**: qué es TlsLib, qué implementa, qué NO implementa
2. **Cipher suite**: TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256 — desglosado en sus componentes
3. **Diagrama de secuencia ASCII** del handshake
4. **Cómo usarlo desde código** (cliente y servidor) — snippets completos
5. **Cómo correr la demo**:
   ```
   Terminal 1: dotnet run --project src/HttpServerApp -- --tls --port 8443
   Terminal 2: dotnet run --project src/HttpClientApp
                > GET https://localhost:8443/cats
   ```
6. **Estructura del proyecto** — explicar cada subcarpeta de TlsLib
7. **Limitaciones conocidas**:
   - Solo TLS 1.2
   - Solo un cipher suite (ECDHE_RSA_AES128_GCM_SHA256)
   - No session resumption
   - No client auth
   - No renegotiation
   - Cert autofirmado solo (sin validación de cadena)
   - Solo P-256
8. **Referencias**: RFC 5246, RFC 5288, RFC 4492, RFC 5289

Adicionalmente, actualizar el `README.md` raíz del proyecto añadiendo una sección "TLS" con el comando para arrancar en modo TLS y un enlace a `src/TlsLib/README.md`.

## Criterios de Aceptación
- [ ] src/TlsLib/README.md existe con todas las secciones
- [ ] Diagrama de secuencia legible
- [ ] Instrucciones de demo reproducibles paso a paso
- [ ] README raíz menciona TLS

## Dependencias
- Bloqueada por: TASK-022 (necesita CLI funcional para documentar)

## Comentarios
### 2026-05-09 - project-manager
> Documentación final. Si la demo en clase falla, este README es el plan B.
