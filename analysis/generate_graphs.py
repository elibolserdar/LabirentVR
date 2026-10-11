"""Unity VMWT oturum CSV dosyalarindan kontrol grafikleri uretir."""
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
TRIAL_REQUIRED = {
    "participant_id", "group", "phase", "trial_no", "latency_s",
    "path_length_m", "path_length_norm", "found_platform",
}
PATH_REQUIRED = {
    "participant_id", "phase", "trial_no", "sample_no", "time_s",
    "pool_relative_x_m", "pool_relative_z_m",
}


def read_csv(path: Path) -> pd.DataFrame:
    try:
        data = pd.read_csv(
            path, sep=";", encoding="utf-8-sig",
            dtype={
                "participant_id": "string", "group": "string",
                "session": "string", "session_id": "string", "phase": "string",
                "start_position": "string", "platform_quadrant": "string",
            },
        )
    except Exception as exc:
        raise RuntimeError(f"CSV okunamadi: {path}\nAyrinti: {exc}") from exc
    data.columns = [str(column).strip() for column in data.columns]
    return data


def default_data_dir() -> Path:
    home = Path.home()
    candidates = [
        home / "Documents" / "VMWT_Data",
        home / "OneDrive" / "Documents" / "VMWT_Data",
        home / "Belgeler" / "VMWT_Data",
        home / "OneDrive" / "Belgeler" / "VMWT_Data",
    ]
    return next((path for path in candidates if path.is_dir()), candidates[0])


def newest_trials_file(data_dir: Path) -> Path:
    new_format = list(data_dir.rglob("trials.csv"))
    new_format = [
        path for path in new_format
        if path.parent.name.lower() not in {"graphs", "output"}
    ]
    if new_format:
        return max(new_format, key=lambda path: path.stat().st_mtime)

    # Eski dosya adlarini da geriye donuk destekle.
    legacy = []
    for path in data_dir.rglob("*.csv"):
        if path.name.lower().endswith("_path_points.csv"):
            continue
        if path.name.lower() in {
            "path_points.csv", "experiment_config.csv", "path_length_validation.csv"
        }:
            continue
        if any(part.lower() in {"graphs", "output"} for part in path.parts):
            continue
        candidate_path = path.with_name(f"{path.stem}_path_points.csv")
        if candidate_path.exists():
            legacy.append(path)
    if not legacy:
        raise FileNotFoundError(
            f"{data_dir} altinda deneme CSV dosyasi bulunamadi. "
            "Unity'nin olusturdugu VMWT_Data klasorunu --data-dir ile belirtin."
        )
    return max(legacy, key=lambda path: path.stat().st_mtime)


def paired_path_file(trial_file: Path) -> Path:
    if trial_file.name.lower() == "trials.csv":
        path_file = trial_file.with_name("path_points.csv")
    else:
        path_file = trial_file.with_name(f"{trial_file.stem}_path_points.csv")
    if not path_file.exists():
        raise FileNotFoundError(f"Eslesen rota dosyasi bulunamadi: {path_file}")
    return path_file


def read_experiment_config(trial_file: Path) -> dict[str, str]:
    config_file = trial_file.with_name("experiment_config.csv")
    if not config_file.exists():
        return {}
    config = read_csv(config_file)
    if not {"setting", "value"}.issubset(config.columns):
        return {}
    return {
        str(row["setting"]): str(row["value"])
        for _, row in config.iterrows()
        if pd.notna(row["setting"]) and pd.notna(row["value"])
    }


def safe_name(value: object) -> str:
    name = re.sub(r"[^A-Za-z0-9_-]+", "_", str(value).strip())
    return name.strip("_") or "unknown"


def add_session(data: pd.DataFrame) -> pd.DataFrame:
    if "session" not in data.columns or "session_id" not in data.columns:
        data = data.copy()
        if "session" not in data.columns:
            data["session"] = "UnknownSession"
        if "session_id" not in data.columns:
            data["session_id"] = "LegacySession"
    return data


