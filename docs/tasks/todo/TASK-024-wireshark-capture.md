---
id: TASK-024
title: Captura Wireshark del handshake + anotación
status: todo
priority: high
phase: fase-5
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

Capturar el handshake en Wireshark y documentarlo. Esto es CLAVE para la defensa académica.

### Pasos
1. Abrir Wireshark, capturar interfaz `Loopback` (Windows) con filtro `tcp.port == 8443`
2. Arrancar servidor: `dotnet run --project src/HttpServerApp -- --tls --port 8443`
3. Lanzar cliente con un GET hacia https://localhost:8443/
4. Detener captura. Guardar como `docs/wireshark/tls-handshake.pcapng`
5. Hacer screenshots del Wireshark mostrando:
   - ClientHello (con cipher suites, random, SNI)
   - ServerHello (con cipher suite seleccionado, random)
   - Certificate (con el cert)
   - ServerKeyExchange (con curva P-256, public point, firma)
   - ServerHelloDone
   - ClientKeyExchange
   - ChangeCipherSpec (cliente)
   - Finished cliente (cifrado: aparece como "Encrypted Handshake Message")
   - ChangeCipherSpec (servidor)
   - Finished servidor (cifrado)
   - Application Data (HTTP cifrado)
6. Guardar screenshots en `docs/wireshark/screenshots/01-client-hello.png` etc.

### Documento
Crear `docs/wireshark/handshake-walkthrough.md`:
- Por cada mensaje, una sección con:
  - Screenshot anotado
  - Bytes raw destacados
  - Explicación de los campos clave
- Sección "Por qué TLS 1.2 y no 1.3": handshake visible facilita el debug y la demo.

## Criterios de Aceptación
- [ ] Archivo .pcapng en docs/wireshark/
- [ ] Mínimo 6 screenshots de mensajes de handshake
- [ ] Walkthrough markdown completo

## Dependencias
- Bloqueada por: TASK-023

## Comentarios
### 2026-05-09 - project-manager
> Esto es lo que demuestra que entendemos lo que hicimos. Vale tanto como el código.
