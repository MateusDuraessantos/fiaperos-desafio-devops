# Especificação dos testes BDD

## Objetivo

Validar o comportamento das APIs da Greenhouse API com cenários Gherkin, testes HTTP e contratos JSON Schema. O projeto está relacionado aos pilares ambiental e governança de ESG: cadastro das áreas de cultivo, registros de monitoramento, rastreabilidade dos alertas e controle de acesso.

## Arquitetura

```text
Gherkin → Reqnroll → Bindings C# → HttpClient → ASP.NET Core TestServer
                                              ↓
                                   JWT / Controllers / EF Core
                                              ↓
                                   SQL Server ou SQLite
```

Reqnroll 3.3.4 executa os arquivos `.feature` em português com xUnit. Os testes usam a aplicação ASP.NET Core e o Entity Framework Core. No modo Docker e no CI, a persistência utiliza um serviço SQL Server 2022. O modo local utiliza SQLite relacional em memória, com chaves estrangeiras habilitadas.

Cada cenário utiliza sua própria instância da aplicação e seu próprio banco, permitindo execução independente. Os dados são criados pelas APIs. O cenário de indicadores históricos prepara leituras e estados históricos via EF e consulta o dashboard por HTTP.

## Funcionalidades e cenários

| Arquivo | Definições | Casos executáveis | Comportamentos |
|---|---:|---:|---|
| `Autenticacao.feature` | 5 | 21 | Login válido com uso do JWT; credenciais inválidas; usuário e senha obrigatórios; operações protegidas sem token; token inválido |
| `Estufas.feature` | 7 | 11 | Cadastro e consulta por Location; paginação; atualização persistida; exclusão em cascata; nome/área inválidos; atualização inválida sem alterar dados; recursos inexistentes |
| `Sensores.feature` | 5 | 5 | Cadastro e consulta; filtro por tipo; sensor inexistente; estufa inexistente; tipo obrigatório |
| `Alertas.feature` | 5 | 6 | Cadastro pendente com vínculo e data; paginação; recurso inexistente; estufa inexistente; mensagem e tipo obrigatórios |
| `Dashboard.feature` | 3 | 3 | Base vazia; consolidação dos registros; exclusão de inativos/resolvidos dos contadores e cálculo da média |
| **Total** | **25** | **46** | **Fluxos positivos e negativos** |

Os arquivos em `tests/Greenhouse.API.BddTests/Features/` constituem a especificação executável. `Contexto` autentica o usuário antes dos cenários das APIs de gestão. Cada linha de `Exemplos` representa um caso de teste. Os identificadores `{estufaId}`, `{sensorId}` e `{alertaId}` são obtidos nas respostas de criação.

## Matriz de cobertura das APIs

| Método | Endpoint | Sucesso | Falhas | Contratos |
|---|---|---|---|---|
| POST | `/api/auth/login` | 200 | 400, 401 | token, validation, message |
| GET | `/api/estufas` | 200 | 401 | estufas-page |
| POST | `/api/estufas` | 201 | 400, 401 | estufa, validation |
| GET | `/api/estufas/{id}` | 200 | 401, 404 | estufa, message |
| PUT | `/api/estufas/{id}` | 204 | 400, 401, 404 | corpo vazio, validation, message |
| DELETE | `/api/estufas/{id}` | 204 | 401, 404 | corpo vazio, message |
| GET | `/api/sensores` | 200 | 401 | sensores-page |
| POST | `/api/sensores` | 201 | 400, 401 | sensor, validation, message |
| GET | `/api/sensores/{id}` | 200 | 401, 404 | sensor, message |
| GET | `/api/alertas` | 200 | 401 | alertas-page |
| POST | `/api/alertas` | 201 | 400, 401 | alerta, validation, message |
| GET | `/api/alertas/{id}` | 200 | 401, 404 | alerta, message |
| GET | `/api/dashboard` | 200 | 401 | dashboard |

A verificação de cobertura compara as operações declaradas pelo OpenAPI da aplicação com as chamadas HTTP registradas durante os testes. Cada operação exige pelo menos uma resposta positiva validada.

## Validação das respostas

Os bindings verificam status code, campos JSON, valores retornados, vínculos entre entidades, paginação, datas e efeitos persistidos. Após uma criação, o teste consulta a URL de `Location` e compara o recurso. Após atualização ou exclusão, novas requisições verificam o estado persistido.

Os dez schemas em `tests/Greenhouse.API.BddTests/Schemas/` utilizam JSON Schema Draft 2020-12. Eles especificam propriedades obrigatórias, tipos, limites de valores, tamanhos, formatos de data/hora, conteúdo das coleções e propriedades adicionais. A validação de `date-time` está habilitada explicitamente.

O schema `validation` corresponde às respostas de validação do ASP.NET Core; `message` corresponde aos erros de negócio. As respostas 204 e os desafios JWT 401 sem conteúdo são validados por corpo vazio. Os desafios também exigem o cabeçalho Bearer. As credenciais inválidas no login geram JSON e são verificadas pelo respectivo contrato.

## Execução e resultados

O [README](../README.md) apresenta os comandos de execução com Docker/SQL Server e sem containers. O comando de execução completa realiza restore, build Release, testes unitários, testes BDD e verificação da cobertura.

Os resultados ficam em `test-results/sqlserver/` ou `test-results/sqlite/`. O relatório `bdd-report.html` permite consultar cenários e steps. Os arquivos TRX registram resultados e duração. O arquivo `bdd-messages.ndjson` utiliza o formato Cucumber Messages. Os resumos JSON/Markdown apresentam as contagens e operações cobertas. Os registros HTTP preservam respostas e asserções, com JWTs omitidos.

## Pipeline

O GitHub Actions executa as suítes com SQL Server antes do build Docker, publish e deploy. O job de validação exige testes aprovados e cobertura de todas as operações. Os relatórios são publicados como artefatos e a matriz é incluída no resumo da execução. As configurações completas estão em `.github/workflows/azure-webapps-dotnet-core.yml`.

## Referências técnicas

- [Reqnroll](https://docs.reqnroll.net/latest/)
- [Relatórios Reqnroll](https://docs.reqnroll.net/latest/reporting/reqnroll-formatters.html)
- [JSON Schema Draft 2020-12](https://json-schema.org/draft/2020-12)
- [ASP.NET Core: testes de integração](https://learn.microsoft.com/aspnet/core/test/integration-tests?view=aspnetcore-8.0)
