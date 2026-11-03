# language: pt
@bdd @governanca
Funcionalidade: Autenticação e proteção das APIs
  Para proteger os dados do monitoramento ambiental
  Como responsável pelas estufas
  Quero que somente usuários autenticados acessem a gestão

  Cenário: Autenticar com credenciais válidas e usar o token emitido
    Quando envio "POST" para "/api/auth/login" com o JSON
      """
      {"username":"bdd-admin","password":"BddOnly@2026!"}
      """
    Então o status HTTP deve ser 200
    E a resposta deve respeitar o contrato "token"
    E o token deve identificar o administrador e expirar em uma hora
    Quando uso o token recebido
    E envio "GET" para "/api/dashboard"
    Então o status HTTP deve ser 200
    E a resposta deve respeitar o contrato "dashboard"

  Esquema do Cenário: Recusar credenciais incorretas
    Quando envio "POST" para "/api/auth/login" com o JSON
      """
      {"username":"<usuario>","password":"<senha>"}
      """
    Então o status HTTP deve ser 401
    E a resposta deve respeitar o contrato "message"
    E o campo JSON "message" deve ser '"Credenciais inválidas"'
    Exemplos:
      | usuario    | senha         |
      | bdd-admin  | incorreta     |
      | inexistente| BddOnly@2026! |

  Esquema do Cenário: Exigir usuário e senha no login
    Quando envio "POST" para "/api/auth/login" com o JSON
      """
      {"username":"<usuario>","password":"<senha>"}
      """
    Então o status HTTP deve ser 400
    E a resposta deve respeitar o contrato "validation"
    E deve existir erro de validação para "<campo>"
    Exemplos:
      | usuario   | senha         | campo    |
      |           | BddOnly@2026! | Username |
      | bdd-admin |               | Password |

  Esquema do Cenário: Bloquear todas as operações protegidas sem token
    Quando envio "<metodo>" para "<rota>" com o JSON
      """
      {}
      """
    Então o status HTTP deve ser 401
    E o corpo deve estar vazio
    E deve haver um desafio Bearer
    Exemplos:
      | metodo | rota              |
      | GET    | /api/estufas      |
      | GET    | /api/estufas/1    |
      | POST   | /api/estufas      |
      | PUT    | /api/estufas/1    |
      | DELETE | /api/estufas/1    |
      | GET    | /api/sensores     |
      | GET    | /api/sensores/1   |
      | POST   | /api/sensores     |
      | GET    | /api/alertas      |
      | GET    | /api/alertas/1    |
      | POST   | /api/alertas      |
      | GET    | /api/dashboard    |

  Esquema do Cenário: Bloquear token inválido nas APIs de gestão
    Dado que utilizo um token inválido
    Quando envio "GET" para "<rota>"
    Então o status HTTP deve ser 401
    E o corpo deve estar vazio
    E deve haver um desafio Bearer
    Exemplos:
      | rota           |
      | /api/estufas   |
      | /api/sensores  |
      | /api/alertas   |
      | /api/dashboard |
