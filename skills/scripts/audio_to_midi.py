#!/usr/bin/env python3
"""
MP3 转 MIDI 工具
使用 Demucs 分离人声 + Basic Pitch 转换 MIDI

用法:
    python audio_to_midi.py <input_mp3> [output_dir]
    python audio_to_midi.py --check  # 检查依赖和硬件

输出:
    JSON 格式的处理结果
"""

import sys
import os
import json
import subprocess
import shutil
from pathlib import Path
from datetime import datetime


def output_json(data):
    """输出 JSON 格式结果"""
    print(json.dumps(data, ensure_ascii=False, indent=2))


def detect_hardware():
    """检测可用硬件加速"""
    try:
        import torch
        if torch.cuda.is_available():
            device_name = torch.cuda.get_device_name(0)
            return {
                "device": "cuda",
                "name": device_name,
                "description": f"NVIDIA GPU 加速 ({device_name})",
                "estimated_time": "1-2 分钟"
            }
        elif hasattr(torch.backends, 'mps') and torch.backends.mps.is_available():
            return {
                "device": "mps",
                "name": "Apple Silicon",
                "description": "Apple Silicon 加速 (MPS)",
                "estimated_time": "2-3 分钟"
            }
    except ImportError:
        pass

    return {
        "device": "cpu",
        "name": "CPU",
        "description": "CPU 模式 (较慢)",
        "estimated_time": "8-15 分钟"
    }


def check_dependencies():
    """检查依赖是否安装"""
    dependencies = {
        "demucs": {"installed": False, "version": None},
        "basic_pitch": {"installed": False, "version": None},
        "torch": {"installed": False, "version": None},
    }

    try:
        import demucs
        dependencies["demucs"]["installed"] = True
        dependencies["demucs"]["version"] = getattr(demucs, '__version__', 'unknown')
    except ImportError:
        pass

    try:
        import basic_pitch
        dependencies["basic_pitch"]["installed"] = True
        dependencies["basic_pitch"]["version"] = getattr(basic_pitch, '__version__', 'unknown')
    except ImportError:
        pass

    try:
        import torch
        dependencies["torch"]["installed"] = True
        dependencies["torch"]["version"] = torch.__version__
    except ImportError:
        pass

    return dependencies


def check_command_available(cmd):
    """检查命令行工具是否可用"""
    return shutil.which(cmd) is not None


def separate_vocals(input_mp3, output_dir, device="cpu"):
    """
    使用 Demucs 分离人声

    Args:
        input_mp3: 输入 MP3 文件路径
        output_dir: 输出目录
        device: 使用的设备 (cuda/mps/cpu)

    Returns:
        vocals_path: 人声文件路径
    """
    input_path = Path(input_mp3)
    output_path = Path(output_dir)

    # 构建 demucs 命令
    cmd = [
        sys.executable, "-m", "demucs",
        "--two-stems=vocals",  # 只分离人声和伴奏
        "-o", str(output_path),
        "--device", device if device != "mps" else "mps",
    ]

    # 添加输入文件
    cmd.append(str(input_path))

    # 执行命令
    try:
        result = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            timeout=1800  # 30 分钟超时
        )

        if result.returncode != 0:
            return None, f"Demucs 执行失败: {result.stderr}"

        # 查找输出的人声文件
        # Demucs 输出格式: output_dir/htdemucs/song_name/vocals.wav
        song_name = input_path.stem
        vocals_path = output_path / "htdemucs" / song_name / "vocals.wav"

        if not vocals_path.exists():
            # 尝试其他可能的路径
            for model_dir in output_path.iterdir():
                if model_dir.is_dir():
                    possible_path = model_dir / song_name / "vocals.wav"
                    if possible_path.exists():
                        vocals_path = possible_path
                        break

        if vocals_path.exists():
            return str(vocals_path), None
        else:
            return None, f"未找到人声文件，请检查 {output_path} 目录"

    except subprocess.TimeoutExpired:
        return None, "Demucs 处理超时 (超过 30 分钟)"
    except Exception as e:
        return None, f"Demucs 执行异常: {str(e)}"


def convert_to_midi(vocals_wav, output_dir):
    """
    使用 Basic Pitch 将人声转换为 MIDI

    Args:
        vocals_wav: 人声 WAV 文件路径
        output_dir: 输出目录

    Returns:
        midi_path: MIDI 文件路径
    """
    vocals_path = Path(vocals_wav)
    output_path = Path(output_dir)

    # 构建 basic-pitch 命令
    cmd = [
        sys.executable, "-m", "basic_pitch",
        str(output_path),
        str(vocals_path)
    ]

    try:
        result = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            timeout=300  # 5 分钟超时
        )

        if result.returncode != 0:
            return None, f"Basic Pitch 执行失败: {result.stderr}"

        # 查找输出的 MIDI 文件
        # Basic Pitch 输出格式: output_dir/vocals_basic_pitch.mid
        midi_name = vocals_path.stem + "_basic_pitch.mid"
        midi_path = output_path / midi_name

        if midi_path.exists():
            return str(midi_path), None
        else:
            # 尝试查找任何 .mid 文件
            for f in output_path.glob("*.mid"):
                return str(f), None
            return None, f"未找到 MIDI 文件，请检查 {output_path} 目录"

    except subprocess.TimeoutExpired:
        return None, "Basic Pitch 处理超时 (超过 5 分钟)"
    except Exception as e:
        return None, f"Basic Pitch 执行异常: {str(e)}"


