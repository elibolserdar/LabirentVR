"""Unity VMWT CSV dosyalarından ilk kontrol grafiklerini üretir."""
from __future__ import annotations
import argparse
import re
import sys
from pathlib import Path

import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
from matplotlib.patches import Circle

PHASE_ORDER = {"Exploration": 0, "Hidden": 1, "Probe": 2, "DualBlock": 3, "Visible": 4}
TRIAL_REQUIRED = {"participant_id", "group", "phase", "trial_no", "latency_s",
                  "path_length_m", "path_length_norm", "found_platform"}
PATH_REQUIRED = {"participant_id", "phase", "trial_no", "sample_no", "time_s",
                 "pool_relative_x_m", "pool_relative_z_m"}


def read_csv(path: Path) -> pd.DataFrame:
    try:
        data = pd.read_csv(
            path, sep=";", encoding="utf-8-sig",
            dtype={"participant_id": "string", "group": "string", "session": "string",
                   "phase": "string", "start_position": "string",
                   "platform_quadrant": "string"})
    except Exception as exc:
        raise RuntimeError(f"CSV dosyası okunamadı: {path}\nAyrıntı: {exc}") from exc
    data.columns = [str(column).strip() for column in data.columns]
    return data


def default_data_dir() -> Path:
    home = Path.home()
    candidates = [home / "Documents" / "VMWT_Data",
                  home / "OneDrive" / "Documents" / "VMWT_Data",
                  home / "Belgeler" / "VMWT_Data",
                  home / "OneDrive" / "Belgeler" / "VMWT_Data"]
    return next((p for p in candidates if p.is_dir()), candidates[0])


def newest_trials_file(data_dir: Path) -> Path:
    candidates = [p for p in data_dir.glob("*.csv")
                  if not p.name.lower().endswith("_path_points.csv")]
    if not candidates:
        raise FileNotFoundError(
            f"{data_dir} içinde deneme CSV dosyası bulunamadı. Doğru klasörü --data-dir ile belirtin.")
    return max(candidates, key=lambda p: p.stat().st_mtime)


def paired_path_file(trial_file: Path) -> Path:
    path_file = trial_file.with_name(f"{trial_file.stem}_path_points.csv")
    if not path_file.exists():
        raise FileNotFoundError(f"Eşleşen rota dosyası bulunamadı: {path_file}")
    return path_file


def safe_name(value: object) -> str:
    name = re.sub(r"[^A-Za-z0-9_-]+", "_", str(value).strip())
    return name.strip("_") or "unknown"


def add_session(data: pd.DataFrame) -> pd.DataFrame:
    if "session" not in data.columns:
        data = data.copy()
        data["session"] = "UnknownSession"
    return data


def plot_trajectories(paths: pd.DataFrame, output_dir: Path) -> int:
    paths = add_session(paths)
    keys = ["participant_id", "session", "phase", "trial_no"]
    created = 0
    for (participant, session, phase, trial_no), route in paths.groupby(keys, dropna=False, sort=False):
        route = route.sort_values("sample_no")
        x = pd.to_numeric(route["pool_relative_x_m"], errors="coerce").to_numpy()
        z = pd.to_numeric(route["pool_relative_z_m"], errors="coerce").to_numpy()
        valid = np.isfinite(x) & np.isfinite(z)
        x, z = x[valid], z[valid]
        if len(x) < 2:
            continue
        fig, ax = plt.subplots(figsize=(7, 7))
        ax.plot(x, z, linewidth=1.8, label="Katılımcı rotası")
        ax.scatter(x[0], z[0], marker="o", s=65, label="Başlangıç", zorder=3)
        ax.scatter(x[-1], z[-1], marker="X", s=90, label="Son konum", zorder=3)
        radius = float(np.nanmax(np.sqrt(x**2 + z**2)))
        if np.isfinite(radius) and radius > 0:
            ax.add_patch(Circle((0, 0), radius, fill=False, linestyle="--",
                                linewidth=1.1, label="Rotaya göre tahmini havuz sınırı"))
        ax.scatter(0, 0, marker="+", s=70, label="Havuz merkezi", zorder=3)
        ax.set_aspect("equal", adjustable="datalim")
        ax.set_xlabel("Havuz merkezine göre X (m)")
        ax.set_ylabel("Havuz merkezine göre Z (m)")
        ax.set_title(f"Rota | Katılımcı {participant} | {phase} - Deneme {trial_no}\nOturum: {session}")
        ax.grid(True, alpha=0.3)
        ax.legend(loc="best", fontsize=8)
        fig.tight_layout()
        filename = (f"trajectory_{safe_name(participant)}_{safe_name(session)}_"
                    f"{safe_name(phase)}_trial_{safe_name(trial_no)}.png")
        fig.savefig(output_dir / filename, dpi=180, bbox_inches="tight")
        plt.close(fig)
        created += 1
    return created


