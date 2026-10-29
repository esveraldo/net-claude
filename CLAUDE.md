# CLAUDE.md

## Projeto

MasterNet é uma API backend em ASP.NET Core (.NET 10) para gestão de cursos online (cursos, instrutores, preços, ratings, fotos, contas de usuário). Usa Clean/Onion Architecture com CQRS em vertical slices e substitui MediatR e AutoMapper por implementações próprias (`libs/Core.MediatOR`, `libs/Core.Mappy`) em vez dos pacotes NuGet, com fins didáticos.

Leia `ARCHITECTURE.md` para o detalhamento completo (diagrama de camadas, sequência de uma requisição, decisões de design, dívida técnica). Não o duplique aqui.

## Comandos

```bash
# Build
dotnet build MasterNet.sln

# Executar a API (Scalar serve como documentação interativa / durante Development)
dotnet run --project src/MasterNet.WebApi

# Migrações EF Core (Executa desde a raiz do repositorio)
dotnet ef migrations add <Nombre> --project src/MasterNet.Persistence --startup-project src/MasterNet.WebApi
dotnet ef database update --project src/MasterNet.Persistence --startup-project src/MasterNet.WebApi

# SQL Server local
docker compose up -d
```

Ainda não existe nenhum teste em `tests/MasterNet.Application.UnitTests` (apenas a pasta `Cursos/` vazia, com xUnit + Moq referenciados). Se você adicionar testes, coloque-os ali e execute-os com `dotnet test MasterNet.sln`.

### Provedor de banco de dados

`MasterNetDbContext` usa `UseSqlServer` com a connection string `SqlServerDatabase` (`src/MasterNet.Persistence/DependencyInjection.cs`). Há uma linha `UseSqlite` comentada e uma connection string `SqliteDatabase` como alternativa local rápida — não a ative sem que o usuário peça. Os arquivos `LocalDatabase.db*` em `src/MasterNet.WebApi/` são resquícios de quando se usava SQLite; o SQL Server (via `docker-compose.yml`) é o provedor ativo.

### Configuração local e segredos

Este repositório **não usa .NET User Secrets**. A configuração local privada de `MasterNet.WebApi` (p. ex. `TokenKey`, `ConnectionStrings:SqlServerDatabase`) fica em `src/MasterNet.WebApi/appsettings.Local.json`, que está no `.gitignore` e só é carregado quando `ASPNETCORE_ENVIRONMENT=Development` (ordem de configuração em `Program.cs`: `appsettings.json` → `appsettings.{Environment}.json` → `appsettings.Local.json` em Development → variáveis de ambiente). Use `src/MasterNet.WebApi/appsettings.Local.example.json` como modelo. Não adicione `UserSecretsId`, `AddUserSecrets` nem nada relacionado a `secrets.json`.


## Git e commits

- Utilize **Conventional Commits**.
- Mantenha o assunto do commit conciso e descritivo.
- Adicione um corpo quando o motivo da mudança não for evidente.
- Explique por que a mudança foi feita, não apenas o que mudou.
- Nunca faça `push` das mudanças, a menos que seja solicitado explicitamente.
- Não faça `commit` automaticamente, a menos que o usuário solicite.
- Antes de criar um commit, revise as mudanças incluídas para evitar arquivos não relacionados.
- Escreva as mensagens de commit em português.


## Arquitetura

As dependências apontam para dentro (Domain não depende de nada; WebApi depende de tudo):

```
MasterNet.Domain          → entidades, IGenericRepository/IUnitOfWork, constantes de seguridad — sin dependencias
MasterNet.Persistence     → MasterNetDbContext (IdentityDbContext<AppUser>), configuraciones/migraciones EF, GenericRepository/UnitOfWork, seed con Bogus+JSON — depende de Domain
MasterNet.Application     → casos de uso CQRS, patrón Result<T>, ValidationBehavior — depende de Domain, Persistence y libs/*
MasterNet.Infrastructure  → Cloudinary (fotos), servicios de token/usuario JWT, reportes CSV — depende de Application
MasterNet.WebApi          → Controllers, Program.cs (composition root), middleware de excepciones — depende de Application + Infrastructure
libs/Core.MediatOR        → mediator propio (IRequest/IRequestHandler/IPipelineBehavior), resuelto vía DI + reflection
libs/Core.Mappy           → mapper propio (IMapper/ProjectTo/IMappingProfile)
```

`Application` referencia `Persistence` diretamente, não apenas interfaces de Domain. Isso é intencional, não um descuido — habilita o split CQRS pragmático:
- **Escritas** → `IUnitOfWork.Repository<T>()` (repositório enxuto: `GetByIdAsync`/`AddAsync`).
- **Leituras complexas** (paginação, filtros, projeções) → injete `MasterNetDbContext` diretamente e use LINQ + `ProjectTo<TDto>()`, sem passar pelo repositório genérico.

