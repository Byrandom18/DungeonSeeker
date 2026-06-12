# DungeonSeeker — Handoff: ИИ союзников + ML движение

**Workspace:** `C:\Unity\DungeonSeeker\Assets\Scripts`  
**Обновлено:** 2026-06-08  
**Правило для агента:** граф `Ally AI.asset` **не редактировать из кода** — только инструкции пользователю, что менять в Unity Editor.

---

## Проект

DungeonSeeker — 2D roguelike RPG, вид сверху. В одиночке — боты-союзники с тем же стеком, что у игрока.

**Стек бота:** `PlayerStats` + `EquipmentComponent` + `AbilitySystem` + `ActiveWeapon` + `NavMeshAgent` + `BehaviorGraphAgent` (`Ally AI.asset`).

---

## Архитектура ИИ (гибрид)

```
Unity Behavior (оркестрация: идти / бить / кастовать / retreat)
    ↓
AllyAIBrain (хаб решений)
    ├─ Utility AI: WeaponTargetSelector, UtilityAbilitySelector
    ├─ CombatPerception → CombatSnapshot
    ├─ ThreatPerception → ThreatSnapshot (снаряды, IsAttacking)
    └─ AllyMLBridge ← AllyPositionAgent (ML-Agents)
         ↓
    AllyMLMovementModifier (NavMesh: кольцо дистанции, стрейф, escape, follow)
         + AllyDodgeHeuristic (реактивный уворот от снарядов)
```

| Слой | Ответственность |
|------|-----------------|
| Utility AI | Цель для оружия, выбор способности, aim |
| Unity Behavior | Когда атаковать / кастовать / navigate / in combat |
| ML-Agents | preferredDistance, retreatUrgency, strafeDirection, strafeIntensity |
| AllyMLMovementModifier | Исполнение движения в бою (не Navigate/Retreat) |

**Удалено:** `AllyAIController`, `AllyAbilityBrain` — всё через Behavior + `AllyAIBrain`.

---

## ML-Agents: AllyPosition

| Параметр | Значение |
|----------|----------|
| Behavior Name | `AllyPosition` |
| Vector Observations | **17** |
| Continuous Actions | **4** |
| Decision Period | **5** |
| Unity package | `com.unity.ml-agents: 4.0.2` |
| PPO config | `ml-agents/config/ally_position.yaml` |
| Train script | `ml-agents/train_ally_position.ps1` |
| Training scene | `Assets/Scenes/ML_Training_Arena.unity` |
| Prefab | `Assets/Prefabs/Player/AllyEntity.prefab` |

### Observations (17)

| # | Поле |
|---|------|
| 0–11 | HP%, MP%, shield%, enemy count, nearest dist, cluster, ally low HP, enemy in combat, leader dist, attack dist, profile, weapon type |
| 12 | Nearest enemy `IsAttacking` |
| 13 | Bearing до ближайшего врага (0–1) |
| 14 | Incoming threat urgency |
| 15–16 | Threat direction X/Y |

### Actions (4)

| # | Поле | Эффект |
|---|------|--------|
| 0 | preferredDistance | 0.5×–1.3× weapon range → `AttackDistance` |
| 1 | retreatUrgency | ≥0.75 → `ShouldForceRetreat` |
| 2 | strafeDirection | 0–1 → lateral −1…+1 |
| 3 | strafeIntensity | сила стрейфа |

### Rewards (AllyPositionAgent)

- Штраф за шаг, награда за дистанцию до `AttackDistance`
- Штраф за отрыв от лидера (>6 без врагов)
- Урон / щит / килл / смерть / clear wave
- Бонус: нет урона пока враг атакует
- Бонус: near-miss при высокой угрозе снаряда

---

## AllyMLMovementModifier — режимы движения

Управляется из **`AllyMLCombatMovementAction`** (граф) + `Update()` при `IsControllingMovement`.

| Режим | Условие | Поведение |
|-------|---------|-----------|
| **EscapeToAlly** | Гистерезис low HP (см. ниже) | Держаться рядом с лидером/союзниками, короткий отход от врага |
| **FollowLeader** | Дистанция до лидера > `MaxDistToLeader` | Догон игрока |
| **CombatPositioning** | Есть цель, HP в норме | Кольцо `AttackDistance` + ML-стрейф |

**Уклонение** (`AllyDodgeHeuristic` + `ThreatPerception`) — во **всех** режимах.

### Гистерезис low HP

- **Вход в escape:** HP ≤ `LowHealthThreshold` (blackboard, default 20%)
- **Выход в бой:** HP ≥ `LowHealthThreshold + LowHealthHysteresis` (default +30% → выход при 50%)
- Флаг `_isInLowHealthEscape` — без дёрганья на границе