def plot_trial_metrics(trials: pd.DataFrame, output_dir: Path) -> None:
    data = add_session(trials.copy())
    for column in ["trial_no", "latency_s", "path_length_m"]:
        data[column] = pd.to_numeric(data[column], errors="coerce")
    data["_phase_order"] = data["phase"].map(PHASE_ORDER).fillna(999)
    data = data.sort_values(["_phase_order", "trial_no", "phase"])
    labels = [f"{phase}\n{int(n) if pd.notna(n) else '?'}"
              for phase, n in zip(data["phase"], data["trial_no"])]
    x = np.arange(len(data))
    fig, axes = plt.subplots(2, 1, figsize=(max(9, len(data) * 1.05), 8), sharex=True)
    axes[0].plot(x, data["latency_s"], marker="o", linewidth=1.5)
    axes[0].set_ylabel("Hedefe ulaşma süresi (s)")
    axes[0].set_title("Deneme sonuçları — tek oturumun kontrol grafiği")
    axes[0].grid(True, alpha=0.3)
    axes[1].plot(x, data["path_length_m"], marker="o", linewidth=1.5)
    axes[1].set_ylabel("Ham yol uzunluğu (m)")
    axes[1].set_xlabel("Faz ve deneme numarası")
    axes[1].grid(True, alpha=0.3)
    axes[1].set_xticks(x, labels, rotation=35, ha="right")
    fig.tight_layout()
    fig.savefig(output_dir / "trial_metrics.png", dpi=180, bbox_inches="tight")
    plt.close(fig)

    hidden = data[data["phase"].astype(str).eq("Hidden")].sort_values("trial_no")
    fig, ax = plt.subplots(figsize=(7, 4.5))
    if not hidden.empty:
        ax.plot(hidden["trial_no"], hidden["latency_s"], marker="o", linewidth=1.5)
    ax.set_xlabel("Gizli Hedef denemesi")
    ax.set_ylabel("Hedefe ulaşma süresi (s)")
    ax.set_title("Gizli Hedef — denemelere göre süre")
    ax.grid(True, alpha=0.3)
    if len(hidden) < 2:
        ax.text(0.5, 0.82, "Bu testte 2'den az deneme var; öğrenme eğrisi yorumlanamaz.",
                transform=ax.transAxes, ha="center", va="center", wrap=True)
    fig.tight_layout()
    fig.savefig(output_dir / "hidden_phase_learning_curve.png", dpi=180, bbox_inches="tight")
    plt.close(fig)


def plot_probe(trials: pd.DataFrame, output_dir: Path) -> bool:
    columns = ["percent_time_NE", "percent_time_NW", "percent_time_SW", "percent_time_SE"]
    if not all(c in trials.columns for c in columns):
        return False
    probe = trials[trials["phase"].astype(str).eq("Probe")]
    if probe.empty:
        return False
    row = probe.iloc[0]
    values = [pd.to_numeric(pd.Series([row[c]]), errors="coerce").iloc[0] for c in columns]
    if not all(pd.notna(v) for v in values):
        return False
    fig, ax = plt.subplots(figsize=(7, 4.5))
    ax.bar(["NE", "NW", "SW", "SE"], values)
    ax.set_ylabel("Süre yüzdesi (%)")
    ax.set_xlabel("Havuz bölgesi")
    ax.set_title("Prob denemesi — bölgelerde geçirilen zaman")
    ax.set_ylim(0, max(100, max(values) * 1.1))
    ax.grid(axis="y", alpha=0.3)
    fig.tight_layout()
    fig.savefig(output_dir / "probe_quadrant_time.png", dpi=180, bbox_inches="tight")
    plt.close(fig)
    return True


