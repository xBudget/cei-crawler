# xBudget CEI Crawler


[![Build Status](https://travis-ci.com/xBudget/cei-crawler.svg?token=1iSZTdpmsYpLXbyppWrQ&branch=master)](https://travis-ci.com/xBudget/cei-crawler)
[![codecov](https://codecov.io/gh/xBudget/cei-crawler/branch/master/graph/badge.svg)](https://codecov.io/gh/xBudget/cei-crawler)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=xBudget_cei-crawler&metric=alert_status)](https://sonarcloud.io/dashboard?id=xBudget_cei-crawler)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=xBudget_cei-crawler&metric=sqale_rating)](https://sonarcloud.io/dashboard?id=xBudget_cei-crawler)
![Nuget](https://img.shields.io/nuget/v/xBudget.CeiCrawler)


## Descrição
xBudget CEI Crawler é um projeto que extrai informações de investimentos na bolsa de valores do Brasil [B3](http://www.b3.com.br/pt_br/) através do [CEI](https://cei.b3.com.br/). A ideia do projeto é oferecer para o usuário uma forma de automatizar o acompanhamento de seus investimentos de uma forma automatizada.
Os dados extraídos do CEI são de total responsabilidade da plataforma, o CEI Crawler apenas consulta os dados disponíveis no site.

## Como Instalar
xBudget CEI Crawler é um projeto escrito em dotnet core atualmente na versão 3.1. A DLL está disponível para instalação através do [nuget.org](https://www.nuget.org/packages/xBudget.CeiCrawler/). 

```
dotnet add package xBudget.CeiCrawler
```

## Métodos disponíveis

```c#
GetOperations(DateTime? startDate = null, DateTime? endDate = null)
```

```c#
GetWallet(DateTime? date = null)
```

## Alternativas em outras linguagens
- [CEI Crawler escrito em Javascript](https://github.com/Menighin/cei-crawler)

## Licença
MIT
