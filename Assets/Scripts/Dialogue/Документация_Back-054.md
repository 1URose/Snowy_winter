# Back-054: бросок кубика по кнопке

Логика проверки броском кубика в диалоге. Это только backend: здесь нет анимации кубика и нет интерфейса. UI подписывается на события `DiceCheck` и рисует кнопку и результат сам.

Ветка: `feature/Back-054_cube_throw_mechanics_on_button_trigger`.

## Правила

- Один кубик d6. 1–3 — провал (`"fail"`), 4–6 — успех (`"success"`). Порог задан константой `DiceCheck.SuccessThreshold`.
- Одна попытка на проверку, переброса нет.
- Результат детерминирован: он зависит только от `playthroughSeed` прохождения и `checkId`. Поэтому если загрузить сейв до броска и бросить снова, выпадет то же число.

## Файлы

| Файл | Что это |
|---|---|
| `DiceCheck.cs` | Логика проверки: открыть, бросить, записать результат в сохранение |
| `IDiceSource.cs` | Источник значения кубика. В тестах подставляется фейк |
| `SeededDiceSource.cs` | Рабочий источник: стабильный хеш от `playthroughSeed` и `checkId` |
| `Assets/Tests/Editor/Dialogue/DiceCheckTests.cs` | 20 EditMode-тестов |
| `Assets/Tests/Editor/Dialogue/SeededDiceSourceTests.cs` | 5 EditMode-тестов |

Изменения в `SaveSystem`:

- В `GameStateData` добавлено поле `playthroughSeed`. Оно сохраняется вместе с прохождением.
- `GameStateManager.StartNew()` генерирует новый `playthroughSeed`. Новую игру нужно начинать через `StartNew()`, иначе seed останется 0 и броски будут одинаковыми во всех таких прохождениях.

## DiceCheck

| Член | Что делает |
|---|---|
| `Open(checkId)` | Открывает проверку и вызывает `Opened`. Если по этому `checkId` уже есть результат, проверка не открывается, а сразу вызывается `Rolled` с сохранённым результатом |
| `Roll()` | Бросает кубик, записывает результат в сохранение, закрывает проверку и вызывает `Rolled` |
| `IsOpen` | Открыта ли проверка сейчас |
| `CheckId` | Id текущей проверки (пустая строка, если проверка закрыта) |
| `Opened` | Событие: пора показать кнопку «Бросить» |
| `Rolled` | Событие с `CheckResultData`: пора показать результат |

### Что происходит при броске

1. В `GameStateData.checkResults` добавляется `CheckResultData`: `checkId`, `value` (1–6) и `outcomeId` (`"success"` или `"fail"`).
2. Проверка закрывается.
3. Состояние игры помечается как изменённое (`GameStateManager.MarkDirty()`).
4. Вызывается `Rolled`. К этому моменту результат уже лежит в состоянии, а внутри обработчика можно сразу открыть следующую проверку.

### Ошибки (бросаются сразу)

| Ситуация | Исключение |
|---|---|
| `new DiceCheck(null, ...)` или `new DiceCheck(..., null)` | `ArgumentNullException` |
| Пустой или `null` `checkId` | `ArgumentException` |
| `Open`, когда проверка уже открыта | `InvalidOperationException` |
| `Roll`, когда проверка закрыта (в том числе второй бросок подряд) | `InvalidOperationException` |
| Источник кубика вернул число вне 1–6 | `InvalidOperationException`, сохранение не меняется |

## Как этим будет пользоваться UI

```csharp
DiceCheck check = new DiceCheck(gameStateManager, new SeededDiceSource());

check.Opened += () => ShowRollButton();            // кнопка вызывает check.Roll()
check.Rolled += result => PlayDiceAnimation(result.value, result.outcomeId);

check.Open("lock-pick");
```

Важно: прячьте кнопку сразу после нажатия или проверяйте `check.IsOpen` перед `Roll`. Иначе двойное нажатие вызовет исключение.

`Rolled` может прийти и без `Opened`, если проверку уже проходили. Тогда UI сразу показывает результат без кнопки.

## Ветвление по результату

Механизма ветвления диалога по результатам (ни по `choices`, ни по `checkResults`) в проекте пока нет. Результат проверки просто записан в состояние, ветвление будет отдельной задачей.

## Не входит в задачу

Модификаторы, несколько кубиков, криты, ветвление диалога, сам интерфейс и анимация.

## Тесты

Window → General → Test Runner → EditMode → `SnowyWinter.Dialogue.Tests` и `SnowyWinter.SaveSystem.Tests`.
