# Especificação de Testes: QuickAuth.WebApi

Você é um especialista em testes em **.NET 10** usando **xUnit**, **NSubstitute** e **FluentAssertions**.
Com base na estrutura real do projeto e nas especificações abaixo, gere os testes **unitários** e de **integração**.

> ⚠️ Este spec foi alinhado ao código-fonte atual. As seções marcadas com 🧭 descrevem
> comportamentos reais que diferem do que se esperaria "no papel" — leia-as antes de escrever os testes.

---

## 🏢 Estrutura Real do Projeto

```
QuickAuth/
│
├── QuickAuth.sln
├── todo-api.spec.md
├── src/
│   └── QuickAuth.WebApi/                    (o projeto fica DENTRO de src/)
│       ├── Application/
│       │   ├── Configurations/JwtSettings.cs
│       │   ├── DTOs/Requests/SignInRequest.cs
│       │   ├── DTOs/Responses/SignInResponse.cs
│       │   ├── Services/ITokenValidator.cs
│       │   └── Services/Impl/TokenValidator.cs
│       ├── Controllers/AuthController.cs
│       ├── Controllers/SignInController.cs
│       ├── Domain/Extensions/StringExtensions.cs   (namespace: ValueOfObjects; atenção: pasta ≠ namespace)
│       ├── Middlewares/AuthMiddleware.cs
│       ├── Program.cs                       (contém `public partial class Program { }` no fim)
│       └── QuickAuth.WebApi.csproj
│
└── tests/
    └── QuickAuth.Tests/
        └── QuickAuth.Tests.csproj           (ProjectReference: ..\..\src\QuickAuth.WebApi\QuickAuth.WebApi.csproj)
```

---

## 🧭 Comportamentos do código que impactam os testes (LEIA PRIMEIRO)

1. **Rotas reais (minúsculas):** os controllers usam `[Route("api/[controller]")]` e o
   `Program.cs` ativa `LowercaseUrls = true`. Portanto as rotas são
   **`POST /api/signin`** e **`GET /api/auth`** — não `/signin` nem `/auth`.

2. **O `AuthMiddleware` é GLOBAL.** Ele é registrado em `Program.cs` com
   `app.UseMiddleware<AuthMiddleware>()` **antes** dos controllers e **sem filtro de rota**.
   Consequência: **TODA** requisição (incluindo `POST /api/signin`) passa pelo middleware e
   **exige um token válido**. Sem token (ou com token inválido) a resposta é **401**, antes
   mesmo de chegar ao controller.
   - ➡️ Este spec reflete o **comportamento atual** (`/signin` exige token). Não altere o
     código de produção; escreva os testes assertando o que o sistema realmente faz hoje.

3. **`ITokenValidator.ValidateJwtToken` NÃO retorna `bool`.** A assinatura é
   `Task<ClaimsPrincipal> ValidateJwtToken(string token)`:
   - **Sucesso:** retorna um `ClaimsPrincipal` com as claims.
   - **Falha** (expirado, assinatura inválida, malformado): **lança `SecurityTokenException`**.
   - Ao mockar nos testes de middleware: configure para **retornar um `ClaimsPrincipal`**
     (caminho feliz) ou para **lançar `SecurityTokenException`** (caminho de erro).

