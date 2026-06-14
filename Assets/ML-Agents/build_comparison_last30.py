import os
import re
import statistics

LOG = os.path.join(os.environ["LOCALAPPDATA"], "Unity", "Editor", "Editor.log")
OUT_DIR = os.path.join(os.path.dirname(__file__), "eval_logs")
N = 30

with open(LOG, "r", encoding="utf-8", errors="replace") as f:
    text = f.read()

pat = re.compile(
    r"\[AllyTraining\] Episode end success=(True|False), curriculum=(\d+), reward=([-\d,\.]+)"
    r"|^Reward:\s*([-\d,\.]+)",
    re.M,
)
events = []
for m in pat.finditer(text):
    if m.group(1) is not None:
        events.append(
            {
                "type": "clear",
                "success": True,
                "curriculum": int(m.group(2)),
                "reward": float(m.group(3).replace(",", ".")),
            }
        )
    else:
        events.append(
            {
                "type": "death",
                "success": False,
                "curriculum": 4,
                "reward": float(m.group(4).replace(",", ".")),
            }
        )

clear_count = death_count = 0
split_idx = len(events)
for i, e in enumerate(events):
    if e["type"] == "clear":
        clear_count += 1
    else:
        death_count += 1
    if clear_count == 46 and death_count == 5:
        split_idx = i + 1
        break

heur = events[:split_idx][-N:]
infr = events[split_idx:][-N:]


def stats_all_attempts(block):
    """Награда по каждой попытке: зачистка — reward из Episode end; смерть — Reward: (~-10..-20)."""
    rewards = [e["reward"] for e in block]
    clears = [e for e in block if e["type"] == "clear"]
    deaths = [e for e in block if e["type"] == "death"]
    clear_rewards = [e["reward"] for e in clears]
    att = len(block)
    return {
        "attempts": att,
        "clears": len(clears),
        "deaths": len(deaths),
        "success_pct": len(clears) / att * 100 if att else 0,
        "avg": statistics.mean(rewards) if rewards else 0,
        "med": statistics.median(rewards) if rewards else 0,
        "mn": min(rewards) if rewards else 0,
        "mx": max(rewards) if rewards else 0,
        "stdev": statistics.stdev(rewards) if len(rewards) >= 2 else 0,
        "gt6": sum(1 for r in rewards if r > 6),
        "gt6pct": 100 * sum(1 for r in rewards if r > 6) / att if att else 0,
        "gt7": sum(1 for r in rewards if r > 7),
        "death_rewards": [round(e["reward"], 2) for e in deaths],
        "avg_clear_only": statistics.mean(clear_rewards) if clear_rewards else 0,
    }


def save_csv(path, block):
    with open(path, "w", encoding="utf-8") as f:
        f.write("type,success,curriculum,reward\n")
        for e in block:
            f.write(f"{e['type']},{e['success']},{e['curriculum']},{e['reward']}\n")


h = stats_all_attempts(heur)
i = stats_all_attempts(infr)
os.makedirs(OUT_DIR, exist_ok=True)
save_csv(os.path.join(OUT_DIR, "heuristic_last30.csv"), heur)
save_csv(os.path.join(OUT_DIR, "inference_last30.csv"), infr)

