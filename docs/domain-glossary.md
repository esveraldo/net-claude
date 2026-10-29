# Glossário do Domínio

Dicionário de dados de apoio para as entidades, objetos de valor, contratos e constantes definidos em `src/MasterNet.Domain`. Os nomes técnicos (classes, propriedades, namespaces) são mantidos em seu idioma original (inglês); as definições são explicadas em português.

## Abstractions

Contratos e tipos base compartilhados por todas as entidades do domínio. Não dependem de nenhuma outra camada.

### BaseEntity

`Abstractions/BaseEntity.cs`

Classe base abstrata da qual herda a maioria das entidades do domínio. Fornece a propriedade `Id` (`Guid`) como identificador único.

| Propriedade | Tipo | Descrição |
|---|---|---|
| `Id` | `Guid` | Identificador único da entidade. |

### IGenericRepository\<TEntity\>

`Abstractions/IGenericRepository.cs`

Contrato de repositório genérico "enxuto", pensado para operações simples de **escrita**. É usado por meio de `IUnitOfWork.Repository<T>()`. As leituras complexas (paginação, filtros, projeções) não passam por aqui; elas injetam `MasterNetDbContext` diretamente a partir de `Application`.

| Membro | Descrição |
|---|---|
| `GetByIdAsync(Guid id)` | Obtém uma entidade pelo seu `Id`, ou `null` se não existir. |
| `AddAsync(TEntity entity)` | Adiciona uma nova entidade ao contexto de persistência. |

### ISpecification\<T\>

`Abstractions/ISpecification.cs`

Contrato do padrão *Specification*, que encapsula um critério de filtragem como uma expressão (`Criteria`) reutilizável. Atualmente tem uma única implementação no projeto (`DeviceByNameContainsSpec`, em `Persistence`) e é aplicado manualmente contra o `DbContext`, sem ser generalizado por meio do repositório genérico.

| Membro | Descrição |
|---|---|
| `Criteria` | Expressão (`Expression<Func<T, bool>>`) que define a condição de filtragem a ser aplicada sobre `T`. |

### IUnitOfWork

`Abstractions/IUnitOfWork.cs`

Contrato da Unidade de Trabalho (*Unit of Work*): coordena um ou mais repositórios genéricos e confirma ou reverte as mudanças como uma única transação.

| Membro | Descrição |
|---|---|
| `Repository<TEntity>()` | Retorna o `IGenericRepository<TEntity>` correspondente à entidade indicada. |
| `SaveChangesAsync()` | Persiste as mudanças pendentes; retorna `true` se pelo menos uma mudança foi salva. |
| `RollbackAsync()` | Descarta as mudanças pendentes sem persisti-las. |

## Courses

Entidades relacionadas aos cursos oferecidos pela plataforma: o curso em si e suas tabelas de junção com instrutores e preços.

### Course

`Courses/Course.cs`

Entidade central do domínio: representa um curso online. Herda de `BaseEntity`.

| Propriedade | Tipo | Descrição |
|---|---|---|
| `Title` | `string?` | Título do curso. |
| `Description` | `string?` | Descrição do curso. |
| `PublishedAt` | `DateTime?` | Data de publicação do curso. |
| `Ratings` | `ICollection<Rating>?` | Avaliações (`Rating`) que os estudantes deixaram sobre o curso. |
| `Prices` | `ICollection<Price>?` | Preços associados ao curso (relação muitos-para-muitos via `CoursePrice`). |
| `CoursePrices` | `ICollection<CoursePrice>?` | Registros da tabela de junção curso-preço. |
| `Instructors` | `ICollection<Instructor>?` | Instrutores que ministram o curso (relação muitos-para-muitos via `CourseInstructor`). |
| `CourseInstructors` | `ICollection<CourseInstructor>?` | Registros da tabela de junção curso-instrutor. |
| `Photos` | `ICollection<Photo>?` | Fotos associadas ao curso. |

### CourseInstructor

`Courses/CourseInstructor.cs`

Entidade de junção (tabela intermediária) que representa a relação muitos-para-muitos entre `Course` e `Instructor`.

| Propriedade | Tipo | Descrição |
|---|---|---|
| `CourseId` | `Guid?` | Chave estrangeira para o curso. |
| `Course` | `Course?` | Referência de navegação para o curso. |
| `InstructorId` | `Guid?` | Chave estrangeira para o instrutor. |
| `Instructor` | `Instructor?` | Referência de navegação para o instrutor. |

### CoursePrice

`Courses/CoursePrice.cs`

Entidade de junção (tabela intermediária) que representa a relação muitos-para-muitos entre `Course` e `Price`.

| Propriedade | Tipo | Descrição |
|---|---|---|
| `CourseId` | `Guid?` | Chave estrangeira para o curso. |
| `Course` | `Course?` | Referência de navegação para o curso. |
| `PriceId` | `Guid?` | Chave estrangeira para o preço. |
| `Price` | `Price?` | Referência de navegação para o preço. |

## Devices

Modela dispositivos, usados como exemplo do padrão *Specification* (`ISpecification<T>`) e de objeto de valor (`DeviceName`).

### Device

`Devices/Device.cs`

Entidade selada (`sealed`) que representa um dispositivo. Herda de `BaseEntity`. Seu construtor é privado por padrão e exige `DeviceName` como parâmetro obrigatório, forçando a criação por meio de um objeto de valor já validado.

| Propriedade | Tipo | Descrição |
|---|---|---|
| `DeviceName` | `DeviceName` | Objeto de valor com o nome do dispositivo. Só pode ser atribuído na construção (`private set`). |

