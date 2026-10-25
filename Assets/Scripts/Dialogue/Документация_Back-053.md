# Back-053: кнопки и меню выбора ответа

Логика меню выбора ответа в диалоге. Это только backend: здесь нет текстов реплик и нет интерфейса. UI подписывается на события `ChoiceMenu` и рисует кнопки сам.

Ветка: `feature/Back-053_buttons_for_menu_and_answer_choosing`.

## Файлы

| Файл | Что это |
|---|---|
| `ChoiceMenu.cs` | Логика меню: открыть выбор, принять ответ, записать его в сохранение |
| `ChoiceOption.cs` | Один вариант ответа (одна кнопка) |
| `SnowyWinter.Dialogue.asmdef` | Сборка модуля, зависит от `SnowyWinter.SaveSystem` |
| `Assets/Tests/Editor/Dialogue/ChoiceMenuTests.cs` | 18 EditMode-тестов |

## ChoiceOption

Вариант ответа хранит только идентификаторы:

- `Id` — стабильный id варианта. Именно он попадает в сохранение (`optionId`).
- `TextKey` — ключ текста в контенте диалога. Реплики в коде не пишутся.

## ChoiceMenu

| Член | Что делает |
|---|---|
| `Open(choiceId, options)` | Открывает меню с вариантами и вызывает событие `Opened` |
| `Choose(optionId)` | Записывает выбор в сохранение, закрывает меню и вызывает событие `OptionChosen` |
| `IsOpen` | Открыто ли меню сейчас |
| `ChoiceId` | Id текущего выбора (пустая строка, если меню закрыто) |
| `Options` | Варианты текущего выбора, в порядке показа |
| `Opened` | Событие: меню открылось, пора рисовать кнопки |
| `OptionChosen` | Событие: игрок выбрал вариант, пора прятать кнопки |

### Что происходит при выборе

1. В `GameStateData.choices` записывается `ChoiceData`: `choiceId`, `optionId` и текущий `checkpointId`.
2. Если на этот `choiceId` уже отвечали, старый ответ заменяется. Ответы на другие выборы не трогаются.
3. Состояние игры помечается как изменённое (`GameStateManager.MarkDirty()`), так что выбор попадёт в следующее сохранение.
4. Меню закрывается, затем вызывается `OptionChosen`. Поэтому внутри обработчика можно сразу открыть следующий выбор.

### Ошибки (бросаются сразу)

| Ситуация | Исключение |
|---|---|
| `new ChoiceMenu(null)` | `ArgumentNullException` |
| Пустой или `null` `choiceId` | `ArgumentException` |
| Пустой или `null` список вариантов | `ArgumentException` |
| Вариант `null`, без `Id` или с повторяющимся `Id` | `ArgumentException` |
| `Open`, когда меню уже открыто | `InvalidOperationException` |
| `Choose`, когда меню закрыто (в том числе второй выбор подряд) | `InvalidOperationException` |
| `Choose` с id, которого нет среди вариантов | `ArgumentException`, сохранение не меняется |

При ошибке в `Open` меню остаётся закрытым.

## Как этим будет пользоваться UI

```csharp
ChoiceMenu menu = new ChoiceMenu(gameStateManager);

menu.Opened += () => DrawButtons(menu.Options);   // каждая кнопка вызывает menu.Choose(option.Id)
menu.OptionChosen += _ => HideButtons();

menu.Open("q-wolf", new[]
{
    new ChoiceOption("ask", "wolf.ask"),
    new ChoiceOption("leave", "wolf.leave"),
});
```

Текст на кнопке UI берёт из контента по `option.TextKey`.

Важно: прячьте кнопки сразу после нажатия или проверяйте `menu.IsOpen` перед `Choose`. Иначе двойное нажатие вызовет исключение.

## Не входит в задачу

Скрытие вариантов по флагам, проверки навыков, граф диалога, подстановка текста по `TextKey`, сам интерфейс.

## Тесты

Window → General → Test Runner → EditMode → `SnowyWinter.Dialogue.Tests`.
