# language: pt
@bdd @ambiental
Funcionalidade: Monitoramento por sensores
  Para apoiar decisões sobre as condições de cultivo
  Como responsável pela estufa
  Quero consultar medições vinculadas a estufas existentes

  Contexto:
    Dado que estou autenticado

  Cenário: Cadastrar e consultar um sensor vinculado à estufa
    Dado que existe uma estufa cadastrada
    Quando envio "POST" para "/api/sensores" com o JSON
      """
      {"estufaId":{estufaId},"tipo":"Temperatura","valorAtual":24.5,"unidade":"C"}
      """
    Então o status HTTP deve ser 201
    E a resposta deve respeitar o contrato "sensor"
    E o campo JSON "estufaId" deve ser '{estufaId}'
    E o campo JSON "tipo" deve ser '"Temperatura"'
    E o campo JSON "valorAtual" deve ser '24.5'
    E o campo JSON "unidade" deve ser '"C"'
    E o campo JSON "dataLeitura" deve registrar o instante atual
    E Location deve permitir consultar o mesmo recurso

  Cenário: Filtrar sensores por tipo sem diferenciar maiúsculas
    Dado que existe uma estufa cadastrada
    E que existe um sensor do tipo "Temperatura"
    E que existe um sensor do tipo "Umidade"
    Quando envio "GET" para "/api/sensores?tipo=temperatura&page=1&pageSize=1"
    Então o status HTTP deve ser 200
    E a resposta deve respeitar o contrato "sensores-page"
    E o campo JSON "total" deve ser '1'
    E o campo JSON "pageSize" deve ser '1'
    E a coleção JSON "data" deve conter 1 item
    E o campo JSON "data.0.tipo" deve ser '"Temperatura"'

  Cenário: Consultar sensor inexistente
    Quando envio "GET" para "/api/sensores/999999"
    Então o status HTTP deve ser 404
    E a resposta deve respeitar o contrato "message"
    E o campo JSON "message" deve ser '"Sensor não encontrado"'

  Cenário: Impedir sensor associado a estufa inexistente
    Quando envio "POST" para "/api/sensores" com o JSON
      """
      {"estufaId":999999,"tipo":"Umidade","valorAtual":60,"unidade":"%"}
      """
    Então o status HTTP deve ser 400
    E a resposta deve respeitar o contrato "message"
    E o campo JSON "message" deve ser '"Estufa não encontrada"'
    Quando envio "GET" para "/api/sensores"
    Então o status HTTP deve ser 200
    E a resposta deve respeitar o contrato "sensores-page"
    E o campo JSON "total" deve ser '0'

  Cenário: Exigir o tipo do sensor
    Dado que existe uma estufa cadastrada
    Quando envio "POST" para "/api/sensores" com o JSON
      """
      {"estufaId":{estufaId},"tipo":"","valorAtual":60,"unidade":"%"}
      """
    Então o status HTTP deve ser 400
    E a resposta deve respeitar o contrato "validation"
    E deve existir erro de validação para "Tipo"
