# Projeto - Cidades ESGInteligentes

## Greenhouse API

API para gestão de estufas, sensores e alertas. Desenvolvida com **ASP.NET Core/.NET 8, Entity Framework Core, SQL Server, JWT e Swagger**. Usa Docker Compose, GitHub Actions e Azure App Service.

## Como executar localmente com Docker

Com o Docker Desktop aberto em modo Linux e Compose 2.22 ou superior, execute na raiz:

```cmd
if not exist .env copy .env.example .env
docker compose up --build --watch
```

No PowerShell, crie o arquivo com `if (!(Test-Path .env)) { Copy-Item .env.example .env }`.

Aguarde `Migrations aplicadas com sucesso.` e abra **[http://localhost:8080/](http://localhost:8080/)**. Mantenha o terminal aberto: alterações em `src/Greenhouse.API` reconstroem a API automaticamente. Atualize o navegador depois do build. `Ctrl+C` para a execução.

O Compose inicia a API e o SQL Server na mesma rede, aguarda o banco ficar pronto e persiste os dados em um volume. O Dockerfile separa a compilação com SDK 8 da execução com runtime ASP.NET 8. Alterações no `.env` ou `docker-compose.yml` exigem reiniciar o comando.

## Testar no Swagger

Execute `POST /api/auth/login` com os valores do `.env`. Com o exemplo local:

```json
{
  "username": "admin",
  "password": "AdminLocal@2026!"
}
```

Copie o campo `token` e informe `Bearer SEU_TOKEN` em **Authorize**. Cadastre uma estufa, consulte seu ID e use-o para criar sensores e alertas. `/api/dashboard` apresenta os indicadores.

O projeto ainda não se conecta a sensores reais nem atualiza leituras automaticamente.

## Pipeline CI/CD

O workflow [azure-webapps-dotnet-core.yml](.github/workflows/azure-webapps-dotnet-core.yml) usa GitHub Actions em um runner Ubuntu, o SDK .NET 8, Docker e Azure App Service. Ele é acionado por push nas branches `dev` e `main`, inclusive após merges, ou manualmente por `workflow_dispatch`.

| Branch | Destino |
|---|---|
| `dev` | `greenhouse-staging` |
| `main` | `greenhouse-production` |

O pipeline executa **restore → build → testes reais → docker build → publish → deploy**. Cada branch publica somente no seu ambiente. Os testes geram um relatório TRX; o pipeline rejeita uma execução sem testes. A imagem validada é `greenhouse-api:test`; o Azure recebe o pacote publicado da aplicação.

Os deploys usam os secrets `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING` e `AZURE_WEBAPP_PUBLISH_PROFILE_PRODUCTION`. O Swagger identifica LOCAL, STAGING ou PRODUÇÃO.

O job de build salva o relatório dos testes como artefato mesmo em caso de falha. Após validar a imagem Docker, publica a API, grava a identificação do ambiente e salva o pacote `greenhouse-api`. Os jobs de deploy dependem do sucesso do build e baixam esse pacote. Staging e produção têm jobs independentes, selecionados pela branch. Falhas de build, testes, imagem ou publicação impedem o deploy. Não há publicação de imagem em registry.

## Containerização

Conteúdo de [src/Greenhouse.API/Dockerfile](src/Greenhouse.API/Dockerfile):

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Greenhouse.API.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "Greenhouse.API.dll"]
```

O build usa dois estágios: o SDK compila a aplicação e a imagem final contém o runtime ASP.NET e os arquivos publicados. Copiar o `.csproj` antes do restante do código permite reutilizar o cache de dependências quando elas não mudam. O `.dockerignore` exclui binários, arquivos intermediários e configurações da IDE do contexto de build.

O [docker-compose.yml](docker-compose.yml) executa a API e o SQL Server 2022 na rede bridge `greenhouse`. O banco fica acessível apenas pela rede dos containers e usa o volume `sql-data` para persistência. O healthcheck executa `SELECT 1`, e a API aguarda o banco ficar saudável por meio de `depends_on`. Ao iniciar, a aplicação tenta aplicar as migrations do Entity Framework Core.

A API atende em `127.0.0.1:8080`, e as credenciais são recebidas por variáveis do `.env`. O Compose Watch reconstrói a imagem após alterações no código, ignorando `bin/obj`. Para encerrar sem apagar o banco, execute `docker compose down`; a opção `-v` também remove o volume e seus dados.

## Prints do funcionamento

As evidências disponíveis incluem execução local, deploys e acesso ao Swagger em staging e produção. Os detalhes estão em [docs/evidencias/README.md](docs/evidencias/README.md).

| Evidência | Link |
|---|---|
| Execução local com .NET | [Print local](docs/evidencias/local.png) |
| Deploy de `dev` para staging | [Execução do pipeline](https://github.com/MateusDuraessantos/fiaperos-desafio-devops/actions/runs/38305665032) |
| Deploy de `main` para produção | [Execução do pipeline](https://github.com/MateusDuraessantos/fiaperos-desafio-devops/actions/runs/38305636147) |
| Aplicação em staging | [Swagger staging](https://greenhouse-staging-hdewbbffadheewd7.chilecentral-01.azurewebsites.net/index.html) |
| Aplicação em produção | [Swagger produção](https://greenhouse-production-eefpdwcaaagce0a6.chilecentral-01.azurewebsites.net/index.html) |

### Staging

![Swagger em staging](docs/evidencias/staging.png)

![Pipeline dev com deploy em staging](docs/evidencias/pipeline-dev.png)

### Produção

![Swagger em produção](docs/evidencias/producao.png)

![Pipeline main com deploy em produção](docs/evidencias/pipeline-main.png)

As capturas registradas em 12/10/2026 representam uma versão anterior às alterações locais atuais. É necessário atualizar as evidências após executar o workflow atualizado nas duas branches. O print local foi obtido com .NET, sem SQL Server. Em 13/10/2026, uma cópia extraída do ZIP foi validada com Docker e SQL Server: build da imagem, Swagger HTTP 200, login, cadastro, consulta e persistência após remover e recriar os containers, preservando o volume. Os links de staging e produção também retornaram HTTP 200 nessa revisão.

## Tecnologias utilizadas

- C# e ASP.NET Core/.NET 8: API HTTP e controllers.
- Entity Framework Core 8: acesso aos dados e migrations.
- SQL Server 2022: banco de dados relacional.
- JWT Bearer: autenticação e autorização.
- Swagger/OpenAPI com Swashbuckle: documentação interativa da API.
- Docker e Docker Compose: imagens, rede, volume e execução local.
- Git e GitHub Actions: versionamento e pipeline CI/CD.
- Azure App Service: hospedagem em staging e produção.
- xUnit e Microsoft.NET.Test.Sdk: testes automatizados.
- Postman: coleção de requisições para testes manuais.
- PowerShell: script de preparação e validação do pacote de entrega.

## Entrega

- [x] Código em `src/`, Dockerfile e `docker-compose.yml`.
- [x] API e banco com volume, variáveis e rede.
- [x] Testes automatizados e workflow por branch.
- [x] `.env.example` e exclusões de arquivos sensíveis.
- [x] [Documentação técnica](docs/ENTREGA.md), arquitetura, comandos e desafios.
- [x] Integrantes e RMs na documentação técnica.
- [x] [Prints e links reais](docs/evidencias/README.md) do pipeline, staging e produção.
- [x] Sete testes aprovados na cópia original e no ZIP extraído.
- [x] Confirmar execução dos containers e persistência na cópia extraída do ZIP.
- [ ] Executar o workflow atualizado nas duas branches antes da entrega.

Ambientes publicados: [staging](https://greenhouse-staging-hdewbbffadheewd7.chilecentral-01.azurewebsites.net/index.html) e [produção](https://greenhouse-production-eefpdwcaaagce0a6.chilecentral-01.azurewebsites.net/index.html). Consulte as execuções no [GitHub Actions](https://github.com/MateusDuraessantos/fiaperos-desafio-devops/actions).

Para validar o pacote: `powershell -File scripts/Validar-Entrega.ps1`. O script gera um ZIP sem `.env` ou `bin/obj`, extrai em uma pasta limpa e valida Compose e build. Depois, execute o Compose nessa pasta e teste cadastro e consulta.

As credenciais do exemplo são apenas locais. No Azure, configure `ConnectionStrings__DefaultConnection`, `JwtSettings__SecretKey`, `AdminCredentials__Username` e `AdminCredentials__Password` em cada App Service. A API exige senha e chave JWT configuradas; segredos antigos usados no Azure devem ser substituídos.

`docker compose down` preserva o banco. `docker compose down -v` apaga os dados.
