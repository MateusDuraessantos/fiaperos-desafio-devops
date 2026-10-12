# Greenhouse API

API ASP.NET Core para gestao de estufas, sensores e alertas, com SQL Server.

## Execucao local com Docker Compose

Instale e inicie o Docker Desktop com containers Linux e Docker Compose 2.22.0
ou superior. Para executar via Docker, nao e necessario instalar o SDK .NET
ou SQL Server no computador.

Na raiz do projeto, execute no PowerShell:

```powershell
if (!(Test-Path .env)) { Copy-Item .env.example .env }
docker compose up --build --watch
```

O Compose aguarda o healthcheck do SQL Server antes de iniciar a API.
A API aplica as migrations automaticamente. Confira nos logs a mensagem
`Migrations aplicadas com sucesso.` Mantenha o terminal aberto: ao salvar
alteracoes na pasta `Greenhouse.API`, o Compose reconstroi a imagem e recria o
container da API automaticamente. Os dados do banco permanecem no volume.
Arquivos gerados em `bin` e `obj` nao disparam reconstrucoes.

O comando acima tambem verifica e reconstroi a imagem ao iniciar, se necessario. Aguarde a
reconstrucao terminar e atualize o navegador para visualizar as mudancas.
Use Ctrl+C para encerrar o acompanhamento e parar os containers.

No Prompt de Comando (CMD), a configuracao inicial equivalente e:

```cmd
if not exist .env copy .env.example .env
docker compose up --build --watch
```

Alteracoes em `.env` ou `compose.yaml` exigem reiniciar o comando para aplicar
a nova configuracao. O monitoramento automatico cobre os arquivos da API.

Abra http://localhost:8080/ para acessar o Swagger. Nele, selecione
`POST /api/auth/login`, clique em **Try it out** e depois em **Execute**, usando
os valores de ADMIN_USERNAME e ADMIN_PASSWORD do arquivo `.env`. Com os valores
de `.env.example`, o corpo da requisicao e:

```json
{
  "username": "admin",
  "password": "AdminLocal@2026!"
}
```

Copie o campo `token` retornado e informe `Bearer ` seguido desse token em **Authorize**.
Crie uma estufa antes de cadastrar sensores ou alertas.

## Comandos uteis

```powershell
# Estado dos servicos
docker compose ps

# Logs do banco e da API
docker compose logs -f db api

# Parar e remover containers, preservando os dados do banco
docker compose down

# Iniciar com atualizacao automatica durante o desenvolvimento
docker compose up --build --watch

# Iniciar em segundo plano, sem monitoramento de arquivos
docker compose up -d --build
```

Os dados ficam no volume `sql-data`. `docker compose down -v` apaga esse volume
e todos os dados do banco.

Os valores em `.env.example` sao apenas para desenvolvimento local. O arquivo
`.env` e ignorado pelo Git. Alterar SQL_PASSWORD depois da primeira inicializacao
nao altera automaticamente a senha armazenada no banco existente; mantenha os
valores consistentes ou altere a senha no SQL Server.

Se a porta 8080 estiver ocupada por uma execucao anterior, pare a aplicacao ou container
antes de iniciar o Compose.
