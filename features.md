# НОО.Платформа 3.0

С 2024 года у онлайн-школы по подготовке к ЕГЭ своя онлайн-платформа, на которой ученики могут самостоятельно проходить курсы, скачивать материалы и делать домашние задания. В период с 2024 по 2026 мы активно собирали информацию об использовании, составляли отчеты об ошибках и занимались оптимизациями. Так как проект довольно большой, за это время мы смогли понять, как платформа используется учениками, какой функционал необходим и от какого можно избавиться. Но главное - мы обнаружили много технических проблем. Исправление некоторых из них требовало кардинального изменения архитектуры программы. Отдельной проблемой была пропускная способность приложения, которую постоянно требовалось повышать в связи с притоком новых пользователей. В итоге мы пришли к выводу, что гораздо эффективнее будет сделать новую платформу для обучения, с учетом всех ошибок и оптимизаций. Название 3.0 объясняется тем, что первая версия платформы была сделана еще на этапе создания проекта, и было решено от нее отказаться еще до старта проекта, поэтому актуальная версия НОО.Платформы - 2.0. 

## Технические решения

Предыдущая версия нашей платформы использовала такие технологии, как Node.js и ASP.NET, для своей серверной (бэкенд) части. Наличие двух взаимозаменяемых технологий усложняло поиск и исправление багов и было не самым оптимальным техническим решением. В новой версии мы полностью перешли на ASP.NET C# 10, что само по себе увеличило пропускную способность сервера на 40%.

Следующим узким местом была база данных: в пиковые часы, когда тысячи учащихся начинали делать домашнее задание, база данных MySQL 8.4 была перегружена. Connection pool решал проблему лишь частично, а один долгий side-effect запрос мог замедлить сервер для всех. Кэширования не было задумано изначально, и задача по его внедрению была слишком трудоемкой. Для резервного копирования приходилось делать Snapshot всего диска, так как стандартные методы добавляли еще больше нагрузки на и без того работающую на пределе базу данных. В новой версии мы не отказались от MySQL, но перешли на схему Master-Slaves (а именно 1 master и 2 slaves), где запись проходит через основную БД, а чтение распределяется равномерно через Round Robin алгоритм. Благодаря новой архитектуре хранилища мы смогли добиться кратно меньшего использования серверных ресурсов (RAM, vCPU, disk utilization), а расположение БД на разных нодах кластера k8s решило проблему неэффективного резервного копирования. При возникновении проблем с одной из нод БД работает в штатном режиме, а после восстановления упавшая нода автоматически синхронизируется с master. 

Далее про файловое хранилище. Для предыдущей версии платформы мы не стали использовать S3-сервис, так как посчитали это чрезмерно сложным и неоправданным. Но, столкнувшись с огромным притоком новых пользователей и объемом файлов до 160 ГБ в сутки, мы поняли, что это стало необходимостью. НОО.Платформа 3.0 сохраняет все файлы в S3-совместимом хранилище от Яндекс, что позволило не только оптимизировать расходы на SSD, но и контролировать доступ к каждому файлу для каждого пользователя более точно, упростив параллельно с этим поиск нужного файла с помощью меток.

В браузерной версии платформы (фронтенд) мы переосмыслили большую часть элементов дизайна: от кнопок до ссылок и анимаций. Теперь стиль веб-приложения выглядит более единообразно, интерфейс стал еще более понятен среднестатистическому пользователю. Механизмы работы фронтенда мы переписали с нуля, используя MVVM паттерн и SignalR для запросов на сервер в реальном времени. Скорость работы увеличилась на 20%, а используемая оперативная память на 55%. Были добавлены 3 отдельных субдомена: для раздела помощи и поддержки, для раздела тренировок учеников по карточкам (по аналогии с Quizlet) и для прохождения опросов.

Все вышеперечисленные компоненты работали внутри нашего одноузлового кластера Kubernetes, но его архитектура фундаментально не подходила под большой поток пользователей и возможное будущее внедрение микросервисов. Эту часть мы тоже переписали и перешли на 3-узловую архитектуру.