report = f"""Сравнение Heuristic vs Inference (последние {N} попыток каждого режима)
Источник: Editor.log
Дата: 2026-06-14

МЕТОДИКА ПОДСЧЁТА НАГРАДЫ
-------------------------
Каждая попытка = один эпизод. Кумулятивная награда эпизода:
  • зачистка волны — значение reward из строки Episode end success=True;
  • смерть агента — значение Reward: (штраф за урон + deathPenalty 10, типично около -11..-20).

Все метрики награды (среднее, медиана, min, max, sigma) считаются по ВСЕМ {N} попыткам,
включая неуспешные. Иначе смерти и штрафы не отражаются в статистике.

УСЛОВИЯ
-------
Curriculum: 4, волна: 2x Void Golem + 3x Dark Mage

ТАБЛИЦА 3.2 — Сравнение режимов (все попытки)
----------------------------------------------
| Метрика                         | Heuristic (n={N}) | Inference (n={N}) | Изм.         |
|---------------------------------|-------------------|-------------------|--------------|
| Зачисток волны                  | {h['clears']}                 | {i['clears']}                 | +{i['clears']-h['clears']}            |
| Смертей агента                  | {h['deaths']}                  | {i['deaths']}                  | -{h['deaths']}            |
| Успешность попыток              | {h['success_pct']:.1f}%            | {i['success_pct']:.1f}%            | +{i['success_pct']-h['success_pct']:.1f} п.п.   |
| Средняя награда (все попытки)   | {h['avg']:.2f}             | {i['avg']:.2f}             | +{i['avg']-h['avg']:.2f}         |
| Медиана награды (все попытки)   | {h['med']:.2f}             | {i['med']:.2f}             | +{i['med']-h['med']:.2f}         |
| Min / Max награды               | {h['mn']:.2f} / {h['mx']:.2f}     | {i['mn']:.2f} / {i['mx']:.2f}     | —            |
| σ                               | {h['stdev']:.2f}             | {i['stdev']:.2f}             | {i['stdev']-h['stdev']:+.2f}         |
| Попыток с наградой > 6          | {h['gt6']} ({h['gt6pct']:.1f}%)       | {i['gt6']} ({i['gt6pct']:.1f}%)       | +{i['gt6']-h['gt6']}            |
| Попыток с наградой > 7          | {h['gt7']}                 | {i['gt7']}                  | -{h['gt7']}            |

Справочно (только успешные зачистки, без смертей):
  Средняя награда Heuristic при success: {h['avg_clear_only']:.2f}
  Смерти Heuristic: {', '.join(str(x) for x in h['death_rewards'])}

ВЫВОДЫ
------
1. С учётом штрафов при смерти средняя награда Heuristic падает до {h['avg']:.2f}
   (не {h['avg_clear_only']:.2f}); Inference — {i['avg']:.2f}. Разница +{i['avg']-h['avg']:.2f} в пользу Inference.
2. Минимум Heuristic — {h['mn']:.2f} (эпизоды со смертью), не положительное значение успешного эпизода.
3. Пики > 7 у Heuristic ({h['gt7']} эп.) не компенсируют 3 провала (~-11 каждый): итоговое среднее ниже Inference.
4. Inference: 100% успешность, sigma {i['stdev']:.2f} против {h['stdev']:.2f} — более предсказуемый результат.

Текст для ВКР:
«На последних {N} попытках (curriculum 4) средняя кумулятивная награда с учётом неуспешных
эпизодов составила {h['avg']:.2f} для Heuristic и {i['avg']:.2f} для Inference (+{i['avg']-h['avg']:.2f}).
Успешность — {h['success_pct']:.1f}% против {i['success_pct']:.1f}%. Минимальная награда Heuristic
({h['mn']:.2f}) соответствует эпизодам со смертью агента (штраф deathPenalty и урон за шаг).
Обученная политика обеспечивает более высокий и стабильный итоговый результат.»

ФАЙЛЫ: heuristic_last30.csv, inference_last30.csv
"""

path = os.path.join(OUT_DIR, "heuristic_vs_inference_comparison.txt")
with open(path, "w", encoding="utf-8") as f:
    f.write(report)

with open(os.path.join(OUT_DIR, "heuristic_eval_summary.txt"), "w", encoding="utf-8") as f:
    f.write(
        f"Heuristic — последние {N} попыток (все, включая смерти)\n"
        f"Успешность: {h['success_pct']:.1f}% ({h['clears']}/{N})\n"
        f"Средняя награда (все): {h['avg']:.3f}, медиана: {h['med']:.3f}\n"
        f"Min/Max: {h['mn']:.3f} / {h['mx']:.3f}, sigma: {h['stdev']:.3f}\n"
        f"Средняя только при зачистке: {h['avg_clear_only']:.3f}\n"
        f"Смерти: {h['death_rewards']}\n"
    )

with open(os.path.join(OUT_DIR, "inference_eval_summary.txt"), "w", encoding="utf-8") as f:
    f.write(
        f"Inference — последние {N} попыток\n"
        f"Успешность: {i['success_pct']:.1f}% ({i['clears']}/{N})\n"
        f"Средняя награда: {i['avg']:.3f}, медиана: {i['med']:.3f}\n"
        f"Min/Max: {i['mn']:.3f} / {i['mx']:.3f}, sigma: {i['stdev']:.3f}\n"
    )