def validate_paths(trials: pd.DataFrame, paths: pd.DataFrame, output_dir: Path) -> pd.DataFrame:
    trials, paths = add_session(trials.copy()), add_session(paths.copy())
    keys = ["participant_id", "session", "phase", "trial_no"]
    summaries = []
    for key, route in paths.groupby(keys, dropna=False, sort=False):
        route = route.sort_values("sample_no")
        x = pd.to_numeric(route["pool_relative_x_m"], errors="coerce").to_numpy()
        z = pd.to_numeric(route["pool_relative_z_m"], errors="coerce").to_numpy()
        valid = np.isfinite(x) & np.isfinite(z)
        x, z = x[valid], z[valid]
        sampled_length = float(np.hypot(np.diff(x), np.diff(z)).sum()) if len(x) > 1 else 0.0
        summaries.append({**dict(zip(keys, key)), "path_point_count": len(route),
                          "sampled_path_length_m": sampled_length})
    result = trials.merge(pd.DataFrame(summaries), on=keys, how="outer", indicator=True)
    if "path_length_m" in result.columns:
        result["path_length_m"] = pd.to_numeric(result["path_length_m"], errors="coerce")
        result["sampled_path_length_m"] = pd.to_numeric(result["sampled_path_length_m"], errors="coerce")
        result["sampled_minus_logged_m"] = result["sampled_path_length_m"] - result["path_length_m"]
    result.to_csv(output_dir / "path_length_validation.csv", index=False, sep=";", encoding="utf-8-sig")
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description="Unity VMWT CSV dosyalarından ilk kontrol grafiklerini üretir.")
    parser.add_argument("--data-dir", type=Path, default=None, help="Unity CSV klasörü; varsayılan Belgeler/VMWT_Data")
    parser.add_argument("--trials", type=Path, default=None, help="Deneme CSV dosyası (isteğe bağlı)")
    parser.add_argument("--paths", type=Path, default=None, help="Rota CSV dosyası (isteğe bağlı)")
    parser.add_argument("--output-dir", type=Path, default=Path(__file__).resolve().parent / "output",
                        help="Grafiklerin kaydedileceği klasör")
    args = parser.parse_args()
    try:
        trial_file = args.trials or newest_trials_file(args.data_dir or default_data_dir())
        path_file = args.paths or paired_path_file(trial_file)
        trials, paths = read_csv(trial_file), read_csv(path_file)
        missing_trials, missing_paths = TRIAL_REQUIRED - set(trials.columns), PATH_REQUIRED - set(paths.columns)
        if missing_trials:
            raise ValueError("Deneme CSV'sinde eksik kolonlar: " + ", ".join(sorted(missing_trials)))
        if missing_paths:
            raise ValueError("Rota CSV'sinde eksik kolonlar: " + ", ".join(sorted(missing_paths)))
        if trials.empty or paths.empty:
            raise ValueError("Deneme veya rota CSV dosyası boş.")
        args.output_dir.mkdir(parents=True, exist_ok=True)
        route_count = plot_trajectories(paths, args.output_dir)
        plot_trial_metrics(trials, args.output_dir)
        probe_created = plot_probe(trials, args.output_dir)
        validation = validate_paths(trials, paths, args.output_dir)
        merge_counts = validation["_merge"].value_counts().to_dict()
        report = [
            "VMWT CSV kontrol raporu", f"Deneme dosyası: {trial_file}", f"Rota dosyası: {path_file}",
            f"Deneme satırı: {len(trials)}", f"Rota noktası satırı: {len(paths)}",
            f"Rota grafiği sayısı: {route_count}",
            f"Prob bölge grafiği oluşturuldu: {'Evet' if probe_created else 'Hayır (Prob verisi yok veya eksik)'}",
            f"Deneme/rota eşleşmesi: {merge_counts}", "",
            "Bu grafikler tek oturumun veri kontrolü içindir. Tek katılımcılı kısa testten",
            "grup ortalaması/SEM veya araştırma sonucu çıkarılmamalıdır."]
        (args.output_dir / "data_quality_report.txt").write_text("\n".join(report), encoding="utf-8")
        print("Grafikler oluşturuldu.")
        print(f"Deneme dosyası : {trial_file}")
        print(f"Rota dosyası   : {path_file}")
        print(f"Çıktı klasörü  : {args.output_dir.resolve()}")
        print(f"Rota grafiği   : {route_count}")
        print(f"Prob grafiği   : {'oluşturuldu' if probe_created else 'oluşturulmadı (veri eksik)'}")
        return 0
    except Exception as exc:
        print(f"HATA: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