def process_audio(input_mp3, output_dir=None):
    """
    完整的音频处理流程

    Args:
        input_mp3: 输入 MP3 文件路径
        output_dir: 输出目录 (默认为输入文件所在目录)

    Returns:
        处理结果字典
    """
    input_path = Path(input_mp3)

    if not input_path.exists():
        return {
            "status": "error",
            "error": f"输入文件不存在: {input_mp3}"
        }

    if output_dir is None:
        output_dir = input_path.parent

    output_path = Path(output_dir)
    output_path.mkdir(parents=True, exist_ok=True)

    # 检测硬件
    hardware = detect_hardware()

    # 检查依赖
    deps = check_dependencies()
    missing_deps = [name for name, info in deps.items()
                   if not info["installed"] and name != "torch"]

    if missing_deps:
        return {
            "status": "error",
            "error": "缺少必要依赖",
            "missing_dependencies": missing_deps,
            "install_command": f"pip install {' '.join(missing_deps).replace('_', '-')}",
            "alternative": "或使用在线工具: https://basicpitch.spotify.com"
        }

    result = {
        "status": "processing",
        "input_file": str(input_path),
        "output_dir": str(output_path),
        "hardware": hardware,
        "steps": []
    }

    # Step 1: 分离人声
    result["steps"].append({
        "step": 1,
        "name": "分离人声",
        "status": "in_progress",
        "tool": "Demucs"
    })

    vocals_path, error = separate_vocals(
        input_mp3,
        output_path,
        hardware["device"]
    )

    if error:
        result["status"] = "error"
        result["steps"][-1]["status"] = "failed"
        result["steps"][-1]["error"] = error
        return result

    result["steps"][-1]["status"] = "completed"
    result["steps"][-1]["output"] = vocals_path
    result["vocals_file"] = vocals_path

    # Step 2: 转换为 MIDI
    result["steps"].append({
        "step": 2,
        "name": "转换 MIDI",
        "status": "in_progress",
        "tool": "Basic Pitch"
    })

    midi_path, error = convert_to_midi(vocals_path, output_path)

    if error:
        result["status"] = "error"
        result["steps"][-1]["status"] = "failed"
        result["steps"][-1]["error"] = error
        return result

    result["steps"][-1]["status"] = "completed"
    result["steps"][-1]["output"] = midi_path
    result["midi_file"] = midi_path

    # 重命名 MIDI 文件为更友好的名称
    final_midi_name = input_path.stem + ".mid"
    final_midi_path = output_path / final_midi_name

    if str(midi_path) != str(final_midi_path):
        try:
            shutil.move(midi_path, final_midi_path)
            result["midi_file"] = str(final_midi_path)
        except Exception:
            pass  # 保持原文件名

    result["status"] = "success"
    result["message"] = "MP3 转 MIDI 完成"
    result["completed_at"] = datetime.now().isoformat()

    return result


def main():
    """主函数"""
    if len(sys.argv) < 2:
        output_json({
            "status": "error",
            "error": "缺少参数",
            "usage": "python audio_to_midi.py <input_mp3> [output_dir]",
            "examples": [
                "python audio_to_midi.py song.mp3",
                "python audio_to_midi.py song.mp3 ./output",
                "python audio_to_midi.py --check"
            ]
        })
        sys.exit(1)

    # 检查模式
    if sys.argv[1] == "--check":
        deps = check_dependencies()
        hardware = detect_hardware()

        all_installed = all(
            info["installed"]
            for name, info in deps.items()
            if name != "torch"
        )

        output_json({
            "status": "ready" if all_installed else "missing_dependencies",
            "dependencies": deps,
            "hardware": hardware,
            "install_command": "pip install demucs basic-pitch" if not all_installed else None,
            "online_alternative": "https://basicpitch.spotify.com"
        })
        sys.exit(0 if all_installed else 1)

    # 处理模式
    input_mp3 = sys.argv[1]
    output_dir = sys.argv[2] if len(sys.argv) > 2 else None

    result = process_audio(input_mp3, output_dir)
    output_json(result)

    sys.exit(0 if result["status"] == "success" else 1)


if __name__ == "__main__":
    main()
