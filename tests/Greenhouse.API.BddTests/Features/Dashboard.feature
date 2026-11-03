# language: pt
@bdd @governanca
Funcionalidade: Indicadores para decisões ambientais
  Para acompanhar a operação das estufas
  Como gestor
  Quero indicadores coerentes com os dados registrados

  Contexto:
    Dado que estou autenticado

  Cenário: Apresentar indicadores zerados sem cadastros
    Quando envio "GET" para "/api/dashboard"
    Então o status HTTP deve ser 200
    E a resposta deve respeitar o contrato "dashboard"
    E o campo JSON "estufasAtivas" deve ser '0'
    E o campo JSON "sensoresOnline" deve ser '0'
    E o campo JSON "alertasPendentes" deve ser '0'
    E o campo JSON "mediaTemperatura" deve ser '0'

  Cenário: Consolidar cadastros de estufas sensores e alertas
    Dado que existem 2 estufas cadastradas
    E que existe um sensor do tipo "Temperatura"
    E que existe um sensor do tipo "Umidade"
    E que existe um alerta cadastrado
    Quando envio "GET" para "/api/dashboard"
    Então o status HTTP deve ser 200
    E a resposta deve respeitar o contrato "dashboard"
    E o campo JSON "estufasAtivas" deve ser '2'
    E o campo JSON "sensoresOnline" deve ser '2'
    E o campo JSON "alertasPendentes" deve ser '1'
    E o campo JSON "mediaTemperatura" deve ser '0'

  Cenário: Desconsiderar estufa inativa e alerta resolvido e calcular a média
    Dado que existe uma estufa cadastrada
    E que existe um alerta cadastrado
    E que há dados históricos com estufa inativa alerta resolvido e temperaturas 20 e 25.2
    Quando envio "GET" para "/api/dashboard"
    Então o status HTTP deve ser 200
    E a resposta deve respeitar o contrato "dashboard"
    E o campo JSON "estufasAtivas" deve ser '0'
    E o campo JSON "sensoresOnline" deve ser '0'
    E o campo JSON "alertasPendentes" deve ser '0'
    E o campo JSON "mediaTemperatura" deve ser '22.6'
