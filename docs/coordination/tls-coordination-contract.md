# Contrato de Coordinación — TLS Avanzado sobre TCP raw

**Fecha decisión:** 2026-05-09
**Entrega:** 2026-05-13 (4 días)
**Project Manager:** software-factory PM
**Approach elegido:** TLS 1.2 (Propuesta #1)

---

## 1. APPROACH ELEGIDO — TLS 1.2

### Justificación

Se elige **TLS 1.2** sobre TLS 1.3 por las siguientes razones, en orden de peso:

1. **Restricción temporal real (4 días):** El analyzer estimó TLS 1.3 en 4-6 días. Con margen para QA, integración y bugs imprevistos, TLS 1.3 entra en zona de riesgo de no entregar. TLS 1.2 estimado en 2-3 días deja un día completo de buffer.
2. **Contexto académico:** Es un trabajo universitario. El criterio de evaluación premia *funcionalidad demostrable + comprensión* sobre *modernidad técnica*. Un TLS 1.2 funcional con demo Wireshark vale más que un TLS 1.3 a medias.
3. **Handshake en claro = demo defendible:** El handshake de TLS 1.2 es visible en Wireshark hasta el ChangeCipherSpec. Esto permite a los estudiantes señalar literalmente cada mensaje en la demo y explicarlo. Con TLS 1.3 cifrado desde ServerHello, la demo se convierte en "confíen en que esto funciona".
4. **Documentación abundante:** RFC 5246 + miles de tutoriales, diagramas, ejemplos en hex. RFC 8446 (TLS 1.3) tiene menos material didáctico.
5. **PRF vs HKDF:** Aunque HKDF es más limpio, P_SHA256 con HMAC se implementa en ~30 líneas. No es una desventaja real.

**Trade-off aceptado:** Sacrificamos modernidad y elegancia criptográfica por riesgo de entrega y claridad pedagógica.

### Especificación técnica congelada

| Parámetro | Valor |
|-----------|-------|
| Versión TLS | 1.2 (record version 0x0303) |
| Cipher Suite | `TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256` (0xC02F) |
| Curva ECDHE | secp256r1 (P-256), named curve 0x0017 |
| Cifrado simétrico | AES-128-GCM (key=16B, IV=12B, tag=16B) |
| MAC en handshake | HMAC-SHA256 (vía P_SHA256 PRF) |
| Firma servidor | RSA-PKCS1-v1_5 con SHA-256 (sigalg 0x0401) |
| Compresión | null (0x00) — única soportada |
| Extensions cliente | supported_groups, ec_point_formats, signature_algorithms |
| Certificado | Autofirmado RSA-2048, generado en arranque del servidor |
| Renegotiation | NO soportado |
| Session resumption | NO soportado |
| Client auth | NO soportado |

---

## 2. ESTRUCTURA DE CARPETAS — TlsLib

```
src/TlsLib/
├── TlsLib.csproj
├── TlsStream.cs                    # Decorator Stream público (API principal)
├── TlsClientFactory.cs             # Factory: NetworkStream -> TlsStream (cliente)
├── TlsServerFactory.cs             # Factory: NetworkStream -> TlsStream (servidor)
│
├── Protocol/
│   ├── TlsConstants.cs             # Magic numbers, enums (ContentType, HandshakeType, etc.)
│   ├── ContentType.cs              # enum: ChangeCipherSpec=20, Alert=21, Handshake=22, AppData=23
│   ├── HandshakeType.cs            # enum: ClientHello=1, ServerHello=2, Certificate=11, ...
│   ├── AlertLevel.cs               # enum: Warning=1, Fatal=2
│   ├── AlertDescription.cs         # enum: CloseNotify=0, BadRecordMac=20, ...
│   └── CipherSuite.cs              # const ushort ECDHE_RSA_AES128_GCM_SHA256 = 0xC02F
│
├── Records/
│   ├── TlsRecord.cs                # struct: ContentType, Version, Length, Payload
│   ├── TlsRecordReader.cs          # Lee 5 bytes header + payload del Stream subyacente
│   └── TlsRecordWriter.cs          # Serializa record al Stream subyacente
│
├── Handshake/
│   ├── HandshakeMessage.cs         # base abstract: Type, Length, Body bytes
│   ├── ClientHello.cs              # Random, SessionId, CipherSuites, Compression, Extensions
│   ├── ServerHello.cs              # Random, SessionId, CipherSuite elegida, Compression, Extensions
│   ├── CertificateMessage.cs       # Lista de certificados DER
│   ├── ServerKeyExchange.cs        # Curva, EC point público, firma RSA
│   ├── ServerHelloDone.cs          # Vacío
│   ├── ClientKeyExchange.cs        # EC point público del cliente
│   ├── FinishedMessage.cs          # 12 bytes verify_data (PRF sobre handshake hash)
│   ├── HandshakeReader.cs          # Reensambla mensajes (puede venir fragmentado en records)
│   └── HandshakeWriter.cs          # Empaqueta y emite handshake records
│
├── Crypto/
│   ├── PrfSha256.cs                # P_SHA256 + PRF(secret, label, seed, length)
│   ├── KeyDerivation.cs            # master_secret + key_block (client/server keys, IVs)
│   ├── EcdheP256.cs                # GenerateKeyPair, DeriveSharedSecret (usa ECDiffieHellman)
│   ├── RsaSigner.cs                # SignSha256, VerifySha256 (PKCS1-v1_5)
│   ├── AesGcmCipher.cs             # Encrypt(seq, plaintext, aad)/Decrypt(seq, ciphertext, aad)
│   └── HandshakeHash.cs            # SHA256 acumulativo de TODOS los handshake messages
│
├── State/
│   ├── TlsConnectionState.cs       # masterSecret, keys, IVs, seqNum read/write, role
│   ├── HandshakeContext.cs         # clientRandom, serverRandom, cert, ecdheKeys, hash
│   └── HandshakeStateMachine.cs    # Orquesta el handshake (cliente y servidor)
│
├── Certificates/
│   └── SelfSignedCertificateGenerator.cs  # Genera RSA-2048 X.509 autofirmado en memoria
│
└── Util/
    ├── BigEndianWriter.cs          # WriteUInt8/16/24/32, WriteVector8/16/24
    ├── BigEndianReader.cs          # ReadUInt8/16/24/32, ReadVector8/16/24
    └── SecureRandom.cs             # Wrapper RandomNumberGenerator (32 bytes random + GMT)
```

**Total estimado:** ~1500-1800 LOC repartidos en ~30 archivos.

---

## 3. INTERFACES PÚBLICAS

### 3.1 `TlsStream` (API principal — la única que ven HttpClient/HttpServer)

```csharp
namespace TlsLib;

public sealed class TlsStream : Stream
{
    // Constructor interno — usar las factories
    internal TlsStream(Stream innerStream, TlsConnectionState state);

    public override bool CanRead => true;
    public override bool CanWrite => true;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override int Read(byte[] buffer, int offset, int count);
    public override void Write(byte[] buffer, int offset, int count);
    public override void Flush();

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

    public override void SetLength(long value) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    protected override void Dispose(bool disposing); // Envía CloseNotify alert antes de cerrar
}
```

### 3.2 Factories — punto de entrada

```csharp
namespace TlsLib;

public static class TlsClientFactory
{
    /// <summary>
    /// Establece TLS 1.2 sobre el stream dado, actuando como cliente.
    /// Lanza TlsException si el handshake falla.
    /// </summary>
    public static TlsStream Connect(Stream innerStream, string serverName, TlsClientOptions? options = null);
}

public static class TlsServerFactory
{
    /// <summary>
    /// Acepta TLS 1.2 sobre el stream dado, actuando como servidor.
    /// </summary>
    public static TlsStream Accept(Stream innerStream, TlsServerOptions options);
}

public sealed class TlsClientOptions
{
    public bool AllowSelfSignedCertificates { get; init; } = true; // Para uso académico
    public Action<string>? Logger { get; init; } // Hook para logs por consola
}

public sealed class TlsServerOptions
{
    public required X509Certificate2 ServerCertificate { get; init; } // RSA con clave privada
    public Action<string>? Logger { get; init; }
}
```

### 3.3 Excepciones

```csharp
public sealed class TlsException : Exception
{
    public AlertDescription? Alert { get; }
    public TlsException(string message, AlertDescription? alert = null);
}
```

### 3.4 Generador de certificado (público — el HttpServer lo usará)

```csharp
namespace TlsLib.Certificates;

public static class SelfSignedCertificateGenerator
{
    public static X509Certificate2 Generate(
        string subjectName = "CN=localhost",
        int rsaKeySizeBits = 2048,
        TimeSpan? validity = null);
}
```

---

## 4. ORDEN DE IMPLEMENTACIÓN (DEPENDENCIAS)

Las tareas se agrupan en 4 fases secuenciales. Dentro de cada fase, las tareas son **paralelizables**.

### Fase 1 — Fundamentos (Día 1, mañana) — PARALELO
Todas pueden hacerse en paralelo, no dependen entre sí.
- TASK-001: TlsLib.csproj + estructura de carpetas + constantes/enums
- TASK-002: BigEndianReader / BigEndianWriter / SecureRandom
- TASK-003: SelfSignedCertificateGenerator
- TASK-004: PrfSha256 + KeyDerivation
- TASK-005: EcdheP256 + RsaSigner + AesGcmCipher

### Fase 2 — Capa Record (Día 1, tarde) — SECUENCIAL tras Fase 1
- TASK-006: TlsRecord + TlsRecordReader + TlsRecordWriter (sin cifrar)
- TASK-007: HandshakeReader + HandshakeWriter (reensamblado fragmentación)
- TASK-008: HandshakeHash (acumulador SHA256)

### Fase 3 — Mensajes Handshake (Día 2) — PARALELIZABLE
- TASK-009: ClientHello (serializar/deserializar)
- TASK-010: ServerHello (serializar/deserializar)
- TASK-011: CertificateMessage
- TASK-012: ServerKeyExchange (con firma RSA)
- TASK-013: ServerHelloDone + ClientKeyExchange
- TASK-014: FinishedMessage (verify_data via PRF)

### Fase 4 — Orquestación + Integración (Día 3) — SECUENCIAL
- TASK-015: TlsConnectionState + HandshakeContext
- TASK-016: HandshakeStateMachine — lado CLIENTE
- TASK-017: HandshakeStateMachine — lado SERVIDOR
- TASK-018: TlsStream (Read/Write con AES-GCM, ChangeCipherSpec, CloseNotify)
- TASK-019: TlsClientFactory + TlsServerFactory + TlsClientOptions/ServerOptions
- TASK-020: Integración en SocketHttpClient (parámetro `useTls`)
- TASK-021: Integración en HttpServer (constructor acepta `X509Certificate2?`)
- TASK-022: CLI flags en HttpClientApp (`--tls`) y HttpServerApp (`--tls --port 8443`)

### Fase 5 — Validación (Día 4) — QA
- TASK-023: Tests handshake end-to-end (cliente HttpClientApp ↔ servidor HttpServerApp)
- TASK-024: Captura Wireshark + documentación de cada mensaje
- TASK-025: README de TlsLib + diagrama de secuencia + instrucciones de demo

---

## 5. ARCHIVOS EXISTENTES QUE SE MODIFICAN

### `src/HttpClientApp/SocketHttpClient.cs`
**Cambio:** Aceptar parámetro opcional para envolver con TLS.

```csharp
public static (HttpResponse Response, TimeSpan Elapsed) Send(
    HttpRequest request,
    ParsedUrl url,
    int timeoutMs = 30000,
    bool useTls = false)   // ← NUEVO
{
    // ... TcpClient.Connect igual ...
    Stream stream = tcp.GetStream();
    if (useTls)
        stream = TlsClientFactory.Connect(stream, url.Host);   // ← NUEVO
    using var _ = stream;
    // ... resto idéntico (la API del Stream no cambia) ...
}
```

### `src/HttpClientApp/Program.cs`
**Cambio:** Añadir flag `--tls` o detectar `https://` en la URL.

### `src/HttpServerApp/Server/HttpServer.cs`
**Cambio:** Constructor acepta certificado opcional. Si no es null, envuelve cada cliente con TLS.

```csharp
public HttpServer(int port, Router router, Func<HttpRequest, Task<HttpResponse>>? fallback = null,
                  X509Certificate2? serverCertificate = null)   // ← NUEVO
```

En `HandleClientAsync`, antes de leer:
```csharp
Stream stream = client.GetStream();
if (_serverCertificate is not null)
    stream = TlsServerFactory.Accept(stream, new TlsServerOptions { ServerCertificate = _serverCertificate });
// ... resto idéntico ...
```

### `src/HttpServerApp/Program.cs`
**Cambio:** Si llega flag `--tls`, generar certificado autofirmado y pasarlo al `HttpServer`.

### `src/HttpClientApp/HttpClientApp.csproj` y `src/HttpServerApp/HttpServerApp.csproj`
**Cambio:** Añadir `<ProjectReference Include="..\TlsLib\TlsLib.csproj" />`.

**REGLA INVIOLABLE:** No se cambia ninguna firma pública de `HttpRequest`, `HttpResponse`, `Router`, `HttpResponseReader`, `RequestReader`. La integración debe respetar la abstracción `Stream`.

---

## 6. CONVENCIONES

### Namespaces
- `TlsLib` — API pública (TlsStream, factories, options, exceptions)
- `TlsLib.Protocol` — enums y constantes
- `TlsLib.Records` — capa Record
- `TlsLib.Handshake` — mensajes de handshake
- `TlsLib.Crypto` — primitivas criptográficas
- `TlsLib.State` — estado de conexión y máquina de estados
- `TlsLib.Certificates` — generación de certificados
- `TlsLib.Util` — helpers (BigEndian, SecureRandom)

### Naming
- Clases: `PascalCase`
- Métodos públicos: `PascalCase`
- Privados/locals: `camelCase`
- Constantes: `PascalCase` (ej: `MaxRecordSize = 16384`)
- Tipos sealed por defecto donde aplique
- `internal` para tipos que no deben exponerse fuera de TlsLib

### Estilo
- Nullable enabled
- ImplicitUsings enabled
- File-scoped namespaces (`namespace X;`)
- `byte[]` para buffers; `Span<byte>` / `ReadOnlySpan<byte>` en código caliente (cifrado)
- BigEndian SIEMPRE en wire format (TLS es big-endian)
- Logging vía `options.Logger?.Invoke($"[tls] ...")`. NUNCA `Console.WriteLine` directo dentro de TlsLib.

### Errores
- Cualquier fallo de protocolo lanza `TlsException` con su `AlertDescription`
- Antes de lanzar, intentar enviar el alert correspondiente al peer (best-effort)
- Timeout en handshake: 10 segundos por defecto

### Testing manual obligatorio (criterio de DONE)
1. `dotnet run --project src/HttpServerApp -- --tls --port 8443` arranca sin error
2. `dotnet run --project src/HttpClientApp` con URL `https://localhost:8443/cats` recibe respuesta JSON correcta
3. `curl --insecure https://localhost:8443/` NO debe funcionar necesariamente (es OK que solo nuestro cliente hable con nuestro servidor — somos la implementación de referencia)
4. Captura Wireshark muestra el handshake completo identificable

---

## 7. ASIGNACIÓN

Todas las tareas son **backend C# .NET 10**. El equipo dev las recoge en orden de fases. El PM asignará a los teammates dev disponibles cuando se invoque. Por ahora todas las tareas quedan en `todo/` sin asignar — la asignación la hace el orquestador / dev lead al lanzar la fase de implementación.