def pool_diameter_from_config(config: dict[str, str], override: float | None) -> float | None:
    if override is not None:
        return override
    value = config.get("pool_diameter_m")
    if value is None:
        return None
    try:
        diameter = float(value)
        return diameter if diameter > 0 else None
    except (TypeError, ValueError):
        return None


def plot_trajectories(
    paths: pd.DataFrame,
    trials: pd.DataFrame,
    output_dir: Path,
    pool_diameter: float | None,
) -> int:
    paths, trials = add_session(paths), add_session(trials)
    keys = ["participant_id", "session_id", "session", "phase", "trial_no"]
    trial_lookup = {}
    for key, rows in trials.groupby(keys, dropna=False, sort=False):
        trial_lookup[key] = rows.iloc[0]

    created = 0
    for key, route in paths.groupby(keys, dropna=False, sort=False):
        participant, session_id, session, phase, trial_no = key
        route = route.sort_values("sample_no")
        x = pd.to_numeric(route["pool_relative_x_m"], errors="coerce").to_numpy()
        z = pd.to_numeric(route["pool_relative_z_m"], errors="coerce").to_numpy()
        valid = np.isfinite(x) & np.isfinite(z)
        x, z = x[valid], z[valid]
        if len(x) < 2:
            continue

        fig, ax = plt.subplots(figsize=(7, 7))
        ax.plot(x, z, linewidth=1.8, label="Katilimci rotasi")
        ax.scatter(x[0], z[0], marker="o", s=65, label="Baslangic", zorder=3)
        ax.scatter(x[-1], z[-1], marker="X", s=90, label="Son konum", zorder=3)
        ax.scatter(0, 0, marker="+", s=70, label="Havuz merkezi", zorder=3)

        trial_row = trial_lookup.get(key)
        if trial_row is not None and {
            "platform_relative_x_m", "platform_relative_z_m"
        }.issubset(trial_row.index):
            goal_x = pd.to_numeric(
                pd.Series([trial_row["platform_relative_x_m"]]), errors="coerce"
            ).iloc[0]
            goal_z = pd.to_numeric(
                pd.Series([trial_row["platform_relative_z_m"]]), errors="coerce"
            ).iloc[0]
            if pd.notna(goal_x) and pd.notna(goal_z):
                ax.scatter(
                    [goal_x], [goal_z], marker="*", s=180,
                    label="Hedef/platform", zorder=4,
                )

        if pool_diameter is not None:
            radius = pool_diameter / 2.0
            ax.add_patch(Circle(
                (0, 0), radius, fill=False, linestyle="--",
                linewidth=1.1, label=f"Havuz siniri (cap {pool_diameter:g} m)",
            ))
            ax.set_xlim(-radius * 1.08, radius * 1.08)
            ax.set_ylim(-radius * 1.08, radius * 1.08)
        else:
            ax.text(
                0.02, 0.02,
                "Not: experiment_config.csv yok; havuz siniri cizilmedi.",
                transform=ax.transAxes, fontsize=8,
            )

        ax.set_aspect("equal", adjustable="box")
        ax.set_xlabel("Havuz merkezine gore X (m)")
        ax.set_ylabel("Havuz merkezine gore Z (m)")
        ax.set_title(
            f"Rota | Katilimci {participant} | {phase} - Deneme {trial_no}\n"
            f"Oturum: {session} ({session_id})"
        )
        ax.grid(True, alpha=0.3)
        ax.legend(loc="best", fontsize=8)
        fig.tight_layout()
        filename = (
            f"trajectory_{safe_name(participant)}_{safe_name(session)}_"
            f"{safe_name(phase)}_trial_{safe_name(trial_no)}.png"
        )
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
    labels = [
        f"{phase}\n{int(number) if pd.notna(number) else '?'}"
        for phase, number in zip(data["phase"], data["trial_no"])
    ]
    positions = np.arange(len(data))
    fig, axes = plt.subplots(
        2, 1, figsize=(max(9, len(data) * 1.05), 8), sharex=True
    )
    axes[0].plot(positions, data["latency_s"], marker="o", linewidth=1.5)
    axes[0].set_ylabel("Hedefe ulasma suresi (s)")
    axes[0].set_title("Deneme sonuclari - tek oturum veri kontrolu")
    axes[0].grid(True, alpha=0.3)
    axes[1].plot(positions, data["path_length_m"], marker="o", linewidth=1.5)
    axes[1].set_ylabel("Ham yol uzunlugu (m)")
    axes[1].set_xlabel("Faz ve deneme numarasi")
    axes[1].grid(True, alpha=0.3)
    axes[1].set_xticks(positions, labels, rotation=35, ha="right")
    fig.tight_layout()
    fig.savefig(output_dir / "trial_metrics.png", dpi=180, bbox_inches="tight")
    plt.close(fig)

    hidden = data[data["phase"].astype(str).eq("Hidden")].sort_values("trial_no")
    fig, ax = plt.subplots(figsize=(7, 4.5))
    if not hidden.empty:
        ax.plot(hidden["trial_no"], hidden["latency_s"], marker="o", linewidth=1.5)
    ax.set_xlabel("Gizli Hedef denemesi")
    ax.set_ylabel("Hedefe ulasma suresi (s)")
    ax.set_title("Gizli Hedef - denemelere gore sure")
    ax.grid(True, alpha=0.3)
    if len(hidden) < 2:
        ax.text(
            0.5, 0.82,
            "Bu testte 2'den az deneme var; ogrenme egrisi yorumlanamaz.",
            transform=ax.transAxes, ha="center", va="center", wrap=True,
        )
    fig.tight_layout()
    fig.savefig(output_dir / "hidden_phase_learning_curve.png", dpi=180, bbox_inches="tight")
    plt.close(fig)