4. **Parâmetros fixos de validação do JWT** (em `TokenValidator.cs`) — necessários para
   forjar um token **válido** nos testes:
   | Parâmetro            | Valor exigido            |
   |----------------------|--------------------------|
   | `Issuer`             | `test`                   |
   | `Audience`           | `test-app`               |
   | Header `typ`         | `JWT`                    |
   | Algoritmo            | `HmacSha256`             |
   | `SecretKey`          | de `JwtSettings.SecretKey` (config `JwtSettings:SecretKey`, 32 chars / 256 bits) |
   | `ValidateLifetime`   | `true` (expiração é checada) |

   Um token que não bata **exatamente** com issuer/audience/typ/alg/chave será rejeitado.

   **Receita para forjar um token válido nos testes** (o `TokenValidator` converte a chave com
   `Encoding.ASCII.GetBytes(SecretKey)` — `TokenValidator.cs:27`):
   - Use uma `SecretKey` com **≥ 32 caracteres** e **apenas ASCII** (sem acentos/caracteres
     especiais). Menos de 32 chars faz o `HmacSha256` lançar exceção **na criação** do token;
     acentos fazem os bytes divergirem entre assinatura e validação.
   - Assine o token forjado com **`Encoding.ASCII.GetBytes(secret)`** (mesmo encoding do validador).
     Para chars puramente ASCII, `UTF8` produziria os mesmos bytes — mas padronize em `ASCII`
     para nunca cair nessa armadilha.
   - Use o mesmo **Issuer `test`**, **Audience `test-app`** e algoritmo **`HmacSha256`** da tabela acima.

5. **Formato do erro do middleware:** ao barrar, o middleware responde
   `401` com `Content-Type: application/json` e corpo **`{ "error": "<mensagem>" }`**.

6. **`AuthController`** responde com **JSON** `{ "message": "Acesso Validado!" }`
   (`Content-Type: application/json`, propriedade em camelCase).

---

## 🧪 Cenários de Teste

Cobrir rigorosamente os critérios abaixo.

### 1. Unitário — `Domain/Extensions/StringExtensions.cs`
Classe `StringExtensions` com **extension method** sob teste: `static bool IsValidCpf(this string cpf)`.
(namespace real: `QuickAuth.WebApi.Domain.ValueOfObjects`; a pasta física é `Domain/Extensions/`)

- **Cenário 1:** Deve retornar `true` para um CPF matematicamente válido (testar com e sem máscara: `"529.982.247-25"` e `"52998224725"`).
- **Cenário 2:** Deve retornar `false` quando os dígitos verificadores estiverem errados.
- **Cenário 3:** Deve retornar `false` para string vazia, `null`, só espaços, ou com quantidade de dígitos ≠ 11 (menor e maior).
- **Cenário 4:** Deve retornar `false` para CPFs com todos os dígitos iguais (`"11111111111"`, `"00000000000"`).

### 2. Unitário — `Application/Services/Impl/TokenValidator.cs`
Injeções: `ILogger<TokenValidator>` e `IOptions<JwtSettings>`. Use a `SecretKey` real nos `IOptions` e
gere os tokens com `JsonWebTokenHandler`/`SecurityTokenDescriptor` respeitando a tabela do item 🧭 4.

- **Cenário 1:** Com um JWT íntegro e dentro da validade, deve **retornar um `ClaimsPrincipal`** e expor as Claims corretamente (ex.: asserir uma claim conhecida injetada na geração do token).
- **Cenário 2:** Com um JWT **expirado**, deve **lançar `SecurityTokenException`** (`Assert.ThrowsAsync<SecurityTokenException>`).
- **Cenário 3:** Com assinatura inválida (chave diferente) ou token malformado, deve **lançar `SecurityTokenException`**.

### 3. Integração (`WebApplicationFactory`) — `Middlewares/AuthMiddleware.cs`
Substitua o `ITokenValidator` por um mock no container de teste.

- **Cenário 1:** Com header `Authorization: Bearer <token>` e o mock configurado para **retornar um `ClaimsPrincipal`**, a requisição deve passar adiante e chegar ao endpoint (ex.: `GET /api/auth` → `200 OK`).
- **Cenário 2:** Sem o header `Authorization`, deve retornar **`401 Unauthorized`** com corpo `{ "error": "..." }`.
- **Cenário 3:** Com token presente porém o mock configurado para **lançar `SecurityTokenException`**, deve retornar **`401 Unauthorized`**.
- **Cenário 4 (borda):** Com `Authorization` presente mas sem o prefixo `"Bearer "`, deve retornar **`401 Unauthorized`**.

