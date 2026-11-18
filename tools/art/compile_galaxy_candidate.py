"""Compile candidate C# against explicit installed Unity references. No Unity process.

This checks API signatures only: it does not import assets, compile shaders,
execute NUnit/native Unity calls, render, or validate receiving asmdef wiring.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "client/HobbyDS9.Unity/Assets/HobbyDS9/GalaxyCandidate"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--managed", type=Path, required=True)
    parser.add_argument("--nunit", type=Path, required=True)
    parser.add_argument("--art-demo", type=Path, required=True)
    args = parser.parse_args()
    if "6000.6.5f1" not in args.managed.parts:
        raise ValueError("Use the pinned Unity6000.6.5f1 installation")
    for path in (args.nunit, args.art_demo, args.managed / "UnityEngine/UnityEngine.CoreModule.dll"):
        if not path.is_file():
            raise ValueError("Missing explicit reference: " + str(path))
    output = ROOT / "artifacts/galaxy-v2-source-compile"
    output.mkdir(parents=True, exist_ok=True)
    runtime_refs = sorted((args.managed / "UnityEngine").glob("UnityEngine*.dll"))
    editor_refs = sorted((args.managed / "UnityEngine").glob("UnityEditor*.dll"))
    evidence = {"mode": "CPU C# API compilation only; no Unity/import/shader/tests/render executed", "builds": []}
    runtime = output / "Runtime/bin/Release/netstandard2.1/HobbyDS9.GalaxyCandidate.dll"
    for part, name, refs in (
        ("Runtime", "HobbyDS9.GalaxyCandidate", runtime_refs),
        ("Editor", "HobbyDS9.GalaxyCandidate.Editor", runtime_refs + editor_refs + [runtime]),
        ("Tests/Editor", "HobbyDS9.GalaxyCandidate.Tests", runtime_refs + editor_refs + [runtime, args.nunit, args.art_demo]),
    ):
        folder = output / part.replace("/", "-")
        folder.mkdir(parents=True, exist_ok=True)
        sources = sorted((SOURCE / part).glob("*.cs"))
        # Explicit source/reference lists, no stub Unity APIs or project engine startup.
        project = folder / "Compile.csproj"
        project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
            '<TargetFramework>netstandard2.1</TargetFramework><LangVersion>9.0</LangVersion>'
            '<EnableDefaultCompileItems>false</EnableDefaultCompileItems><Nullable>disable</Nullable>'
            '<TreatWarningsAsErrors>true</TreatWarningsAsErrors><ImplicitUsings>disable</ImplicitUsings>'
            '<AssemblyName>' + name + '</AssemblyName></PropertyGroup><ItemGroup>'
            + ''.join('<Compile Include="' + escape(str(p), {'"': '&quot;'}) + '" />' for p in sources)
            + ''.join('<Reference Include="' + escape(p.stem) + '"><HintPath>' + escape(str(p)) + '</HintPath></Reference>' for p in refs)
            + '</ItemGroup></Project>', encoding="utf-8")
        result = subprocess.run(["dotnet", "build", str(project), "-c", "Release", "--nologo"], cwd=ROOT, text=True, capture_output=True)
        (folder / "compile.log").write_text(result.stdout + result.stderr, encoding="utf-8")
        if result.returncode:
            raise RuntimeError("Compile failed; inspect " + str(folder / "compile.log"))
        assembly = folder / ("bin/Release/netstandard2.1/" + name + ".dll")
        evidence["builds"].append({"assembly": name, "sha256": sha(assembly),
            "sources": {str(p.relative_to(ROOT)): sha(p) for p in sources},
            "references": {str(p): sha(p) for p in refs}})
        print(name + ": C# API compile passed (warnings as errors)")
    (output / "evidence.json").write_text(json.dumps(evidence, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