### DeviceName

`Devices/DeviceName.cs`

Objeto de valor (*Value Object*) que encapsula o nome de um dispositivo. Valida que o valor não seja nulo nem vazio ao ser construído e define igualdade estrutural (`IEquatable<DeviceName>`) case-insensitive.

| Membro | Descrição |
|---|---|
| `Value` | `string` com o nome do dispositivo; imutável após a construção. |
| `Equals(DeviceName?)` | Compara duas instâncias ignorando maiúsculas/minúsculas. |
| `GetHashCode()` | Hash consistente com a comparação case-insensitive. |

## Instructors

### Instructor

`Instructors/Instructor.cs`

Entidade que representa um instrutor de cursos. Herda de `BaseEntity`.

| Propriedade | Tipo | Descrição |
|---|---|---|
| `LastName` | `string?` | Sobrenome do instrutor. |
| `FirstName` | `string?` | Nome do instrutor. |
| `Degree` | `string?` | Título ou grau acadêmico do instrutor. |
| `Courses` | `ICollection<Course>?` | Cursos que o instrutor ministra (relação muitos-para-muitos via `CourseInstructor`). |
| `CourseInstructors` | `ICollection<CourseInstructor>?` | Registros da tabela de junção curso-instrutor. |

## Photos

### Photo

`Photos/Photo.cs`

Entidade que representa uma foto associada a um curso, armazenada por meio de um provedor externo (Cloudinary, gerenciado em `Infrastructure`). Herda de `BaseEntity`.

| Propriedade | Tipo | Descrição |
|---|---|---|
| `Url` | `string?` | URL pública da foto. |
| `CourseId` | `Guid?` | Chave estrangeira para o curso ao qual a foto pertence. |
| `Course` | `Course?` | Referência de navegação para o curso. |
| `PublicId` | `string?` | Identificador público da imagem no provedor externo (Cloudinary), usado para excluí-la ou referenciá-la. |

## Prices

### Price

`Prices/Price.cs`

Entidade que representa um plano ou nível de preço que pode ser aplicado a um ou mais cursos. **Não** herda de `BaseEntity`; declara seu próprio `Id`.

| Propriedade | Tipo | Descrição |
|---|---|---|
| `Id` | `Guid` | Identificador único do preço. |
| `Name` | `string?` | Nome do plano de preço (ex.: "Básico", "Premium"). |
| `CurrentPrice` | `decimal` | Preço atual/regular. |
| `PromotionPrice` | `decimal` | Preço promocional ou de oferta. |
| `Courses` | `ICollection<Course>?` | Cursos que usam este preço (relação muitos-para-muitos via `CoursePrice`). |
| `CoursePrices` | `ICollection<CoursePrice>?` | Registros da tabela de junção curso-preço. |

## Ratings

### Rating

`Ratings/Rating.cs`

Entidade que representa a avaliação que um estudante deixa sobre um curso. Herda de `BaseEntity`.

| Propriedade | Tipo | Descrição |
|---|---|---|
| `Student` | `string?` | Nome do estudante que avalia. |
| `Score` | `int` | Pontuação numérica da avaliação. |
| `Comment` | `string?` | Comentário opcional do estudante. |
| `CourseId` | `Guid?` | Chave estrangeira para o curso avaliado. |
| `Course` | `Course?` | Referência de navegação para o curso. |

## Security

Constantes usadas pelo sistema de autorização baseado em claims (ver `ARCHITECTURE.md` e `CLAUDE.md` para o detalhamento do mecanismo). Os roles agrupam políticas (*policies*), e são as políticas que efetivamente são verificadas nos endpoints via `[Authorize(Policy = "...")]`.

### CustomClaims

`Security/CustomClaims.cs`

Nomes de *claims* personalizados incluídos no JWT.

| Constante | Valor | Descrição |
|---|---|---|
| `POLICIES` | `"POLICIES"` | Nome do claim que transporta as políticas (permissões granulares) do usuário dentro do token. |

### CustomRoles

`Security/CustomRoles.cs`

Roles de usuário suportados pela aplicação. Funcionam como agrupamentos de políticas (`PolicyMaster`) no momento da emissão do JWT, não como mecanismo de autorização direto.

| Constante | Valor | Descrição |
|---|---|---|
| `ADMIN` | `"ADMIN"` | Role de administrador; agrupa o conjunto completo de políticas. |
| `CLIENT` | `"CLIENT"` | Role de usuário cliente/estudante; agrupa um subconjunto reduzido de políticas. |

### PolicyMaster

`Security/PolicyMaster.cs`

Catálogo de políticas (permissões granulares) usadas por `[Authorize(Policy = "...")]` nos controllers. Cada política habilita uma operação específica sobre uma entidade do domínio.

| Constante | Descrição |
|---|---|
| `COURSE_WRITE` | Permite criar cursos. |
| `COURSE_READ` | Permite ler/consultar cursos. |
| `COURSE_UPDATE` | Permite atualizar cursos existentes. |
| `COURSE_DELETE` | Permite excluir cursos. |
| `INSTRUCTOR_READ` | Permite ler/consultar instrutores. |
| `INSTRUCTOR_UPDATE` | Permite atualizar instrutores existentes. |
| `INSTRUCTOR_CREATE` | Permite criar instrutores. |
| `RATING_READ` | Permite ler/consultar avaliações. |
| `RATING_DELETE` | Permite excluir avaliações. |
| `RATING_CREATE` | Permite criar avaliações. |