### Параметры на компоненте (Inspector)

| Поле | Default | Смысл |
|------|---------|-------|
| `LowHealthHysteresis` | 30 | +% к порогу выхода |
| `AllyHoldRadius` | 2.5 | «рядом с союзником» |
| `MaxEscapeDistanceFromParty` | 5 | дальше — принудительно к партии |
| `EnemyThreatRadiusDuringEscape` | 3.5 | короткий шаг от врага |
| `EscapeApproachStep` | 1.2 | сближение с партией |
| `HoldRetreatStep` | 0.75 | короткий отход |

---

## Ключевые файлы

| Путь | Роль |
|------|------|
| `AI/AllyAIBrain.cs` | Центральный хаб, `AttackDistance` с ML-масштабом |
| `AI/Perception/CombatPerception.cs` | Снимок боя |
| `AI/Perception/ThreatPerception.cs` | Снаряды врага, `IsAttacking` |
| `AI/Perception/ThreatSnapshot.cs` | Struct угроз |
| `AI/Targeting/WeaponTargetSelector.cs` | Utility-скоринг цели |
| `AI/Abilities/UtilityAbilitySelector.cs` | Выбор способности |
| `AI/AllyAIProfile.cs` | Профили Aggressive / Defensive / Support |
| `Behavior/ChooseEnemyAction.cs` | Цель + sync blackboard |
| `Behavior/AllyMLCombatMovementAction.cs` | Behavior-нода ML-движения |
| `Behavior/NavigateToTarget2DAction.cs` | No-op при `IsControllingMovement` |
| `Behavior/RetreatAction.cs` | No-op при `IsControllingMovement` |
| `AI/ML/AllyPositionAgent.cs` | ML obs/reward/actions |
| `AI/ML/AllyMLBridge.cs` | ML → distance / retreat / strafe |
| `AI/ML/AllyMLMovementModifier.cs` | NavMesh combat movement |
| `AI/ML/AllyMLMovementMode.cs` | Enum режимов |
| `AI/ML/AllyDodgeHeuristic.cs` | Реактивный dodge |
| `AI/ML/AllyMLRewardForwarder.cs` | Награды из боя |
| `AI/ML/Training/AllyTrainingEnvironment.cs` | Эпизоды, волны, curriculum |
| `AI/ML/Training/AllyTrainingEnemySpawner.cs` | Спавн врагов |
| `AI/ML/Training/AllyTrainingCurriculumHook.cs` | `curriculum_level` из Python |
| `AI/ML/Training/AllyTrainingLeaderDummy.cs` | Патруль лидера в training |
| `Projectiles/EnemyProjectileRegistry.cs` | Реестр вражеских снарядов |
| `AI/Debug/AllyAIDebugOverlay.cs` | Overlay: ML, mode, escape until X% |
| `Behavior/Ally AI.asset` | Граф поведения (**правки только в Editor**) |

---

## Настройка графа (вручную в Unity)

### Боевая ветка (In Combat → Parallel → Movement Selector)

1. В **Selector движения** оставить только **`Ally ML Combat Movement`**
2. Удалить/отключить в этом Selector: Sequence с **Retreat (Escape)** и Sequence с **Navigate к врагу**
3. Parallel **Attack + Ability** — не трогать

### Blackboard на `Ally ML Combat Movement`

| Поле ноды | Blackboard variable |
|-----------|---------------------|
| Agent | `Self` |
| Target | `Enemy` |
| Leader | `Player` |
| HealthPercent | `Health percent` |
| LowHealthThreshold | `LowHealthThreshold` (20) |
| MaxDistToLeader | `MaxDistToLeader` (10) |
| LowHealthHysteresis | `LowHealthHysteresis` (30) — опционально |

### ChooseEnemy → blackboard

- `Health percent` ← HealthPercent  
- `Attack Distance` ← AttackDistance  
- `RetreatUrgency` ← RetreatUrgency (если используется)

---

## Настройка ML_Training_Arena (вручную)

| Объект | Проверить |
|--------|-----------|
| NavMeshSurface | NavMesh запечён |
| AllyTrainingEnvironment | spawns, spawner, `_trainingAgent` → AllyEntity |
| AllyTrainingEnemySpawner | Slime + Dark Mage prefabs |
| AllyTrainingCurriculumHook | на том же GO |
| **PartyManager** | GO с компонентом (может отсутствовать — добавить) |
| Player | `_registerAsPrimaryPlayer = true`, `AllyTrainingLeaderDummy` |
| Environment `_leaderDummy` | → компонент на Player (**сейчас часто null — подключить**) |
| AllyEntity | Behavior Parameters: 17 obs, 4 actions, Name `AllyPosition` |

