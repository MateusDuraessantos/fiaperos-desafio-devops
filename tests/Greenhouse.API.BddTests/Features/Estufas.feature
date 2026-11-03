# language: pt
@bdd @ambiental
Funcionalidade: Gestão de estufas sustentáveis
  Para manter o cadastro das áreas de cultivo confiável
  Como gestor ambiental
  Quero cadastrar, consultar, atualizar e excluir estufas

  Contexto:
    Dado que estou autenticado

  Cenário: Cadastrar e consultar uma estufa com rastreabilidade
    Quando envio "POST" para "/api/estufas" com o JSON
      """
      {"nome":"Horta comunitária","localizacao":"São Paulo","area":120,"tipoCultivo":"Orgânico","ativa":true}
      """
    Então o status HTTP deve ser 201
    E a resposta deve respeitar o contrato "estufa"
    E o campo JSON "nome" deve ser '"Horta comunitária"'
    E o campo JSON "area" deve ser '120'
    E o campo JSON "ativa" deve ser 'true'
    E Location deve permitir consultar o mesmo recurso

  Cenário: Listar estufas com paginação
    Dado que existem 3 estufas cadastradas
    Quando envio "GET" para "/api/estufas?page=2&pageSize=2"
    Então o status HTTP deve ser 200
    E a resposta deve respeitar o contrato "estufas-page"
    E o campo JSON "total" deve ser '3'
    E o campo JSON "page" deve ser '2'
    E o campo JSON "pageSize" deve ser '2'
    E a coleção JSON "data" deve conter 1 item

  Cenário: Atualizar uma estufa e confirmar a persistência
    Dado que existe uma estufa cadastrada
    Quando envio "PUT" para "/api/estufas/{estufaId}" com o JSON
      """
      {"nome":"Cultivo atualizado","localizacao":"Campinas","area":250,"tipoCultivo":"Hidroponia","ativa":false}
      """
    Então o status HTTP deve ser 204
    E o corpo deve estar vazio
    Quando envio "GET" para "/api/estufas/{estufaId}"
    Então o status HTTP deve ser 200
    E a resposta deve respeitar o contrato "estufa"
    E o campo JSON "nome" deve ser '"Cultivo atualizado"'
    E o campo JSON "localizacao" deve ser '"Campinas"'
    E o campo JSON "area" deve ser '250'
    E o campo JSON "tipoCultivo" deve ser '"Hidroponia"'
    E o campo JSON "ativa" deve ser 'false'

  Cenário: Excluir a estufa e seus sensores e alertas vinculados
    Dado que existe uma estufa cadastrada
    E que existe um sensor do tipo "Temperatura"
    E que existe um alerta cadastrado
    Quando envio "DELETE" para "/api/estufas/{estufaId}"
    Então o status HTTP deve ser 204
    E o corpo deve estar vazio
    Quando envio "GET" para "/api/estufas/{estufaId}"
    Então o status HTTP deve ser 404
    E a resposta deve respeitar o contrato "message"
    E o campo JSON "message" deve ser '"Estufa não encontrada"'
    Quando envio "GET" para "/api/sensores/{sensorId}"
    Então o status HTTP deve ser 404
    E a resposta deve respeitar o contrato "message"
    Quando envio "GET" para "/api/alertas/{alertaId}"
    Então o status HTTP deve ser 404
    E a resposta deve respeitar o contrato "message"

  Esquema do Cenário: Recusar estufas com dados inválidos sem persistir
    Quando envio "POST" para "/api/estufas" com o JSON
      """
      {"nome":"<nome>","localizacao":"São Paulo","area":<area>,"tipoCultivo":"Orgânico"}
      """
    Então o status HTTP deve ser 400
    E a resposta deve respeitar o contrato "validation"
    E deve existir erro de validação para "<campo>"
    Quando envio "GET" para "/api/estufas"
    Então o status HTTP deve ser 200
    E a resposta deve respeitar o contrato "estufas-page"
    E o campo JSON "total" deve ser '0'
    Exemplos:
      | nome  | area   | campo |
      |       | 100    | Nome  |
      | Horta | 0      | Area  |
      | Horta | 100001 | Area  |

  Cenário: Recusar atualização inválida preservando os dados
    Dado que existe uma estufa cadastrada
    Quando envio "PUT" para "/api/estufas/{estufaId}" com o JSON
      """
      {"nome":"Alteração inválida","localizacao":"São Paulo","area":-1,"tipoCultivo":"Orgânico"}
      """
    Então o status HTTP deve ser 400
    E a resposta deve respeitar o contrato "validation"
    E deve existir erro de validação para "Area"
    Quando envio "GET" para "/api/estufas/{estufaId}"
    Então o status HTTP deve ser 200
    E a resposta deve respeitar o contrato "estufa"
    E o campo JSON "nome" deve ser '"Estufa BDD 1"'
    E o campo JSON "area" deve ser '100'

  Esquema do Cenário: Informar estufa inexistente
    Quando envio "<metodo>" para "/api/estufas/999999" com o JSON
      """
      {"nome":"Horta","localizacao":"São Paulo","area":100,"tipoCultivo":"Orgânico"}
      """
    Então o status HTTP deve ser 404
    E a resposta deve respeitar o contrato "message"
    E o campo JSON "message" deve ser '"Estufa não encontrada"'
    Exemplos:
      | metodo |
      | GET    |
      | PUT    |
      | DELETE |
