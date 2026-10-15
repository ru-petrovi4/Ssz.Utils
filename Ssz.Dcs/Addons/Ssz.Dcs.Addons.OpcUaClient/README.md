# Ssz.Dcs.Addons.OpcUaClient

Аддон-источник данных центрального сервера (Ssz.Dcs.CentralServer): OPC UA клиент, аналог аддона
`Ssz.Dcs.Addons.OpcClient` (OPC DA/AE/HDA). Каждый включённый экземпляр становится
сессией движка (EngineSession) системы DCS, как и остальные аддоны-наследники
`DataAccessProviderGetter_AddonBase`. Используется стек OPC Foundation
`OPCFoundation.NetStandard.Opc.Ua.Client` (лицензия MIT).

## Что поддерживается

| Центральный сервер | OPC UA |
| --- | --- |
| Подписка на значения (`ValueSubscription`) | Subscription + MonitoredItem (Value), один элемент на тег |
| Запись (`WriteAsync`) | Write; значение приводится к типу переменной (по последнему значению или атрибуту DataType) |
| События и алармы (`EventMessagesCallback`) | MonitoredItem на объекте-источнике событий (по умолчанию Server) с фильтром BaseEventType/ConditionType/AlarmConditionType; после подключения выполняется ConditionRefresh |
| Квитирование (`AckAlarms`) | Вызов метода Acknowledge условия (ConditionId и EventId берутся из `OccurrenceId`) |
| История значений (`ReadElementValuesJournalsAsync`) | HistoryRead ReadRawModified с обработкой continuation points |
| История событий (`ReadEventMessagesJournalAsync`) | HistoryRead ReadEvent на объекте-источнике событий |

Переподключение: при сбое keep-alive или закрытии сессии все подписки получают статус Uncertain,
сессия пересоздаётся раз в 10 секунд, элементы подписки создаются заново.

## Идентификаторы тегов

ElementId центрального сервера (после `map.csv`) преобразуется в NodeId так:

- строка вида `ns=2;s=Tag1`, `i=1001`, `s=Tag1`, `g=...`, `b=...`, `nsu=urn:...;s=Tag1` разбирается как NodeId;
- любая другая строка — строковый идентификатор в пространстве имён по умолчанию
  (`DefaultNamespaceUri`, если задан и найден на сервере, иначе `DefaultNamespaceIndex`).

`map.csv`/`tags.csv` в каталоге экземпляра работают как в остальных аддонах (константы, конвертеры,
составные теги).

## Сообщения событий

Событие OPC UA, у которого есть ConditionId (или тип — наследник ConditionType), превращается в
`EventMessage` типа SimpleAlarm с `AlarmMessageData`:

- `EventId.SourceElementId` — SourceName (или SourceNode);
- `EventId.Conditions[0].LocalId` — ConditionName;
- `EventId.OccurrenceId` — `<EventId hex>|<ConditionId>`, нужен для квитирования;
- `AlarmState` — Active/Unacked/Disabled/Suppressed из ActiveState, AckedState, EnabledState, SuppressedState;
- `TimeLastActive` — ActiveState/TransitionTime (или время события);
- `AlarmStateChange` — вычисляется по сравнению с предыдущим состоянием того же условия;
- `Priority` — Severity, `TextMessage` — Message.

Остальные события — SystemEvent (AuditEventType — OperatorActionEvent). Все поля фильтра
дублируются в `EventMessage.Fields` (EventType, SourceNode, ConditionId, Retain, ...).

## Настройка

Опции в `CsvDb/OpcUaClient.1/options.csv`:

| Опция | По умолчанию | Описание |
| --- | --- | --- |
| `%(OpcUaClient_EndpointUrl)` | `opc.tcp://localhost:4840` | Адрес сервера |
| `%(OpcUaClient_UseSecurity)` | `true` | Выбирать самую защищённую конечную точку сервера; `false` — без защиты |
| `%(OpcUaClient_UserName)` / `%(OpcUaClient_Password)` | | Пусто — анонимно; пароль можно взять из зашифрованных настроек: `%(yml:ключ)` |
| `%(OpcUaClient_AutoAcceptServerCertificate)` | `true` | Доверять сертификату сервера автоматически |
| `%(OpcUaClient_PkiDirectory)` | `OpcUa\pki` | Каталог хранилищ сертификатов, подкаталог по имени экземпляра |
| `%(OpcUaClient_DefaultNamespaceUri)` | | URI пространства имён тегов |
| `%(OpcUaClient_DefaultNamespaceIndex)` | `2` | Индекс пространства имён, если URI не задан |
| `%(OpcUaClient_PublishingIntervalMs)` | `500` | Интервал публикации |
| `%(OpcUaClient_SamplingIntervalMs)` | `250` | Интервал опроса элементов |
| `%(OpcUaClient_SubscribeToEvents)` | `true` | Подписка на события |
| `%(OpcUaClient_EventNotifierNodeId)` | `i=2253` | Объект-источник событий |
| `%(OpcUaClient_SessionTimeoutMs)` | `60000` | Таймаут сессии |

Сертификат клиента создаётся автоматически в `<PkiDirectory>/<InstanceId>/own`. Если сервер
не доверяет клиенту, его сертификат нужно перенести из `rejected` сервера в `trusted`.

## Потоки

Все вызовы сессии OPC UA выполняются в рабочей задаче провайдера (`WorkingThreadSafeDispatcher`),
уведомления стека приходят в его потоках и только читают словарь узлов под блокировкой, обновления
подписчиков и события доставляются через диспетчер колбэков (`CallbackDispatcher`), как и в остальных
провайдерах.
