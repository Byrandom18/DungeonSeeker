import os
import re
import statistics
from collections import defaultdict

log_path = os.path.join(os.environ["LOCALAPPDATA"], "Unity", "Editor", "Editor.log")
out_path = os.path.join(os.path.dirname(__file__), "eval_logs", "editor_log_parsed.txt")

with open(log_path, "r", encoding="utf-8", errors="replace") as f:
    text = f.read()

episode_re = re.compile(
    r"\[AllyTraining\] Episode end success=(True|False), curriculum=(\d+), reward=([-\d,\.]+)"
)
spawn_re = re.compile(
    r"\[AllyTraining\] Episode spawn: curriculum=(\d+), enemies=(\d+), wave=(.*)"
)
death_re = re.compile(r"^Reward:\s*([-\d,\.]+)", re.M)

episodes = []
for m in episode_re.finditer(text):
    reward = float(m.group(3).replace(",", "."))
    episodes.append(
        {
            "success": m.group(1) == "True",
            "curriculum": int(m.group(2)),
            "reward": reward,
        }
    )

deaths = [float(m.group(1).replace(",", ".")) for m in death_re.finditer(text)]
spawns = spawn_re.findall(text)

lines_out = []
lines_out.append(f"LOG: {log_path}")
lines_out.append(f"File size bytes: {os.path.getsize(log_path)}")
lines_out.append(f"Episode end lines: {len(episodes)}")
lines_out.append(f"Death (Reward:) lines: {len(deaths)}")
lines_out.append(f"Spawn lines: {len(spawns)}")
lines_out.append("")

if episodes:
    by_curr = defaultdict(list)
    for e in episodes:
        by_curr[e["curriculum"]].append(e)

    lines_out.append("By curriculum:")
    for lvl in sorted(by_curr):
        arr = by_curr[lvl]
        rewards = [e["reward"] for e in arr]
        succ = sum(1 for e in arr if e["success"])
        lines_out.append(
            f"  level {lvl}: episodes={len(arr)}, success={succ}, "
            f"avg_reward={statistics.mean(rewards):.3f}, "
            f"min={min(rewards):.3f}, max={max(rewards):.3f}"
        )

    rewards_all = [e["reward"] for e in episodes]
    succ_all = sum(1 for e in episodes if e["success"])
    lines_out.append("")
    lines_out.append("Overall:")
    lines_out.append(f"  success_rate={succ_all / len(episodes) * 100:.1f}%")
    lines_out.append(f"  avg_reward={statistics.mean(rewards_all):.3f}")
    if len(rewards_all) >= 2:
        lines_out.append(f"  stdev_reward={statistics.stdev(rewards_all):.3f}")
    lines_out.append("  last 10 episodes:")
    for e in episodes[-10:]:
        lines_out.append(
            f"    curr={e['curriculum']} reward={e['reward']:.3f} success={e['success']}"
        )

lines_out.append("")
lines_out.append("Keyword mentions:")
for pat in ["Heuristic", "Inference", "Training", "onnx", "ONNX", "IsCommunicatorOn"]:
    c = len(re.findall(re.escape(pat), text, re.I))
    if c:
        lines_out.append(f"  {pat}: {c}")

lines_out.append("")
lines_out.append("Last 20 relevant lines:")
for ln in text.splitlines():
    s = ln.strip()
    if "[AllyTraining]" in s or s.startswith("Reward:"):
        lines_out.append("  " + s[:250])

relevant = [ln.strip() for ln in text.splitlines() if "[AllyTraining]" in ln or ln.strip().startswith("Reward:")]
for ln in relevant[-20:]:
    lines_out.append("  " + ln[:250])

os.makedirs(os.path.dirname(out_path), exist_ok=True)
report = "\n".join(lines_out)
with open(out_path, "w", encoding="utf-8") as f:
    f.write(report)

print(report)
