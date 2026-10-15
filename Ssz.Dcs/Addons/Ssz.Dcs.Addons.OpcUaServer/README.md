# Ssz.Dcs.Addons.OpcUaServer

Аддон центрального сервера (Ssz.Dcs.CentralServer), который публикует одну систему сервера
(DCS или сессию моделирования) как OPC UA сервер. Используется стек OPC Foundation
`OPCFoundation.NetStandard.Opc.Ua.Server` (лицензия MIT).

Аддон подключается к центральному серверу in-process через стандартный `GrpcDataAccessProvider`
(`LocalDataAccessServerWorker`), то есть является обычным клиентом сервера со своим контекстом.

## Что поддерживается

| OPC UA | Центральный сервер |
| --- | --- |
| Переменные (Read, Write, подписки / MonitoredItems) | ElementValueList, `ValueSubscription`, `WriteAsync` |
| События (BaseEventType) на корневой папке и объекте Server | EventList, `EventMessagesCallback` |
| Аларм-условия AlarmConditionType (ActiveState, AckedState, Retain, ConditionRefresh) | EventMessage с `AlarmMessageData` |
| Квитирование (метод Acknowledge) | `AckAlarms` |
| История значений (HistoryRead, ReadRawModified) | ElementValuesJournalList |
| История событий (HistoryRead, ReadEvent) на корневой папке | `ReadEventMessagesJournalAsync` |

## Адресное пространство

```
Objects
└── <SystemNameToConnect>            ns=2;s=/          (EventNotifier, история событий)
    ├── <папки из nodes.csv>          ns=2;s=/Папка/Подпапка
    │   └── <теги из nodes.csv>       ns=2;s=<ElementId>
    ├── Dynamic                       теги, запрошенные клиентами по NodeId
    └── Alarms                        объекты AlarmConditionType, по одному на (источник, условие)
```

NodeId любого тега — строка с ElementId в пространстве имён аддона:
`ns=2;s=TAG001.PV`. Тег не обязан быть описан в `nodes.csv`: при первом обращении
(Read, Write, MonitoredItem) узел создаётся автоматически в папке `Dynamic` и подписывается
на значение. Теги без подписок клиентов отписываются от источника по таймауту
(`IdleUnsubscribeSeconds`), динамические при этом удаляются из адресного пространства.

Тип данных переменной (`DataType`) уточняется по первому полученному значению.

## Настройка

Аддон появляется в `CsvDb/addons.csv` как `OpcUaServer.1` (многоэкземплярный — можно
поднять несколько серверов для разных систем на разных портах). Опции в
`CsvDb/OpcUaServer.1/options.csv`:

| Опция | По умолчанию | Описание |
| --- | --- | --- |
| `%(OpcUaServer_EndpointUrl)` | `opc.tcp://localhost:4840/SszDcsCentralServer` | Адрес конечной точки; `localhost` заменяется на имя компьютера |
| `%(OpcUaServer_SystemNameToConnect)` | `DCS` | Система центрального сервера |
| `%(OpcUaServer_ContextParams)` | | Параметры контекста подключения |
| `%(OpcUaServer_SecurityPolicies)` | `None,Basic256Sha256,Aes128_Sha256_RsaOaep,Aes256_Sha256_RsaPss` | Политики безопасности (для всех, кроме None, создаются Sign и SignAndEncrypt) |
| `%(OpcUaServer_AllowAnonymous)` | `true` | Анонимный доступ |
| `%(OpcUaServer_UserName)` | | Имя пользователя (пусто — аутентификация по паролю отключена) |
| `%(OpcUaServer_Password)` | | Пароль; можно сослаться на зашифрованный ключ appsettings: `%(yml:ключ)` |
| `%(OpcUaServer_AutoAcceptClientCertificates)` | `true` | Автоматически доверять сертификатам клиентов |
| `%(OpcUaServer_PkiDirectory)` | `OpcUa\pki` | Каталог хранилищ сертификатов (относительно рабочего каталога сервера); для каждого экземпляра создаётся подкаталог с именем экземпляра |
| `%(OpcUaServer_IdleUnsubscribeSeconds)` | `120` | Таймаут отписки неиспользуемых тегов |

Сертификат приложения создаётся автоматически (самоподписанный) в `<PkiDirectory>/<InstanceId>/own`.
Сертификаты отклонённых клиентов (при `AutoAcceptClientCertificates = false`) попадают в `rejected`,
их нужно перенести в `trusted`.

### nodes.csv

Необязательный файл `CsvDb/OpcUaServer.1/nodes.csv` описывает статические (просматриваемые) узлы:

```
!ElementId,Path,DisplayName,Description
TAG001.PV,Unit1/Pumps,TAG001 PV,Pump 1 pressure
TAG001.SP,Unit1/Pumps,TAG001 SP,
TAG002.PV,Unit1,,
```

Колонки: ElementId (ключ), путь папок через `/`, отображаемое имя (по умолчанию ElementId), описание.
Файлы `map.csv` и `tags.csv` в том же каталоге работают как у остальных аддонов (`ElementIdsMap`).

## Потоки

Стек OPC UA вызывает менеджер узлов из своих потоков; провайдер данных вызывает обратно в
диспетчере аддона (поток `ServerWorker`). Все операции с `ValueSubscription` выполняются в
диспетчере, состояние узлов защищено `CustomNodeManager2.Lock`. Ожидание результатов записи,
первого значения при Read и чтения истории блокирует только поток запроса OPC UA.