### Серверная часть кода

| Параметр | НОО.Платформа 2.0 | НОО.Платформа 3.0 |
|---|---|---|
| Объём кода | 844 файла, ~55 тыс. строк | 1246 файлов, ~142 тыс. строк |
| Язык и платформа | ASP.NET + Node.js (TypeScript) | ASP.NET (C#) |
| Архитектура | MVC | MVC (без изменений) |
| Тестирование | Юнит-тесты | Юнит, интеграционные, нагрузочные тесты и тесты БД |
| Количество тестов | 88 | ~1300 |
| Покрытие кода тестами | 60% | 100% |
| Связь с фронтендом | REST | REST + SignalR |
| Обновление сущностей | PATCH с полной перезаписью | Модифицированный JsonPatchDocument (RFC 6902 + вложенные обновления) |
| Авторизация | Только access token (действует 30 дней) | OAuth 2.0 (RFC 6749): access token на 15 минут + автоматическая ротация |
| База данных | MySQL 8.4, один сервер | MySQL, 1 master + 2 slaves, чтение через Round Robin |
| ORM | TypeORM | Entity Framework Core |
| Кэширование | Нет | Redis (3 instances) |
| Файловое хранилище | Диск сервера | S3-совместимое хранилище Яндекс |
| Масштабирование real-time | - | SignalR с Redis backplane |
| Логирование и мониторинг | | OpenTelemetry (логи, трейсы, метрики) |
| Защита от перегрузки | Нет | RateLimiter (FixedWindow) + блокировка IP |
| Устойчивость к сбоям внешних сервисов | ручная | Polly (повторные запросы) |
| Проверки состояния сервиса | - | Health checks |
| Фоновые задачи | cronjobs | Собственный планировщик |
| Версионирование API | - | Asp.Versioning |
| Документация API | md файлы | OpenAPI (Swagger, Scalar) |
| Статический анализ кода | js-linter | .NET Analyzers + Roslynator |
| Кластер Kubernetes | 1 узел | 3 узла |
| Макс. одновременных пользователей | 12 000 | 25 000 |
| Среднее время ответа | 220 ms | 20 ms |
| Запросов в секунду (пик) | 180 | 6000 |
| Потребление RAM сервером | ~2 Gb | ~3 Gb |
| Потребление vCPU сервером | ~1.3 | ~1.2 |

Все эти решения позволили сделать платформу намного более интерактивной, быстрой и радикально снизили количество возможных багов.

### Браузерная часть кода

| Параметр | НОО.Платформа 2.0 | НОО.Платформа 3.0 |
|---|---|---|
| Объём кода | 897 файлов, ~59 тыс. строк | 772 файла, ~87 тыс. строк |
| Фреймворк | Vue.js | Vue.js (без изменений) |
| Линтер и типизация | Базовые правила | Значительно более строгий ESLint и строгая типизация TypeScript |
| Тестирование | Нет | Юнит-тесты (Vitest) |
| Количество тестов | 0 | 663 |
| Архитектура | Pinia stores | Многослойный расширяемый MVVM |
| HTTP-запросы | XMLHttpRequest | Axios |
| Запросы в реальном времени | Нет | SignalR |
| Система событий | Нет | Event emitter |
| Richtext-редактор | Quill | Tiptap |
| Совместное редактирование | Нет | Yjs + SignalR |
| Обновление сущностей | PATCH с полной перезаписью | Модифицированный JSON Patch (RFC 6902) |
| Валидация данных | helper functions | Zod |
| Загрузка файлов | XMLHttpRequest | Возобновляемая загрузка (tus) |
| Типы данных API | динамические | Генерируются из схемы OpenAPI |
| Проверки перед коммитом | - | Husky + Prettier + ESLint |
| Сборщик | Vite | Vite (без изменений) |
| Субдомены | 1 | 4 (основной, поддержка, карточки, опросы) |
| Скорость работы | | +20% |
| Потребление оперативной памяти | | −55% |
| Размер бандла | 1.3 Mb | 80 Kb |
| Время первой загрузки | ~2 s | ~200 ms |

Кроме заметного ускорения работы приложения в браузере, мы смогли внедрить multiple cursor editing режим для редактирования контента за счет перехода на TipTap и использования SignalR.

## Новые функции

1. Значительно улучшена производительность и скорость работы

Как уже выше описано, это было сделано за счет полного пересмотра кода, его архитектуры и использования более быстрых технологий.

2. Переработан дизайн многих компонентов

В первую очередь мы унифицировали все элементы дизайна. Теперь все элементы, такие как кнопки, списки, блоки, изображения и прочее, выглядят как часть одной системы. У большинства элементов управления появились новые функции: одновременная работа нескольких пользователей в одном richtext-поле ввода, наш выпадающий календарь для выбора даты вместо нативного, всплывающие подсказки у элементов форм, иконки индикации загрузки у картинок и файлов и т.д.

3. Три новых сайта-поддомена: для опросов, для поддержки и для карточек с заданиями

Раньше все эти функции были встроены в один layout, из-за чего многие пользователи путали, например, страницу поддержки в разделе "Курсы" со страницей списка всех курсов. Теперь у каждого из этих разделов свой сайт-поддомен, визуально отличающийся от рабочего пространства ученика. Добавлены многочисленные анимации.

4. Одновременное редактирование работы/курса несколькими людьми

SignalR дал возможность видеть изменение контента на странице в режиме реального времени, без необходимости перезагрузки вкладки. Мы использовали это, среди прочего, для совместного редактирования данных. Теперь, если несколько пользователей редактируют один и тот же раздел на платформе (курсы, работы или опросы), они видят курсоры друг друга и места, где и кто в каком месте редактирует.

5. Максимальное количество одновременных пользователей до 25 тысяч

Переход на новую топологию кластера, рефакторинг узких мест в коде и внедрение ASP.NET увеличили пропускную способность сервера до 25 000 без увеличения мощностей серверов. Тест был проведен с помощью программного обеспечения Apache JMeter и симулировал одновременную сдачу домашнего задания, что является самым ресурсоёмким процессом для сервера. Это самое актуальное изменение, учитывая постоянно ускоряющийся рост числа новых учеников и увеличение числа работников школы.

6. Возможность просмотреть список изменений после изменения курса, работы, опроса

После редактирования любой сущности в приложении теперь можно посмотреть список изменений, как в популярной программе git. Для этого пришлось сделать новый алгоритм сравнения, базирующийся на стандарте RFC 6902 (JsonPatch). Но в таком виде алгоритм не подошел, так как возникли проблемы с обновлением вложенных сущностей. Для этого мы сделали fork официального стандарта и кастомизировали его, добавив nested update. 

7. Добавлена статистика пользователей по устройствам и браузерам

На старой платформе была только статистика, нужная преподавателям и администраторам платформы, такая как текущий онлайн и количество сданных работ. Мы добавили статистику по браузерам и устройствам пользователей, что поможет сузить поиск багов в будущем.

8. В течение года не требуется повторно входить в аккаунт

Мы перешли с механизма Access Token Only на общепринятый стандарт RFC 6749. Access token выдается пользователю при входе на платформу и действителен 15 минут, после чего происходит автоматическая ротация токена через HTTP-заголовки. Это стало важным элементом новой системы защиты программы от взлома и утечки данных.

9. Добавлена история пользователя и его действий

Для каждого пользователя можно теперь отследить все его действия или действия, связанные с ним: смена куратора, новый вход, изменение личных данных, сдача домашнего задания, изменение роли и т.д. Раньше данная информация была доступна только через логи системному администратору, теперь каждый пользователь с ролью "преподаватель" или "администратор" может зайти на страницу пользователя и увидеть все его действия.

10. Можно прикрепить более одной работы к материалу, добавив при этом заметку и дату выключения работы

Функционал добавления домашнего задания стал более обширным: можно добавить больше 1 домашнего задания (работы) к одному материалу курса, а к каждой работе добавить примечание и дату, после которой работа больше не будет доступна для решения. Это требовалось для некоторых курсов, где был так называемый "Банк заданий" - большой список работ для тренировки ученикам.

11. Добавлена статистика в НОО.Tube

Внедрение своего видеохостинга на платформу помогло решить проблему недоступности других видеосервисов, убрать рекламу и стать в этом смысле более независимой онлайн-школой. Однако предыдущий API не позволял получать статистику просмотров. Теперь это возможно. Кроме самих просмотров, у администратора есть возможность посмотреть, какие участки видео более просматриваемые, какие ученик решил посмотреть несколько раз и какая средняя длительность просмотра. Планируется, что это поможет создавать видеоуроки, лучше отвечающие потребностям учащихся.

12. Подробная статистика скачиваний файлов курса по времени и ученикам

Для избежания использования учебных материалов, доступных в нашем приложении, для дальнейшей передачи третьим лицам, мы добавили статистику скачиваний всех файлов. Можно посмотреть, кто и когда что скачал и сколько раз.

13. Поиск по материалам и главам внутри курса

Мы столкнулись с тем, что в некоторых годовых курсах огромное количество материалов и навигация по ним крайне неудобна, ученики зачастую терялись и обращались в поддержку. Мы добавили поиск по всем материалам курса: по их текстовому содержанию, прикрепленным файлам, опросам и видео.

14. Подготовлена основа для внедрения подписок и оплат

В планах онлайн-школы НОО перенести все существующие ее отделы на новую платформу, в том числе и раздел оплат. Внедрить механизмы платежей на предыдущей платформе не представлялось возможным в силу недостаточного уровня безопасности и не рассчитанной для этого программной архитектуры. Новая платформа в данном плане имеет повышенный уровень безопасности и флексибельную архитектуру, что даёт возможность переноса на неё системы покупки и дальнейшей оплаты курсов.

15. Добавлена возможность ученика просмотреть свои ответы в опросе и возможность преподавателя увидеть их на странице ученика

Опросы были одной из поздних функций платформы, введенной в эксплуатацию относительно поздно. Рассчитав время и доступные ресурсы, мы сделали минимально рабочую систему опросов, при которой пользователь не мог посмотреть ни список пройденных опросов, ни конкретные ответы на один из них. Это изменилось, и теперь все данные об опросах доступны пользователю в read-only режиме.

16. Все элементы на странице остаются живыми и обновляются без обновления страницы

В эргономику платформы, благодаря технологии SignalR, встроились обновления контента в режиме реального времени. Если преподаватель изменил задание в определенной работе, это сразу же будет видно ученику, решающему эту работу, без обновления страницы или любых других дополнительных действий. Даже элементы вроде дат и времени стали живыми: сначала платформа показывает, что работа была сдана 1 минуту назад, а затем 2 минуты назад, без интеракций пользователя.

17. Новый слой защиты от DDoS-атак

Сервер теперь включает в себя RateLimiter, блокирующий интенсивный входной трафик по алгоритму FixedWindow. Попытка загрузить сервер автоматически сгенерированными запросами не принесет ущерб серверу, который внесет вредоносный IP-адрес в черный список и не будет отвечать на запросы с него в течение определенного времени.

## Для системных администраторов

Мы добавили логи, трейсы и отчеты по стандарту OpenTelemetry для всех запросов на сервер. Теперь каждый баг или проблему ученика можно отследить вплоть до конкретного запроса на сервер и даже посмотреть timeframes запросов в базу данных, совершенных в этот момент. Исправление ошибок теперь станет проще, быстрее и эффективнее, а работа отдела поддержки качественнее.

## Ключевые моменты в коде

Отправная точка всего приложения - файл Program.cs, где происходит сборка и инициализация всех сервисов:

```cs
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.Initialization.App;
using Noo.Api.Core.Initialization.ServiceCollection;
using Noo.Api.Core.Initialization.WebHostBuilder;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Sessions.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider((context, options) =>
{
    options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
    options.ValidateOnBuild = true;
});

builder.Services.LoadEnvConfigs(builder.Configuration);
builder.Services.AddS3Storage();
builder.Services.AddNooDbContext(builder.Configuration);
builder.Services.AddNooAuthentication(builder.Configuration);
builder.Services.AddNooAuthorization();
builder.Services.AddNooSwagger(builder.Configuration);
builder.Services.AddLogger(builder.Configuration);
builder.Services.AddNooOpenTelemetry(builder.Configuration);
builder.Services.RegisterDependencies();
builder.Services.AddNooControllersAndConfigureJson();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClientFactory();
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddNooResponseCompression();
builder.Services.AddHealthcheckServices();
builder.Services.AddRouteConstraints();
builder.Services.AddRequestRateLimiter(builder.Configuration);
builder.Services.AddRouting();
builder.Services.AddAutoMapperProfiles();
builder.Services.AddCacheProvider(builder.Configuration);
builder.Services.AddMetrics();
builder.Services.AddDomainEvents();
builder.Services.AddHostedServices();
builder.Services.AddNooRealtime(builder.Configuration);
builder.Services.AddNooCollaboration(builder.Configuration);

builder.WebHost.AddWebServerConfiguration(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandling();
app.UseRouting();
app.UseRateLimiter();
app.UseNooSwagger(app.Configuration);
app.UseCors();
app.UseResponseCompression();
app.UseAuthentication();
app.UseAuthorization();
app.UseSessionActivity();
app.MapControllers();
app.MapNooHubs();
app.MapHealthAllChecks();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();
    if (app.Environment.IsEnvironment("Testing"))
    {
        await db.Database.EnsureCreatedAsync();
    }
    else
    {
        await db.Database.MigrateAsync();
    }
}

await app.RunAsync();
```

Архитектура построена по принципу монолита, в будущем легко разбиваемого на микросервисы. Пример архитектуры:

Контроллер:
```cs
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Noo.Api.Core.Request;
using Noo.Api.Core.Response;
using Noo.Api.Core.Utils.Versioning;
using Noo.Api.Works.DTO;
using Noo.Api.Works.Filters;
using Noo.Api.Works.Models;
using Noo.Api.Works.Services;
using Noo.Api.Works.Types;
using SystemTextJsonPatch;
using ProducesAttribute = Noo.Api.Core.Documentation.ProducesAttribute;

namespace Noo.Api.Works;

[ApiVersion(NooApiVersions.Current)]
[ApiController]
[Route("work")]
public class WorkController : ApiController
{
    private readonly IWorkService _workService;

    public WorkController(IWorkService workService, IMapper mapper)
        : base(mapper)
    {
        _workService = workService;
    }

    /// <summary>
    /// Searches for works based on the provided criteria.
    /// </summary>
    [HttpGet]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = WorkPolicies.CanSearchWorks)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<WorkDTO>>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> GetWorksAsync([FromQuery] WorkFilter filter)
    {
        var result = await _workService.GetWorksAsync(filter);

        return SendResponse<WorkModel, WorkDTO>(result);
    }

    /// <summary>
    /// Retrieves a specific work by its ID.
    /// It will also include the work's associated tasks
    /// </summary>
    [HttpGet("{id}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = WorkPolicies.CanGetWork)]
    [Produces(
        typeof(ApiResponseDTO<WorkDTO>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> GetWorkAsync([FromRoute] Ulid id)
    {
        var work = await _workService.GetWorkAsync(id);

        return SendResponse<WorkModel, WorkDTO>(work);
    }

    /// <summary>
    /// Retrieves statistics of a particular work by its id
    /// </summary>
    [HttpGet("{id}/statistics")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = WorkPolicies.CanGetWorkStatistics)]
    [Produces(
        typeof(ApiResponseDTO<WorkStatisticsDTO>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> GetWorkStatisticsAsync([FromRoute] Ulid id)
    {
        var statistics = await _workService.GetWorkStatisticsAsync(id);

        return SendResponse<WorkStatistics, WorkStatisticsDTO>(statistics);
    }

    /// <summary>
    /// Retrieves the list of materials with their chain via chapters up to a course
    /// which has a work assignment with the given work id
    /// </summary>
    [HttpGet("{id}/relation")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = WorkPolicies.CanGetWorkRelations)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<WorkRelationDTO>>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> GetWorkMaterialAsync([FromRoute] Ulid id)
    {
        var relations = await _workService.GetWorkRelationsAsync(id);

        return SendResponse<IEnumerable<WorkRelationDTO>>(
            _mapper.Map<IEnumerable<WorkRelationDTO>>(relations)
        );
    }

    /// <summary>
    /// Creates a new work.
    /// </summary>
    [HttpPost]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = WorkPolicies.CanCreateWorks)]
    [Produces(
        typeof(ApiResponseDTO<IdResponseDTO>),
        StatusCodes.Status201Created,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public IActionResult CreateWork([FromBody] CreateWorkDTO work)
    {
        var id = _workService.CreateWork(work);
        return SendResponse(id);
    }

    /// <summary>
    /// Updates an existing work.
    /// </summary>
    [HttpPatch("{id}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = WorkPolicies.CanEditWorks)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> UpdateWorkAsync(
        [FromRoute] Ulid id,
        [FromBody] JsonPatchDocument<UpdateWorkDTO> work
    )
    {
        await _workService.UpdateWorkAsync(id, work);

        return SendResponse();
    }

    /// <summary>
    /// Deletes a work by its ID.
    /// </summary>
    [HttpDelete("{id}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = WorkPolicies.CanDeleteWorks)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public IActionResult DeleteWork([FromRoute] Ulid id)
    {
        _workService.DeleteWork(id);
        return SendResponse();
    }
}
```

Пример рабочего сервиса:

```cs
using AutoMapper;
using Noo.Api.Core.DataAbstraction.Cache;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Courses.Utils;
using Noo.Api.Works.DTO;
using Noo.Api.Works.Filters;
using Noo.Api.Works.Models;
using Noo.Api.Works.Types;
using SystemTextJsonPatch;

namespace Noo.Api.Works.Services;

[RegisterScoped(typeof(IWorkService))]
public class WorkService : IWorkService
{
    private static readonly TimeSpan _statisticsCacheTtl = TimeSpan.FromMinutes(15);

    private readonly IWorkRepository _workRepository;

    private readonly IMapper _mapper;

    private readonly IJsonPatchUpdateService _patchUpdateService;

    private readonly ICacheRepository _cache;

    public WorkService(
        IWorkRepository workRepository,
        IMapper mapper,
        IJsonPatchUpdateService patchUpdateService,
        ICacheRepository cache
    )
    {
        _workRepository = workRepository;
        _mapper = mapper;
        _patchUpdateService = patchUpdateService;
        _cache = cache;
    }

    public Ulid CreateWork(CreateWorkDTO work)
    {
        var model = _mapper.Map<WorkModel>(work);

        model.Tasks.Renumber();
        model.MaxScore = model.Tasks?.Sum(t => t.MaxScore) ?? 0;
        _workRepository.Add(model);

        return model.Id;
    }

    public Task<WorkModel?> GetWorkAsync(Ulid id)
    {
        return _workRepository.GetWithTasksAsync(id);
    }

    public Task<SearchResult<WorkModel>> GetWorksAsync(WorkFilter filter)
    {
        return _workRepository.SearchAsync(filter);
    }

    public async Task UpdateWorkAsync(Ulid id, JsonPatchDocument<UpdateWorkDTO> updateWorkDto)
    {
        var workModel = await _workRepository.GetWithTasksAsync(id);

        workModel.ThrowNotFoundIfNull();

        _patchUpdateService.ApplyPatch(workModel, updateWorkDto);

        // A patch that removed a task leaves a hole where it was, so the numbering is
        // settled here rather than trusted from the client.
        workModel.Tasks.Renumber();
        workModel.MaxScore = workModel.Tasks?.Sum(t => t.MaxScore) ?? 0;

        // Every cached figure is a percentage of MaxScore or a per-task summary, so an edit that
        // touched either leaves the cache reporting numbers that no longer describe this work.
        await _cache.RemoveAsync(StatisticsCacheKey(id));
    }

    public void DeleteWork(Ulid id)
    {
        _workRepository.DeleteById(id);
    }

    public async Task<WorkStatistics?> GetWorkStatisticsAsync(Ulid id)
    {
        var work = await _workRepository.GetWithTasksAsync(id);

        if (work is null)
        {
            return null;
        }

        var statistics = await _cache.GetOrSetAsync(
            StatisticsCacheKey(id),
            () => BuildStatisticsAsync(id, work.MaxScore),
            _statisticsCacheTtl
        );

        statistics!.Work = work;

        return statistics;
    }

    private async Task<WorkStatistics> BuildStatisticsAsync(Ulid id, int maxScore)
    {
        var scores = await _workRepository.GetScoresAsync(id);
        var taskSummaries = await _workRepository.GetTaskSummariesAsync(id);
        var solveCount = await _workRepository.CountSolvedAsync(id);

        return new WorkStatistics
        {
            TaskSummaries = taskSummaries,
            AverageWorkScore = new()
            {
                Absolute = scores.AverageOrNull(),
                Percentage = scores.AveragePercentageOrNull(maxScore),
            },
            MedianWorkScore = new()
            {
                Absolute = scores.MedianOrNull(),
                Percentage = scores.MedianPercentageOrNull(maxScore),
            },
            WorkSolveCount = solveCount,
        };
    }

    private static string StatisticsCacheKey(Ulid id)
    {
        return $"work:statistics:{id}";
    }

    public async Task<IEnumerable<WorkRelation>> GetWorkRelationsAsync(Ulid workId)
    {
        var relations = await _workRepository.GetWorkRelationsAsync(workId);

        foreach (var relation in relations)
        {
            var material = relation.Assignment.CourseMaterialContent.Material;
            var course = material.Chapter.Course;

            relation.Path = CourseMaterialPath.Build(
                course.Name,
                course.Chapters.ToDictionary(chapter => chapter.Id),
                material.ChapterId,
                material.Title
            );
        }

        return relations;
    }
}
```

Пример репозитория:

```cs
using Microsoft.EntityFrameworkCore;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Courses.Models;
using Noo.Api.Works.Models;
using Noo.Api.Works.Types;

namespace Noo.Api.Works.Services;

[RegisterScoped(typeof(IWorkRepository))]
public class WorkRepository : Repository<WorkModel>, IWorkRepository
{
    public WorkRepository(NooDbContext context)
        : base(context) { }

    public Task<WorkModel?> GetWithTasksAsync(Ulid id)
    {
        return Context
            .GetDbSet<WorkModel>()
            .Include(x => x.Tasks!.OrderBy(task => task.Order))
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<int> CountSolvedAsync(Ulid id)
    {
        return AssignedWorksOf(id)
            .CountAsync(aw => AssignedWorkStatuses.Solved.Contains(aw.SolveStatus));
    }

    public Task<List<int>> GetScoresAsync(Ulid id)
    {
        return AssignedWorksOf(id)
            .Where(aw => aw.Score != null)
            .Select(aw => aw.Score!.Value)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<TaskSummary>> GetTaskSummariesAsync(Ulid id)
    {
        var maxScores = await Context
            .GetDbSet<WorkTaskModel>()
            .Where(task => task.WorkId == id)
            .Select(task => new { task.Id, task.MaxScore })
            .ToListAsync();

        var averageScores = await GetTaskAverageScoresAsync(id);

        return maxScores
            .Select(task => new TaskSummary
            {
                TaskId = task.Id,
                MaxScore = task.MaxScore,
                AverageScore = averageScores.TryGetValue(task.Id, out var average) ? average : null,
            })
            .OrderBy(summary =>
                summary.AverageScore is double average && summary.MaxScore > 0
                    ? average / summary.MaxScore
                    : double.MaxValue
            )
            .ToList();
    }

    private async Task<IReadOnlyDictionary<Ulid, double>> GetTaskAverageScoresAsync(Ulid id)
    {
        return await Context
            .GetDbSet<AssignedWorkAnswerModel>()
            .Where(answer => answer.AssignedWork.WorkId == id && answer.Score != null)
            .GroupBy(answer => answer.TaskId)
            .Select(group => new
            {
                TaskId = group.Key,
                Average = group.Average(answer => answer.Score!.Value),
            })
            .ToDictionaryAsync(entry => entry.TaskId, entry => entry.Average);
    }

    private IQueryable<AssignedWorkModel> AssignedWorksOf(Ulid workId)
    {
        return Context.GetDbSet<AssignedWorkModel>().Where(aw => aw.WorkId == workId);
    }

    public async Task<IEnumerable<WorkRelation>> GetWorkRelationsAsync(Ulid workId)
    {
        var assignments = await Context
            .GetDbSet<CourseWorkAssignmentModel>()
            .Where(assignment => assignment.WorkId == workId)
            .Include(assignment => assignment.CourseMaterialContent.Material.Chapter.Course)
            .ThenInclude(course => course.Chapters)
            .Include(assignment => assignment.CourseMaterialContent.Material.Chapter.Course)
            .ThenInclude(course => course.Subject)
            .AsSplitQuery()
            .ToListAsync();

        return assignments.ConvertAll(assignment => new WorkRelation
        {
            CourseId = assignment.CourseMaterialContent.Material.Chapter.CourseId,
            MaterialId = assignment.CourseMaterialContent.Material.Id,
            Subject = assignment.CourseMaterialContent.Material.Chapter.Course.Subject,
            Assignment = assignment,
        });
    }
}
```

За ключевой функционал, используемый в разных частях приложения (авторизация, проверка безопасности, парсинг данных и прочее) отвечает системный модуль Core, пример его использования:

```cs
using Noo.Api.Core.System.Events;

namespace Noo.Api.Core.System.Scheduling;

public class SchedulerHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ScheduledJobRegistry _registry;
    private readonly ILogger<SchedulerHostedService> _logger;

    public SchedulerHostedService(
        IServiceProvider services,
        ScheduledJobRegistry registry,
        ILogger<SchedulerHostedService> logger
    )
    {
        _services = services;
        _registry = registry;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken ct)
    {
        var loops = _registry.JobTypes.Select(type => RunLoopAsync(type, ct));
        return Task.WhenAll(loops);
    }

    // TODO: Rethink the loop, maybe use some lib or so... Timer is not the best option
    private async Task RunLoopAsync(Type jobType, CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(GetInterval(jobType));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var job = (IScheduledJob)scope.ServiceProvider.GetRequiredService(jobType);
                var collector = scope.ServiceProvider.GetRequiredService<IDomainEventCollector>();

                try
                {
                    await job.RunAsync(stoppingToken);
                    await collector.FlushAsync(stoppingToken);
                }
                catch
                {
                    collector.Discard();
                    throw;
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Scheduled job {Job} failed.", jobType.Name);
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private TimeSpan GetInterval(Type jobType)
    {
        using var scope = _services.CreateScope();
        return ((IScheduledJob)scope.ServiceProvider.GetRequiredService(jobType)).Interval;
    }
}
```

Вышепредставленный код отвечает за регистрацию и исполнение запланированных действий. Так как этот функционал нужен в практически всех частях программы (публикация курсов, удаление просроченных авторизаций, открытие и закрытие доступа к сущностям), он был вынесен в системный модуль Core.
