# Fases del Proyecto TLS

| Fase | Nombre | Descripción | Días estimados |
|------|--------|-------------|----------------|
| fase-1 | Fundamentos | Estructura csproj, util big-endian, primitivas crypto, certificado | Día 1 mañana |
| fase-2 | Capa Record | TlsRecord reader/writer, handshake reader/writer, hash | Día 1 tarde |
| fase-3 | Mensajes Handshake | ClientHello, ServerHello, Certificate, KeyExchange, Finished | Día 2 |
| fase-4 | Orquestación + Integración | State machine, TlsStream, factories, integración HTTP apps | Día 3 |
| fase-5 | Validación | Tests E2E, captura Wireshark, documentación demo | Día 4 |

Approach técnico: TLS 1.2 con ECDHE_RSA_WITH_AES_128_GCM_SHA256.
Ver: `docs/coordination/tls-coordination-contract.md`