### 4. Integração/API — `Controllers/SignInController.cs` (`POST /api/signin`)
> 🧭 Lembre: o middleware global exige token. Para testar o comportamento do controller (200/400),
> forneça um `Authorization: Bearer <token>` que o mock do `ITokenValidator` aceite (retorne `ClaimsPrincipal`).

- **Cenário 1 (Sem token):** `POST /api/signin` sem `Authorization` deve retornar **`401 Unauthorized`** (barrado pelo middleware, não chega ao controller).
- **Cenário 2 (Sucesso):** Com token aceito + JSON `{ "cpf": "<cpf válido>" }`, deve retornar **`200 OK`** e corpo exatamente:
  ```json
  { "status": 200, "message": "Access authorized. Valid CPF." }
  ```
- **Cenário 3 (CPF Inválido):** Com token aceito + JSON `{ "cpf": "<cpf inválido>" }`, deve retornar **`400 BadRequest`** e corpo exatamente:
  ```json
  { "status": 400, "message": "Access Failed. Invalid CPF." }
  ```
- **Cenário 4 (Payload sem `cpf`):** Com token aceito + JSON `{}`, deve retornar **`400 BadRequest`** (validação de `[Required]` do `[ApiController]` → `ProblemDetails`/`ValidationProblem`).

> Observação de serialização: `SignInResponse.Status` é `HttpStatusCode` (enum) e serializa como número
> (`200`/`400`); as propriedades saem em camelCase (`status`, `message`).

### 5. Integração/API — `Controllers/AuthController.cs` (`GET /api/auth`)
- **Cenário 1:** `GET /api/auth` com token aceito pelo mock → passa pelo middleware e retorna **`200 OK`**
  com `Content-Type: application/json` e corpo exatamente:
  ```json
  { "message": "Acesso Validado!" }
  ```
- **Cenário 2:** `GET /api/auth` sem token, ou com token que faz o mock lançar `SecurityTokenException`, deve retornar **`401 Unauthorized`** (barrado pelo middleware).

---

## 🛠️ Requisitos de Código para os Testes

- Framework: **xUnit**.
- Mocks: **NSubstitute** (pacote já instalado).
- Asserções: **FluentAssertions** (v7.x — a v8+ exige licença paga para uso comercial).
- Nomenclatura: `NomeDoMetodo_Cenario_ComportamentoEsperado`
  (ex.: `IsValid_CpfComDigitosIguais_DeveRetornarFalso`, `ValidateJwtToken_TokenExpirado_DeveLancarSecurityTokenException`).
- Testes de Controller/Middleware: usar **`Microsoft.AspNetCore.Mvc.Testing`** (`WebApplicationFactory<Program>`)
  com servidor HTTP em memória.
  - Substituir o `ITokenValidator` real por mock via `WebApplicationFactory.WithWebHostBuilder` →
    `ConfigureTestServices`.
  - **`JwtSettings:SecretKey` NÃO precisa ser configurada no host de teste:** o único consumidor da
    chave é o `TokenValidator` real, que nos testes de integração é **substituído pelo mock**. No teste
    unitário do `TokenValidator` (grupo 2), a chave é passada via `Options.Create(new JwtSettings { ... })`
    diretamente em código — sem depender de configuração.
  - Para `WebApplicationFactory<Program>` funcionar, `Program` precisa ser acessível — já resolvido com
    `public partial class Program { }` no fim do `Program.cs`.
- Estrutura **AAA** (Arrange / Act / Assert) comentada em cada teste.
- Projeto de teste **já criado e referenciado**: `tests/QuickAuth.Tests/QuickAuth.Tests.csproj`
  referencia `QuickAuth.WebApi.csproj` e está incluído no `QuickAuth.sln`.
  Pacotes já instalados: xUnit, NSubstitute, FluentAssertions (7.x), Microsoft.AspNetCore.Mvc.Testing.
