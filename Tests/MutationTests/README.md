# Mutation Tests (Stryker.NET)

Testes de mutação rodam contra o projeto de testes unitários em `../UnitTests`.

## Pré-requisitos

Instalar o Stryker como ferramenta local na primeira vez:

```bash
dotnet new tool-manifest --force
dotnet tool install dotnet-stryker
```

Já existe um `.config/dotnet-tools.json` na raiz do repositório com o Stryker pinado — basta executar:

```bash
dotnet tool restore
```

## Executar

A partir desta pasta (`Tests/MutationTests`):

```bash
dotnet stryker
```

A configuração em `stryker-config.json` aponta para a solution, o projeto de produção (`Core.API`) e o projeto de testes unitários. O relatório HTML é gerado em `StrykerOutput/`.
