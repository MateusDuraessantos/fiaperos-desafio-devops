# Greenhouse API - Testes Automatizados com BDD

Projeto acadêmico para gestão de estufas, sensores e alertas ambientais. A aplicação utiliza C#/.NET 8, ASP.NET Core, Entity Framework Core, SQL Server e autenticação JWT. A suíte BDD valida as funcionalidades das cinco APIs com Gherkin em português, Reqnroll e xUnit.

## Integrantes

| Integrante | RM |
|---|---|
| Mateus Durães dos Santos | 565195 |
| Gabrielle Correa Leme | 561176 |
| Jonatas Henrique da Silva Capelati | 565732 |
| Vinicius Tarifa Fabiano | 565314 |
| Israel da Silva Capelati | 565730 |

## Estrutura

```text
.github/workflows/          Pipeline GitHub Actions
src/Greenhouse.API/         Aplicação ASP.NET Core e Dockerfile
tests/Greenhouse.API.Tests/ Testes unitários
tests/Greenhouse.API.BddTests/
  Features/                Cenários Gherkin
  Steps/                   Bindings e asserções HTTP/JSON
  Support/                 Configuração da API e validação de contratos
  Schemas/                 Contratos JSON Schema
tests/Dockerfile           Imagem de execução dos testes
scripts/                   Execução e empacotamento
docs/BDD.md                Especificação técnica dos testes
Greenhouse.sln             Solução .NET
docker-compose.yml         API e SQL Server
docker-compose.bdd.yml     Testes BDD e SQL Server isolado
```

## Cobertura dos testes

São 25 definições de cenário em cinco funcionalidades. Os esquemas de cenário e suas tabelas de exemplos geram **46 casos BDD**, incluindo caminhos positivos e negativos. A solução também contém **7 testes unitários** de autenticação e validação de estufas.

| Funcionalidade | Casos BDD | Comportamentos |
|---|---:|---|
| Autenticação | 21 | Login, campos obrigatórios e proteção JWT |
| Estufas | 11 | Cadastro, consulta, paginação, atualização, exclusão e validação |
| Sensores | 5 | Cadastro, consulta, filtros e integridade do vínculo |
| Alertas | 6 | Cadastro, consulta, paginação e validação |
| Dashboard | 3 | Contadores, estados dos registros e média de temperatura |

As **13 operações HTTP** são verificadas por status code, conteúdo da resposta e contratos JSON Schema Draft 2020-12. As respostas sem conteúdo são verificadas como corpo vazio. A matriz completa está em [docs/BDD.md](docs/BDD.md).

Os cenários exercitam a aplicação em ASP.NET Core TestServer, com autenticação, controllers, middleware e persistência. Cada cenário possui banco isolado. O modo Docker e o CI utilizam SQL Server 2022; a execução local sem containers utiliza SQLite em memória.

## Executar os testes com Docker e SQL Server

Pré-requisitos: Docker Desktop em modo Linux e Docker Compose.

Na raiz do projeto:

```sh
docker compose -p greenhouse-bdd -f docker-compose.bdd.yml up --build --abort-on-container-exit --exit-code-from bdd
```

O Compose cria o SQL Server, aguarda o healthcheck, compila a solução e executa os testes. O código de saída é o código retornado pelo serviço `bdd`: zero indica aprovação. As credenciais do banco estão limitadas ao ambiente descartável de testes. Esse banco não compartilha volumes nem portas com a aplicação.

Os resultados são gravados em `test-results/sqlserver/`:

| Arquivo | Conteúdo |
|---|---|
| `bdd-report.html` | Relatório interativo de funcionalidades, cenários e steps |
| `bdd.trx` e `unit.trx` | Resultados das suítes, nomes e tempos dos testes |
| `bdd-messages.ndjson` | Eventos no formato Cucumber Messages |
| `summary.json` e `summary.md` | Contagens e cobertura das operações |
| `http/` | Requisições e respostas com status e contrato validados |

Abra `test-results/sqlserver/bdd-report.html` no navegador. Para encerrar e remover os containers de teste:

```sh
docker compose -p greenhouse-bdd -f docker-compose.bdd.yml down
```

