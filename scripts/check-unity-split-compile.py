#!/usr/bin/env python3
"""Local pinned-reference compiler coverage. No Unity process, tests or runtime assembly loading."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess

ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/"prototypes/HobbyDS9.UnityReviewSplitCompile"
NAMES={"DefaultRuntime":"HobbyDS9.UnityClient","ReviewRuntime":"HobbyDS9.UnityReview","DefaultEditor":"HobbyDS9.UnityClient.Editor",
       "ReviewEditor":"HobbyDS9.UnityReview.Editor","DefaultTests":"HobbyDS9.UnityClient.Tests","ReviewTests":"HobbyDS9.UnityReview.Tests"}
SOURCE={"DefaultRuntime":"Runtime","ReviewRuntime":"Review/Runtime","DefaultEditor":"Editor","ReviewEditor":"Review/Editor",
        "DefaultTests":"Tests/Editor","ReviewTests":"Review/Tests"}
EXPECTED_NEWTONSOFT="7292d3eb508652d14726749dd27094f2d481aeccf2db6427b62f68a71460897e"

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--unity-managed-dir",type=Path,required=True)
    parser.add_argument("--newtonsoft",type=Path,required=True)
    parser.add_argument("--nunit",type=Path,required=True)
    parser.add_argument("--test-runner-dir",type=Path,required=True)
    parser.add_argument("--art-demo",type=Path,required=True,help="Receiving project's compiled HobbyDS9.ArtDemo.dll")
    parser.add_argument("--input-system",type=Path,required=True,help="Receiving project's compiled Unity.InputSystem.dll")
    parser.add_argument("--output",type=Path,default=ROOT/"artifacts/split-compiler")
    args=parser.parse_args()
    args.output.mkdir(parents=True,exist_ok=True)
    for path in (args.unity_managed_dir/"UnityEngine/UnityEngine.CoreModule.dll",args.newtonsoft,args.nunit,
                 args.test_runner_dir/"UnityEngine.TestRunner.dll",args.test_runner_dir/"UnityEditor.TestRunner.dll",args.art_demo,args.input_system):
        if not path.is_file(): raise ValueError("Missing explicit local reference: "+str(path))
    if "6000.6.5f1" not in args.unity_managed_dir.parts: raise ValueError("References must belong to pinned Unity6000.6.5f1 installation")
    if sha(args.newtonsoft)!=EXPECTED_NEWTONSOFT: raise ValueError("Pinned Unity Runtime Newtonsoft bytes differ")
    refs={"UnityManagedDir":args.unity_managed_dir,"UnityNewtonsoftPath":args.newtonsoft,"UnityNUnitPath":args.nunit,"UnityTestRunnerDir":args.test_runner_dir,"UnityArtDemoPath":args.art_demo,"UnityInputSystemPath":args.input_system}
    record={"mode":"compiler-only; no Unity process/AOT/tests executed","references":{k:str(v.resolve()) for k,v in refs.items()},
            "newtonsoft_sha256":sha(args.newtonsoft),"nunit_sha256":sha(args.nunit),"art_demo_sha256":sha(args.art_demo),"input_system_sha256":sha(args.input_system),"builds":[]}
    paths=[]
    for project,variant in [(p,v) for v in ("Player","Editor") for p in ("DefaultRuntime","ReviewRuntime")]+[(p,"Editor") for p in ("DefaultEditor","ReviewEditor","DefaultTests","ReviewTests")]:
        command=["dotnet","build",str(BASE/project/(project+".csproj")),"-c","Release","-t:Rebuild","-p:RestoreLockedMode=true","-p:UnityCompileVariant="+variant]+["-p:"+key+"="+str(value.resolve()) for key,value in refs.items()]
        result=subprocess.run(command,cwd=ROOT,text=True,capture_output=True)
        log=result.stdout+result.stderr;(args.output/(project+"-"+variant+".log")).write_text(log,encoding="utf-8")
        if result.returncode: raise RuntimeError(project+" "+variant+" compile failed; inspect local log")
        assembly=BASE/project/"bin"/variant/"Release/netstandard2.1"/(NAMES[project]+".dll")
        if not assembly.is_file():raise ValueError("Missing separately named output")
        sources=sorted((ROOT/"client/HobbyDS9.Unity/Assets/HobbyDS9"/SOURCE[project]).glob("*.cs"))
        warnings=sorted(set(re.findall(r"warning (CS\d+): ([^\r\n]+?) \[",log)))
        record["builds"].append({"project":project,"variant":variant,"assembly":NAMES[project],"sha256":sha(assembly),"sources":{str(p.relative_to(ROOT)):sha(p) for p in sources},
                                 "symbols":["UNITY_EDITOR","UNITY_EDITOR_WIN"] if variant=="Editor" else [],"include_tests":project.endswith("Tests"),"warnings":warnings,"errors":0})
        paths.append(assembly)
        print(project+" "+variant+": compiled "+str(len(sources))+" actual files; distinct warnings="+str(len(warnings)))
    probe=["dotnet","run","--project",str(BASE/"MetadataProbe"),"-c","Release","--verbosity","quiet","--"]+[str(p) for p in paths]
    result=subprocess.run(probe,cwd=ROOT,text=True,capture_output=True,check=True)
    metadata=json.loads(result.stdout)
    for item,build in zip(metadata,record["builds"]):
        if item["name"]!=build["assembly"] or item["framework"]!=".NETStandard,Version=v2.1":raise ValueError("Wrong actual assembly name/target")
        if any(r.startswith(("HobbyDS9.TacticalReviewStorage","HobbyDS9.TacticalReviewShell","System.Text.Json","xunit")) for r in item["references"]):raise ValueError("Host-only runtime reference")
        if build["project"].endswith("Runtime") and any(r.startswith("UnityEditor") for r in item["references"]):raise ValueError("Runtime compiler imported UnityEditor")
        if build["project"]=="DefaultRuntime":
            if "HobbyDS9.UnityReview" in item["references"]:raise ValueError("Unexpected direct default-to-review assembly dependency")
            t=next(t for t in item["types"] if t["name"]=="HobbyDS9.UnityClient.ClientEntry")
            b=next(m for m in t["methods"] if m["name"]=="Bootstrap")
            if not b["isStatic"] or b["ilBytes"]==0:raise ValueError("Actual default bootstrap not compiled")
        if build["project"]=="ReviewRuntime":
            if "HobbyDS9.UnityClient" in item["references"]:raise ValueError("Unexpected review-to-default assembly dependency")
            t=next(t for t in item["types"] if t["name"]=="HobbyDS9.UnityReview.ReviewLaunch")
            b=next(m for m in t["methods"] if m["name"]=="Launch")
            if not b["isStatic"] or not b["isPublic"] or b["ilBytes"]==0:raise ValueError("Public reflected bridge not compiled")
    record["metadata"]=metadata
    (args.output/"evidence.json").write_text(json.dumps(record,indent=2),encoding="utf-8")
    print("Eight split compiler outputs and bootstrap/bridge IL metadata verified; actual asmdef/import/reflection/AOT/test execution remain unverified.")

if __name__=="__main__":main()
