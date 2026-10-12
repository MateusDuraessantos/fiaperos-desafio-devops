# Greenhouse API

API para gestão de estufas, sensores e alertas. Desenvolvida com **ASP.NET Core/.NET 8, Entity Framework Core, SQL Server, JWT e Swagger**. Usa Docker Compose, GitHub Actions e Azure App Service.

## Executar localmente

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

## CI/CD e ambientes

| Branch | Destino |
|---|---|
| `dev` | `greenhouse-staging` |
| `main` | `greenhouse-production` |

O pipeline executa **restore → build → testes reais → docker build → publish → deploy**. Cada branch publica somente no seu ambiente. Os testes geram um relatório TRX; o pipeline rejeita uma execução sem testes. A imagem validada é `greenhouse-api:test`; o Azure recebe o pacote publicado da aplicação.

Os deploys usam os secrets `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING` e `AZURE_WEBAPP_PUBLISH_PROFILE_PRODUCTION`. O Swagger identifica LOCAL, STAGING ou PRODUÇÃO.

## Entrega

- [x] Código em `src/`, Dockerfile e `docker-compose.yml`.
- [x] API e banco com volume, variáveis e rede.
- [x] Testes automatizados e workflow por branch.
- [x] `.env.example` e exclusões de arquivos sensíveis.
- [x] [Documentação técnica](docs/ENTREGA.md), arquitetura, comandos e desafios.
- [x] Integrantes e RMs na documentação técnica.
- [x] [Prints e links reais](docs/evidencias/README.md) do pipeline, staging e produção.
- [x] Sete testes aprovados na cópia original e no ZIP extraído.
- [ ] Confirmar execução dos containers e persistência no ZIP final.
- [ ] Executar o workflow atualizado nas duas branches antes da entrega.

Ambientes publicados: [staging](https://greenhouse-staging-hdewbbffadheewd7.chilecentral-01.azurewebsites.net/index.html) e [produção](https://greenhouse-production-eefpdwcaaagce0a6.chilecentral-01.azurewebsites.net/index.html). Consulte as execuções no [GitHub Actions](https://github.com/MateusDuraessantos/fiaperos-desafio-devops/actions).

Para validar o pacote: `powershell -File scripts/Validar-Entrega.ps1`. O script gera um ZIP sem `.env` ou `bin/obj`, extrai em uma pasta limpa e valida Compose e build. Depois, execute o Compose nessa pasta e teste cadastro e consulta.

As credenciais do exemplo são apenas locais. No Azure, configure `ConnectionStrings__DefaultConnection`, `JwtSettings__SecretKey`, `AdminCredentials__Username` e `AdminCredentials__Password` em cada App Service. A API exige senha e chave JWT configuradas; segredos antigos usados no Azure devem ser substituídos.

`docker compose down` preserva o banco. `docker compose down -v` apaga os dados.