## Executar os testes sem containers

Pré-requisitos: SDK .NET 8.0.4xx, PowerShell e acesso ao NuGet no primeiro restore.

```sh
dotnet test Greenhouse.sln -c Release --logger "console;verbosity=detailed"
```

Para executar as suítes e verificar também a cobertura de todas as operações HTTP:

```powershell
powershell -File scripts/Testar-Bdd.ps1
```

O script restaura dependências, compila a solução em Release, executa as duas suítes e verifica a cobertura. Os resultados ficam em `test-results/sqlite/`. O arquivo `global.json` seleciona o SDK .NET 8. Não é necessário configurar `.env` nesse modo.

Para uma instância SQL Server de testes já disponível, configure `BDD_SQLSERVER_CONNECTION` e execute:

```sh
powershell -File scripts/Testar-Bdd.ps1 -Provedor sqlserver
```

A conexão deve permitir criar e excluir bancos. Cada cenário utiliza um banco com nome aleatório `GreenhouseBdd_*`, removido ao término.

## Executar a aplicação

Crie o arquivo de configuração local:

```powershell
Copy-Item .env.example .env
```

Inicie a API e o banco:

```sh
docker compose -p greenhouse-app up -d --build
```

A documentação Swagger fica em [http://localhost:8080](http://localhost:8080). Execute `POST /api/auth/login` com `ADMIN_USERNAME` e `ADMIN_PASSWORD` definidos no `.env`. Informe o token retornado em **Authorize**, no formato `Bearer TOKEN`.

```sh
docker compose -p greenhouse-app ps
docker compose -p greenhouse-app logs api
docker compose -p greenhouse-app down
```

O volume `sql-data` conserva os dados entre execuções. O `.env` contém a configuração local e não integra o pacote de código-fonte.

## CI/CD

O workflow [.github/workflows/azure-webapps-dotnet-core.yml](.github/workflows/azure-webapps-dotnet-core.yml) possui os seguintes gatilhos: push e pull request para `dev`/`main` e execução manual.

```text
Restore → Build → Unitários e BDD com SQL Server → Cobertura → Docker build → Publish → Deploy
```

O job utiliza SQL Server 2022 como serviço. A aprovação exige suítes executadas sem falhas ou testes ignorados, pelo menos três casos BDD e teste HTTP positivo para cada operação do OpenAPI. Cada resposta deve ter status e contrato JSON Schema, ou corpo vazio, verificados.

O passo **Executar cenários BDD em Gherkin e validar as APIs** apresenta o nome de cada funcionalidade e cenário, os passos Dado/Quando/Então, os resultados HTTP e a validação dos contratos. A saída detalhada dos testes é produzida pelo runner .NET/Reqnroll.

Os relatórios são publicados no artefato `test-results`, inclusive em caso de falha. A matriz de cobertura aparece no resumo da execução. Os jobs de deploy dependem da aprovação do build e são executados fora de pull requests.

| Branch | App Service | Secret de publicação |
|---|---|---|
| `dev` | `greenhouse-staging` | `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING` |
| `main` | `greenhouse-production` | `AZURE_WEBAPP_PUBLISH_PROFILE_PRODUCTION` |

No Azure, a aplicação recebe `ConnectionStrings__DefaultConnection`, `JwtSettings__SecretKey`, `AdminCredentials__Username` e `AdminCredentials__Password` pelas configurações do App Service.

## Gerar o pacote de código-fonte

```powershell
powershell -File scripts/Validar-Entrega.ps1
```

O comando gera `artifacts/greenhouse-bdd-entrega.zip`, extrai uma cópia e executa os testes locais nessa cópia. Para incluir a validação com SQL Server e containers:

```powershell
powershell -File scripts/Validar-Entrega.ps1 -ComDocker
```

O ZIP contém os fontes da aplicação e dos testes, arquivos Gherkin, contratos, Dockerfiles, configurações, scripts de execução e README. Arquivos `.env`, binários, caches e relatórios gerados são excluídos. A documentação com os cenários e as capturas de execução é entregue separadamente em PDF.
