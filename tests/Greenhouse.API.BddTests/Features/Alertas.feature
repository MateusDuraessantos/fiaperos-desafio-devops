# language: pt
@bdd @governanca @ambiental
Funcionalidade: Rastreabilidade de alertas ambientais
  Para acompanhar riscos nas estufas
  Como gestor ambiental
  Quero registrar alertas com data e vínculo válidos

  Contexto:
    Dado que estou autenticado

  Cenário: Registrar e consultar alerta inicialmente pendente
    Dado que existe uma estufa cadastrada
    Quando envio "POST" para "/api/alertas" com o JSON
      """
      {"estufaId":{estufaId},"mensagem":"Umidade baixa: verificar irrigação","tipo":"Umidade"}
      """
    Então o status HTTP deve ser 201
    E a resposta deve respeitar o contrato "alerta"
    E o campo JSON "estufaId" deve ser '{estufaId}'
    E o campo JSON "mensagem" deve ser '"Umidade baixa: verificar irrigação"'
    E o campo JSON "tipo" deve ser '"Umidade"'
    E o campo JSON "resolvido" deve ser 'false'
    E o campo JSON "dataCriacao" deve registrar o instante atual
    E Location deve permitir consultar o mesmo recurso

  Cenário: Listar alertas com paginação
    Dado que existe uma estufa cadastrada
    E que existe um alerta cadastrado
    E que existe um alerta cadastrado
    Quando envio "GET" para "/api/alertas?page=2&pageSize=1"
    Então o status HTTP deve ser 200
    E a resposta deve respeitar o contrato "alertas-page"
    E o campo JSON "total" deve ser '2'
    E o campo JSON "page" deve ser '2'
    E o campo JSON "pageSize" deve ser '1'
    E a coleção JSON "data" deve conter 1 item
    E o campo JSON "data.0.resolvido" deve ser 'false'

  Cenário: Consultar alerta inexistente
    Quando envio "GET" para "/api/alertas/999999"
    Então o status HTTP deve ser 404
    E a resposta deve respeitar o contrato "message"
    E o campo JSON "message" deve ser '"Alerta não encontrado"'

  Cenário: Impedir alerta associado a estufa inexistente
    Quando envio "POST" para "/api/alertas" com o JSON
      """
      {"estufaId":999999,"mensagem":"Temperatura elevada","tipo":"Temperatura"}
      """
    Então o status HTTP deve ser 400
    E a resposta deve respeitar o contrato "message"
    E o campo JSON "message" deve ser '"Estufa não encontrada"'
    Quando envio "GET" para "/api/alertas"
    Então o status HTTP deve ser 200
    E a resposta deve respeitar o contrato "alertas-page"
    E o campo JSON "total" deve ser '0'

  Esquema do Cenário: Exigir mensagem e tipo no alerta
    Dado que existe uma estufa cadastrada
    Quando envio "POST" para "/api/alertas" com o JSON
      """
      {"estufaId":{estufaId},"mensagem":"<mensagem>","tipo":"<tipo>"}
      """
    Então o status HTTP deve ser 400
    E a resposta deve respeitar o contrato "validation"
    E deve existir erro de validação para "<campo>"
    Exemplos:
      | mensagem   | tipo    | campo    |
      |            | Umidade | Mensagem |
      | Verificar  |         | Tipo     |