Não introduza um repositório genérico para queries de leitura complexas: siga o padrão existente e injete `MasterNetDbContext` diretamente no handler da query.

### Vertical slices

Cada caso de uso vive em `src/MasterNet.Application/<Feature>/<CasoDeUso>/` em um único arquivo, dentro de uma classe contêiner (`public class`) que aninha: o record `IRequest<Result<T>>`, seu `IRequestHandler<,>` e seu `IValidator<T>`. Exemplo real: `Courses/CourseCreate/CreateCourseCommand.cs` contém `CreateCourseCommandRequest`, `CreateCourseCommandHandler` e `CreateCourseCommandRequestValidator`.

Siga esse mesmo layout de um arquivo por caso de uso ao adicionar novas features — não separe Commands, Handlers e Validators em pastas por tipo técnico.

Os validadores são detectados automaticamente: `ValidationBehavior<,>` no pipeline do mediator procura um `IValidator<TRequest>` registrado e o executa antes do handler. Não o invoque manualmente a partir do handler nem do controller.

Os erros de fluxos de negócio esperados (validação, "não encontrado" etc.) são devolvidos como `Result<T>.Failure(...)`, nunca como exceções. `ExceptionMiddleware` captura apenas falhas verdadeiramente inesperadas e devolve um JSON 500 uniforme. Nos controllers, traduza `Result<T>` para HTTP com `this.FromResult(result)` (`WebApi/Extensions/ResultExtensions.cs`) — não construa respostas de erro manualmente nos controllers.

Não use bibliotecas de mapeamento automático (AutoMapper, Mapster) nem de mediator (MediatR). Use `Core.Mappy` (`IMapper`/`ProjectTo`/`IMappingProfile`) e `Core.MediatOR` (`IRequest`/`IRequestHandler`/`IPipelineBehavior`), que são as implementações próprias do projeto.

### Autorização

Baseada em claims, não em roles: os roles (`ADMIN`, `CLIENT`, em `Domain/Security/CustomRoles.cs`) são apenas agrupamentos de claims `POLICIES` granulares (`COURSE_WRITE`, `COURSE_READ`, `RATING_CREATE` etc., em `Domain/Security/PolicyMaster.cs`) atribuídos ao usuário na emissão do JWT. `[Authorize(Policy = "...")]` nos controllers verifica esses claims diretamente (ver `WebApi/Extensions/PoliciesConfiguration.cs`). Ao adicionar um endpoint protegido, defina ou reutilize uma constante em `PolicyMaster`; não compare roles diretamente. O JWT é HMAC-SHA256 sem estado, expira em 7 dias e não há refresh tokens.

### Onde as coisas são registradas

- `Program.cs` é o composition root: cada camada expõe um `Add<Capa>()` (`AddApplication`, `AddPersistence`, `AddInfrastructure`, `AddIdentityServices`, `AddPoliciesServices`) invocado ali.
- `MasterNet.Infrastructure.DependencyInjection.AddInfrastructure()` está vazio de propósito — os serviços de infraestrutura (`IPhotoService`, `IReportService<>`) são registrados diretamente em `Program.cs`. Siga esse mesmo padrão se adicionar um novo serviço de infraestrutura; não o mova para `AddInfrastructure()` sem que isso seja pedido.
- OData está habilitado na rota `courses` (`/odata`) além dos controllers REST padrão.

## Pontos fracos conhecidos (não os "corrija" sem que seja pedido)

- `TokenKey` (chave de assinatura JWT) só está em `appsettings.Development.json`, faltando em Production/Staging.
- O CORS usa `WithOrigins("*")` + `AllowAnyMethod()`/`AllowAnyHeader()` — aberto a qualquer origem; válido para dev, não para produção.
- As credenciais do admin de seed (`src/MasterNet.Persistence/SeedData` + `SeedDatabase`, invocado via `UseAsyncSeeding` em `Persistence/DependencyInjection.cs`) e a senha do SQL Server (`docker-compose.yml`, connection strings) estão hardcoded em texto puro — é o esperado neste setup local/didático; não replique isso como padrão em código novo.
- `ISpecification<T>` em Domain tem uma única implementação (`DeviceByNameContainsSpec`) e nem sequer é resolvido por meio do repositório genérico — é aplicado manualmente contra o `DbContext`. Não generalize esse padrão sem que o usuário peça explicitamente.

# Domínio
@docs/domain-glossary.md