---

## Запуск обучения (кратко)

### 1. Python (один раз)

```powershell
cd C:\Unity\DungeonSeeker
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install mlagents
```

### 2. Heuristic smoke-test

- Scene: `ML_Training_Arena`
- Behavior Parameters → **Heuristic Only**
- Ally ML Bridge → **Heuristic**
- Play → проверить спавн, `ML move: ON`, эпизоды

### 3. PPO

```powershell
.\ml-agents\train_ally_position.ps1 -RunId "ally_position_v2_strafe" -TimeScale 20
```

Unity:
- Behavior Parameters → **Default**, Model = None
- Play

### 4. После обучения

- Экспорт `.onnx` из `results/<run-id>/AllyPosition/`
- Prefab: Model → onnx, Behavior Type → **Inference Only**, Bridge Mode → **Inference**

Подробный чеклист — см. историю чата «порядок действий для начала обучения».

---

## Curriculum (ally_position.yaml)

| Lesson | Enemies | Prefab index |
|--------|---------|--------------|
| 0 | 1 | 0 (Slime) |
| 1 | 2 | 1 |
| 2 | 3 | 1 |
| 3 | 4+ | 1 (Dark Mage) |

---

## AllyMLMode

| Mode | Когда |
|------|-------|
| Disabled | ML выкл |
| Heuristic | Тест без модели |
| Training | PPO (ResetEpisode ставит автоматически) |
| Inference | ONNX в игре |

---

## Компоненты на префабе AllyEntity

Обязательные: `AllyAIBrain`, `CombatPerception`, `ThreatPerception`, `AllyMLBridge`, `AllyMLMovementModifier`, `AllyDodgeHeuristic`, `AllyPositionAgent`, `AllyMLRewardForwarder`, `DecisionRequester`, `Behavior Parameters`, `BehaviorGraphAgent`, `NavMeshAgent`.

Союзник: `PlayerStats._registerAsPrimaryPlayer = false`.  
Training-лидер: Player с `true`.

---

## Известные проблемы / ограничения

- **Parallel Attack + Move** — атака параллельно движению; возможна осцилляция на границе Attack Distance (если в графе остались старые Navigate/Retreat).
- **`RetreatUrgency`** в графе не wired как условие — только `ShouldForceRetreat` в коде Retreat (no-op при ML).
- **`_leaderDummy` null** в сцене — лидер не ресетится между эпизодами.
- **PartyManager** может отсутствовать в training-сцене.
- **ONNX** в репозитории нет — обучение не завершено / не экспортировано.
- Navigate/Retreat в **небоевой** ветке графа — для follow без врагов (норма).

---

## Сделано

- Utility AI: цели оружия и способностей, focus fire
- Способности: Heal, Shield, GroundAoE; Behavior-ноды
- ML: 17 obs / 4 actions, strafe, threat perception, dodge heuristic
- `AllyMLMovementModifier`: combat ring, escape с гистерезисом, follow leader, hold near allies
- `AllyMLCombatMovementAction` + training infrastructure
- `EnemyProjectileRegistry`, rewards shaping
- Debug overlay с mode / escape threshold

---

## Запланировано (future)

- [ ] Первый полный прогон PPO + экспорт ONNX
- [ ] A/B: Heuristic vs Inference в training scene
- [ ] Dark Mage в curriculum spawner (lesson 3+)
- [ ] Гистерезис Engage/Retreat Distance в графе (если вернуть Navigate)
- [ ] Классовые `AllyAIProfile` на префабах
- [ ] CooldownReduction в AbilityBase
- [ ] Координация focus fire между несколькими ботами

---

## Принципы для нового агента

1. **Не учить ML** выбор цели/способности — это Utility AI + Behavior.
2. **Не редактировать граф `.asset`** — только описывать шаги для Editor.
3. Один префаб для игры и training; режим через `AllyMLBridge.Mode` / Behavior Parameters.
4. Движение в бою — через `AllyMLMovementModifier`, не через Navigate/Retreat в боевом Selector.
5. Минимальный diff; не переписывать граф с нуля.

---

## С чего начать новому агенту

1. Прочитать `AllyAIBrain.cs`, `AllyMLMovementModifier.cs`, `AllyPositionAgent.cs`, `AllyMLCombatMovementAction.cs`.
2. Если задача — **training:** проверить сцену + wiring blackboard (см. выше).
3. Если задача — **поведение escape/follow:** правки в `AllyMLMovementModifier.cs`.
4. Если задача — **стабильность:** гистерезис + убрать дубли Navigate/Retreat в графе (инструкции пользователю).
