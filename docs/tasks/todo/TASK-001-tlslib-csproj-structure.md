---
id: TASK-001
title: Crear proyecto TlsLib (csproj + estructura de carpetas + constantes)
status: todo
priority: critical
phase: fase-1
assignee: unassigned
created: 2026-05-09
updated: 2026-05-09
---

# Descripción

Crear el nuevo proyecto `src/TlsLib/TlsLib.csproj` (.NET 10, Nullable enabled, ImplicitUsings enabled, file-scoped namespaces).

Crear la estructura de carpetas vacía según el contrato:
- `Protocol/`, `Records/`, `Handshake/`, `Crypto/`, `State/`, `Certificates/`, `Util/`

Crear los archivos de constantes y enums en `Protocol/`:
- `TlsConstants.cs` — `MaxRecordSize=16384`, versión `0x0303`, magic numbers
- `ContentType.cs` — enum byte: `ChangeCipherSpec=20, Alert=21, Handshake=22, ApplicationData=23`
- `HandshakeType.cs` — enum byte: `ClientHello=1, ServerHello=2, Certificate=11, ServerKeyExchange=12, ServerHelloDone=14, ClientKeyExchange=16, Finished=20`
- `AlertLevel.cs` — enum byte: `Warning=1, Fatal=2`
- `AlertDescription.cs` — enum byte con los códigos relevantes (CloseNotify=0, UnexpectedMessage=10, BadRecordMac=20, HandshakeFailure=40, IllegalParameter=47, DecodeError=50, DecryptError=51, ProtocolVersion=70, InternalError=80)
- `CipherSuite.cs` — `public const ushort ECDHE_RSA_WITH_AES_128_GCM_SHA256 = 0xC02F;` y `NamedCurve_secp256r1 = 0x0017`

Añadir `<ProjectReference>` desde HttpClientApp y HttpServerApp hacia TlsLib.

Crear la excepción `TlsException` en raíz del namespace `TlsLib`.

## Criterios de Aceptación
- [ ] `dotnet build src/TlsLib/TlsLib.csproj` compila sin errores ni warnings
- [ ] `dotnet build` global de la solución compila
- [ ] Existen los 7 directorios y los 6 archivos de constantes
- [ ] HttpClientApp y HttpServerApp referencian TlsLib

## Comentarios
### 2026-05-09 - project-manager
> Tarea creada. Es la base de todo lo demás. Bloquea TASK-002..025.