def plot_probe(trials: pd.DataFrame, output_dir: Path) -> bool:
    columns = ["percent_time_NE", "percent_time_NW", "percent_time_SW", "percent_time_SE"]
    if not all(column in trials.columns for column in columns):
        return False
    probe = trials[trials["phase"].astype(str).eq("Probe")]
    if probe.empty:
        return False
    row = probe.iloc[0]
    values = [
        pd.to_numeric(pd.Series([row[column]]), errors="coerce").iloc[0]
        for column in columns
    ]
    if not all(pd.notna(value) for value in values):
        return False
    fig, ax = plt.subplots(figsize=(7, 4.5))
    ax.bar(["NE", "NW", "SW", "SE"], values)
    ax.set_ylabel("Sure yuzdesi (%)")
    ax.set_xlabel("Havuz bolgesi")
    ax.set_title("Prob denemesi - bolgelerde gecirilen zaman")
    ax.set_ylim(0, max(100, max(values) * 1.1))
    ax.grid(axis="y", alpha=0.3)
    fig.tight_layout()
    fig.savefig(output_dir / "probe_quadrant_time.png", dpi=180, bbox_inches="tight")
    plt.close(fig)
    return True


def validate_paths(
    trials: pd.DataFrame, paths: pd.DataFrame, output_dir: Path
) -> pd.DataFrame:
    trials, paths = add_session(trials.copy()), add_session(paths.copy())
    keys = ["participant_id", "session_id", "session", "phase", "trial_no"]
    summaries = []
    for key, route in paths.groupby(keys, dropna=False, sort=False):
        route = route.sort_values("sample_no")
        x = pd.to_numeric(route["pool_relative_x_m"], errors="coerce").to_numpy()
        z = pd.to_numeric(route["pool_relative_z_m"], errors="coerce").to_numpy()
        valid = np.isfinite(x) & np.isfinite(z)
        x, z = x[valid], z[valid]
        sampled_length = (
            float(np.hypot(np.diff(x), np.diff(z)).sum()) if len(x) > 1 else 0.0
        )
        summaries.append({
            **dict(zip(keys, key)),
            "path_point_count": len(route),
            "sampled_path_length_m": sampled_length,
        })
    result = trials.merge(pd.DataFrame(summaries), on=keys, how="outer", indicator=True)
    if "path_length_m" in result.columns:
        result["path_length_m"] = pd.to_numeric(result["path_length_m"], errors="coerce")
        result["sampled_path_length_m"] = pd.to_numeric(
            result["sampled_path_length_m"], errors="coerce"
        )
        result["sampled_minus_logged_m"] = (
            result["sampled_path_length_m"] - result["path_length_m"]
        )
    result.to_csv(
        output_dir / "path_length_validation.csv",
        index=False, sep=";", encoding="utf-8-sig",
    )
    return result


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Unity VMWT CSV dosyalarindan kontrol grafikleri uretir."
    )
    parser.add_argument(
        "--data-dir", type=Path, default=None,
        help="Unity CSV kok klasoru; varsayilan Belgeler/VMWT_Data",
    )
    parser.add_argument("--trials", type=Path, default=None, help="Deneme CSV dosyasi")
    parser.add_argument("--paths", type=Path, default=None, help="Rota CSV dosyasi")
    parser.add_argument(
        "--output-dir", type=Path, default=None,
        help="Grafik klasoru; varsayilan yeni oturumlarda oturum/graphs",
    )
    parser.add_argument(
        "--pool-diameter", type=float, default=None,
        help="Config dosyasi yoksa havuz capi (metre); yalnizca dogrulanmis deger girin",
    )
    args = parser.parse_args()

    try:
        trial_file = args.trials or newest_trials_file(args.data_dir or default_data_dir())
        path_file = args.paths or paired_path_file(trial_file)
        trials, paths = read_csv(trial_file), read_csv(path_file)

        missing_trials = TRIAL_REQUIRED - set(trials.columns)
        missing_paths = PATH_REQUIRED - set(paths.columns)
        if missing_trials:
            raise ValueError(
                "Deneme CSV'sinde eksik kolonlar: " + ", ".join(sorted(missing_trials))
            )
        if missing_paths:
            raise ValueError(
                "Rota CSV'sinde eksik kolonlar: " + ", ".join(sorted(missing_paths))
            )
        if trials.empty or paths.empty:
            raise ValueError("Deneme veya rota CSV dosyasi bos.")

        config = read_experiment_config(trial_file)
        pool_diameter = pool_diameter_from_config(config, args.pool_diameter)
        if args.output_dir is not None:
            output_dir = args.output_dir
        elif trial_file.name.lower() == "trials.csv":
            output_dir = trial_file.parent / "graphs"
        else:
            output_dir = Path(__file__).resolve().parent / "output"

        output_dir.mkdir(parents=True, exist_ok=True)
        route_count = plot_trajectories(paths, trials, output_dir, pool_diameter)
        plot_trial_metrics(trials, output_dir)
        probe_created = plot_probe(trials, output_dir)
        validation = validate_paths(trials, paths, output_dir)
        merge_counts = validation["_merge"].value_counts().to_dict()
        report = [
            "VMWT CSV kontrol raporu",
            f"Deneme dosyasi: {trial_file}",
            f"Rota dosyasi: {path_file}",
            f"Deneme satiri: {len(trials)}",
            f"Rota noktasi satiri: {len(paths)}",
            f"Havuz capi (m): {pool_diameter if pool_diameter is not None else 'bilinmiyor; daire cizilmedi'}",
            f"Rota grafigi sayisi: {route_count}",
            f"Prob bolge grafigi: {'olusturuldu' if probe_created else 'olusturulmadi (veri yok/eksik)'}",
            f"Deneme/rota eslesmesi: {merge_counts}",
            "",
            "Bu grafikler tek oturumun veri kontrolu icindir. Tek katilimcili kisa testten",
            "grup ortalamasi/SEM veya arastirma sonucu cikarilmamalidir.",
        ]
        (output_dir / "data_quality_report.txt").write_text("\n".join(report), encoding="utf-8")
        print("Grafikler olusturuldu.")
        print(f"Deneme dosyasi : {trial_file}")
        print(f"Rota dosyasi   : {path_file}")
        print(f"Cikti klasoru  : {output_dir.resolve()}")
        print(f"Rota grafigi   : {route_count}")
        print(f"Prob grafigi   : {'olusturuldu' if probe_created else 'olusturulmadi (veri eksik)'}")
        if pool_diameter is None:
            print("UYARI: experiment_config.csv yok; havuz siniri cizilmedi.")
        return 0
    except Exception as exc:
        print(f"HATA: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
