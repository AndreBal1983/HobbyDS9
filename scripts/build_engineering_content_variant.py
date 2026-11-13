"""Source-only preparation: do not invoke until Root/frontend review and admissions.
Creates only the NEW EngineeringDevelopmentV1 data capsule; no SDK/engine/native work.
"""
import argparse
import copy
import datetime as dt
import hashlib
import json
import math
import os
from pathlib import Path
import re
import stat
import subprocess
import sys
import time
import uuid

REPOSITORY = Path(r"C:\Users\ambal\.codex\worktrees\ui-fleet-cards-20261107\HobbyDS9")
SOURCE_COMMIT = "e365ec1ecf3d686b114548f805f68f73f742e89b"
SOURCE_BLOB = "scenarios/ds9-slice/scenario.json"
SCENARIO_OUTPUT = "scenarios/engineering-development-v1"
RESOURCE_OUTPUT = "client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources/EngineeringDevelopmentV1"
BASE = "scenarios/playable-fleet-v1/"
FILES = ("scenario.json","core.json","physical.json","package.json","playable.json")
META_NAMESPACE = uuid.UUID("eaf582b6-88de-54e1-a612-202611120001")
KEYS = {"HullFamilies":"FamilyId","Hulls":"VersionId","Components":"VersionId","Roles":"RoleId"}
TAG = "NEW_ENGINEERING_DEVELOPMENT_V1_DATA_WRITE"
ADMISSION_DIRECTORY = Path(r"E:\HobbyDS9\artifacts\engineering-development-source-20261112")
AUTHORITIES = (
    (r"E:\HobbyDS9\docs\usage-stop-80-owner-20261110.json",2382,"1E27E18B67F78D8597761082D08115EE58B400008BBBFCA616FC86B17D71DB92"),
    (r"E:\HobbyDS9\docs\usage-continuation-owner-20261112.json",3172,"A4EC3AAF91A453BFD8177F704C878CC14AFA8E281E12AC3E8A0A006A024AF344"))
INPUT_PATHS = [BASE+name for name in FILES]+["scenarios/activities-v1/catalogue.json","scenarios/colonisation-v1/catalogue.json",
    "client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Runtime/ReviewExactPackageLoader.cs"]

class Hold(Exception):
    pass

def require(condition, code):
    if not condition: raise Hold(code)

def digest(raw):
    return hashlib.sha256(raw).hexdigest()

def parse(raw):
    def pairs(items):
        result = {}
        for key,value in items:
            require(key not in result,"DUPLICATE_JSON_KEY"); result[key] = value
        return result
    def no_float(_):
        raise Hold("UNREVIEWED_NONINTEGER_NUMBER")
    return json.loads(raw.decode("utf-8-sig"),object_pairs_hook=pairs,parse_int=int,parse_float=no_float,parse_constant=no_float)

def encoded(value):
    return (json.dumps(value,ensure_ascii=False,allow_nan=False,indent=2)+"\n").encode("utf-8")

def semantic(value):
    return json.dumps(value,sort_keys=True,ensure_ascii=False,allow_nan=False,separators=(",",":")).encode("utf-8")

def same(a,b):
    return semantic(a) == semantic(b)

def plain(path, missing_final=False):
    path = Path(os.path.abspath(path)); parts = path.parts
    require(path.is_absolute() and not str(path).startswith("\\\\"),"ABSOLUTE_LOCAL_PATH_REQUIRED")
    current = Path(parts[0])
    anchor = current.lstat()
    require(stat.S_ISDIR(anchor.st_mode) and not getattr(anchor,"st_file_attributes",0) & 0x400,"ROOT_NOT_PLAIN_DIRECTORY")
    for index,part in enumerate(parts[1:],1):
        current = current/part
        try: info = current.lstat()
        except FileNotFoundError:
            require(missing_final and index == len(parts)-1,"MISSING_INTERMEDIATE_ANCESTOR"); return path
        require(not stat.S_ISLNK(info.st_mode) and not getattr(info,"st_file_attributes",0) & 0x400,"REPARSE_ANCESTOR_OR_LEAF")
        if index < len(parts)-1: require(stat.S_ISDIR(info.st_mode),"ANCESTOR_NOT_DIRECTORY")
    return path

def input_bytes(pin, root=REPOSITORY):
    relative = pin["relativePath"]
    require(type(relative) is str and not Path(relative).is_absolute() and ".." not in Path(relative).parts,"INPUT_RELATIVE_PATH")
    path = plain(root/relative)
    require(os.path.commonpath((str(path),str(root))) == str(root),"INPUT_PATH_ESCAPE")
    before = path.stat(); raw = path.read_bytes(); after = path.stat()
    require(stat.S_ISREG(before.st_mode) and (before.st_dev,before.st_ino,before.st_size,before.st_mtime_ns) ==
            (after.st_dev,after.st_ino,after.st_size,after.st_mtime_ns),"INPUT_METADATA_CHANGED")
    require(type(pin["bytes"]) is int and len(raw) == pin["bytes"] and digest(raw).upper() == pin["sha256"],"INPUT_BYTES_PIN_MISMATCH")
    require(pin["fileIdentity"] == {"device":before.st_dev,"inode":before.st_ino},"INPUT_FILE_IDENTITY_CHANGED")
    return raw

def git(arguments):
    result = subprocess.run(["git","-C",str(REPOSITORY)]+arguments,stdout=subprocess.PIPE,stderr=subprocess.PIPE,
                            check=False,shell=False,creationflags=0x08000000 if os.name=="nt" else 0)
    require(result.returncode == 0 and not result.stderr,"GIT_SOURCE_READ_FAILED")
    return result.stdout

def mapping_for(catalogue):
    mapping = {}
    for component in catalogue["Components"]:
        old = component["VersionId"]; family = component["FamilyId"]
        require(old.startswith("component.") and not old.startswith("component.utility."),"UNEXPECTED_IMPORTED_COMPONENT_NAMESPACE")
        require(family.startswith("component-family.") and not family.startswith("component-family.utility."),"UNEXPECTED_IMPORTED_FAMILY_NAMESPACE")
        require(component["CompatibleHullFamilyIds"] == ["hull-family.utility"],"IMPORTED_UTILITY_COMPATIBILITY_CHANGED")
        mapping[old] = "component.utility."+old[len("component."):]
        mapping[family] = "component-family.utility."+family[len("component-family."):]
    require(len(set(mapping.values())) == len(mapping),"NONINJECTIVE_NAMESPACE_MAP")
    return mapping

def remap(value,mapping,field=None):
    if type(value) is dict:
        result = {}
        for key,item in value.items():
            changed = mapping.get(key,key); require(changed not in result,"MAPPED_KEY_COLLISION")
            result[changed] = remap(item,mapping,key)
        return result
    if type(value) is list: return [remap(item,mapping,field) for item in value]
    if type(value) is str:
        if value in mapping: return mapping[value]
        if field == "EffectSummary":
            pattern = r"(?<![A-Za-z0-9_.-])(?:"+"|".join(re.escape(key) for key in sorted(mapping,key=len,reverse=True))+r")(?![A-Za-z0-9_.-])"
            return re.sub(pattern,lambda match:mapping[match.group(0)],value)
    return value

def union(rows,key):
    result,seen = [],{}
    for row in rows:
        identifier = row[key]; require(type(identifier) is str,"UNTYPED_CONTENT_ID")
        if identifier in seen:
            require(same(row,seen[identifier]),"CONFLICTING_DUPLICATE_ID")
        else:
            seen[identifier] = row; result.append(copy.deepcopy(row))
    return result

def catalogue_union(*catalogues):
    require(all(c["FormatVersion"] == 1 for c in catalogues),"CATALOGUE_FORMAT_CHANGED")
    result = {"FormatVersion":1}
    for group,key in KEYS.items(): result[group] = union([row for c in catalogues for row in c[group]],key)
    return result

def closure(catalogue,gates,applications):
    app_ids = {row["DefinitionId"] for row in applications}
    require(len(app_ids) == len(applications) == 20,"EXACT_TWENTY_APPLICATIONS_REQUIRED")
    versions = catalogue["Hulls"]+catalogue["Components"]; by_id = {row["VersionId"]:row for row in versions}
    require(len(by_id) == len(versions),"VERSION_ID_COLLISION")
    for group in (catalogue["Hulls"],catalogue["Components"]):
        revisions = set()
        for row in group:
            revision = (row["FamilyId"],row["VersionNumber"])
            require(revision not in revisions,"FAMILY_REVISION_COLLISION"); revisions.add(revision)
            seen = set(); current = row
            while current.get("ParentVersionId") is not None:
                identifier = current["ParentVersionId"]
                require(identifier in by_id and identifier not in seen,"MISSING_OR_CYCLIC_PARENT")
                parent = by_id[identifier]; require(parent["FamilyId"] == current["FamilyId"] and parent["VersionNumber"] < current["VersionNumber"],"PARENT_FAMILY_OR_REVISION_CHANGED")
                seen.add(identifier); current = parent
    require(len(gates) == len(by_id) and {g["VersionId"] for g in gates} == set(by_id),"EXACT_VERSION_ACCESS_COVERAGE_REQUIRED")
    for gate in gates:
        required = gate["RequiredEngineeringApplicationDefinitionIds"]
        require(len(set(required)) == len(required) and set(required) <= app_ids,"VERSION_GATE_MISSING_APPLICATION")
    by_app = {row["DefinitionId"]:row for row in applications}
    def visit(identifier,ancestors):
        require(identifier in by_app and identifier not in ancestors,"ENGINEERING_PREREQUISITE_CLOSURE")
        for parent in by_app[identifier]["PrerequisiteDefinitionIds"]: visit(parent,ancestors|{identifier})
    for identifier in by_app: visit(identifier,set())

def loader_hashes(files):
    core = digest(("campaign-tactical-review-package-v1\n"+digest(files["core.json"])+"\n"+digest(files["scenario.json"])+"\n").encode("ascii"))
    physical = digest(("campaign-physical-review-package-v2\n"+digest(files["physical.json"])+"\n"+core+"\n").encode("ascii"))
    integrated = digest(("campaign-integrated-playtest-package-v3\n"+digest(files["package.json"])+"\n"+physical+"\n").encode("ascii"))
    playable = digest(("campaign-playable-fleet-package-v6\n"+digest(files["playable.json"])+"\n"+integrated+"\n").encode("ascii"))
    return {"scenarioContentHash":digest(files["scenario.json"]),"corePackageHash":core,"physicalPackageHash":physical,"integratedPackageHash":integrated,"playablePackageHash":playable}

def prepare(manifest):
    require([pin["relativePath"] for pin in manifest["inputs"]] == INPUT_PATHS,"UNREVIEWED_INPUT_PATH_SET")
    inputs = {pin["relativePath"]:input_bytes(pin) for pin in manifest["inputs"]}
    old_raw = git(["show",SOURCE_COMMIT+":"+SOURCE_BLOB]); old = parse(old_raw)
    source_pin = manifest["sourceEngineeringScenario"]
    require(source_pin["commit"] == SOURCE_COMMIT and source_pin["gitPath"] == SOURCE_BLOB and len(old_raw) == source_pin["bytes"]
            and digest(old_raw).upper() == source_pin["sha256"],"SOURCE_TWENTY_BLOB_PIN_MISMATCH")
    require(git(["rev-parse",SOURCE_COMMIT+":"+SOURCE_BLOB]).decode("ascii").strip() == source_pin["blobObjectId"],"SOURCE_BLOB_OBJECT_ID_MISMATCH")
    base_raw = {name:inputs[BASE+name] for name in FILES}; base = parse(base_raw["scenario.json"])
    old_def = old["Definitions"]; source_cat = old_def["ShipDesignCatalogue"]
    require(len(old_def["EngineeringApplications"]) == 20 and len(source_cat["Hulls"]) == 3 and len(source_cat["Components"]) == 30,"SOURCE_CLOSURE_COUNTS_CHANGED")
    mapping = mapping_for(source_cat); inverse = {new:original for original,new in mapping.items()}
    imported = remap(source_cat,mapping); apps = remap(old_def["EngineeringApplications"],mapping); gates = remap(old_def["ShipDesignVersionAccess"],mapping)
    require(same(remap(imported,inverse),source_cat) and same(remap(apps,inverse),old_def["EngineeringApplications"])
            and same(remap(gates,inverse),old_def["ShipDesignVersionAccess"]),"IMPORTED_SEMANTIC_ROUNDTRIP_FAILED")
    activities_raw = inputs["scenarios/activities-v1/catalogue.json"]
    colonisation_raw = inputs["scenarios/colonisation-v1/catalogue.json"]
    activities,colonisation = parse(activities_raw),parse(colonisation_raw)
    ready = catalogue_union(activities,colonisation)
    candidate = remap(copy.deepcopy(base),mapping); definition = candidate["Definitions"]
    # Every current base row must be present unmodified in the imported parent closure.
    for group,key in KEYS.items():
        source_rows = {row[key]:row for row in source_cat[group]}
        require(all(row[key] in source_rows and same(row,source_rows[row[key]]) for row in base["Definitions"]["ShipDesignCatalogue"][group]),"BASE_CATALOGUE_OUTSIDE_REVIEWED_UTILITY_CLOSURE")
    original_apps = {row["DefinitionId"]:row for row in old_def["EngineeringApplications"]}
    require(all(row["DefinitionId"] in original_apps and same(row,original_apps[row["DefinitionId"]]) for row in base["Definitions"]["EngineeringApplications"]),"BASE_ENGINEERING_DRIFT")
    definition["ShipDesignCatalogue"] = catalogue_union(ready,imported)
    definition["EngineeringApplications"] = apps
    ready_gates = [{"VersionId":row["VersionId"],"RequiredEngineeringApplicationDefinitionIds":[]} for row in ready["Hulls"]+ready["Components"]]
    definition["ShipDesignVersionAccess"] = union(gates+ready_gates,"VersionId")
    definition["DefinitionIds"] = list(base["Definitions"]["DefinitionIds"])
    for app in apps:
        if app["DefinitionId"] not in definition["DefinitionIds"]: definition["DefinitionIds"].append(app["DefinitionId"])
    require(len(definition["DefinitionIds"]) == len(set(definition["DefinitionIds"])),"DEFINITION_ID_COLLISION")
    closure(definition["ShipDesignCatalogue"],definition["ShipDesignVersionAccess"],apps)
    require(same(remap(candidate["InitialState"],inverse),base["InitialState"]),"INITIAL_STATE_NUMERIC_RNG_OR_IDENTITY_CHANGED")
    untouched = remap(copy.deepcopy(candidate),inverse)
    for field in ("ShipDesignCatalogue","EngineeringApplications","ShipDesignVersionAccess","DefinitionIds"):
        untouched["Definitions"][field] = copy.deepcopy(base["Definitions"][field])
    require(same(untouched,base),"UNAPPROVED_SCENARIO_FIELD_CHANGED")
    for group,key in KEYS.items():
        merged = {row[key]:row for row in definition["ShipDesignCatalogue"][group]}
        require(all(same(row,merged[row[key]]) for row in ready[group]),"MODERN_READY_CATALOGUE_CHANGED")
    files = {"scenario.json":encoded(candidate)}
    for name in FILES[1:]:
        original = parse(base_raw[name]); mapped = remap(original,mapping)
        require(same(remap(mapped,inverse),original),"PROFILE_NON_ID_FACT_CHANGED")
        require(mapped.get("ProfileId") == original.get("ProfileId") and mapped.get("FormatVersion") == original.get("FormatVersion"),"WIRE_PROFILE_OR_FORMAT_CHANGED")
        files[name] = base_raw[name] if same(mapped,original) else encoded(mapped)
    hashes = loader_hashes(files)
    require(hashes["playablePackageHash"] == manifest["prospectivePackageHash"],"PROSPECTIVE_PACKAGE_HASH_RECIPE_MISMATCH")
    files["catalogue.json"] = encoded(definition["ShipDesignCatalogue"])
    files["ready-activities.catalogue.json"] = activities_raw; files["ready-colonisation.catalogue.json"] = colonisation_raw
    files["id-map.json"] = encoded({"originalToNew":mapping,"newToOriginal":inverse,"exactIdOnly":True,"textTokenFields":["EffectSummary"]})
    files["provenance.json"] = encoded({"status":"UNVALIDATED_NEW_CONTENT_VARIANT_DATA","sourceEngineeringScenario":source_pin,"inputs":manifest["inputs"],
        "namespacePolicy":"ALL_IMPORTED_UTILITY_COMPONENT_VERSION_AND_FAMILY_IDS_ONLY","originalParentClosurePreserved":True,"importedUnmapDeepEquality":True,
        "initialStateUnmapDeepEquality":True,"modernReadyCatalogueObjectEquality":True,"modernReadyRawCopiesUnchanged":True,"applicationCount":20,"derivedVersionCount":19,
        "catalogueCounts":{group:len(rows) for group,rows in definition["ShipDesignCatalogue"].items() if type(rows) is list},"hashes":hashes,
        "sourceOnlyNoPortablePackageValidation":True,"noRuntimeOrNativeAcceptance":True,"noGalaxyDefiantUpgradeOrArtRebind":True})
    return files,hashes

def guid_for(relative):
    return uuid.uuid5(META_NAMESPACE,"HobbyDS9/engineering-development-v1/"+relative).hex

def meta(guid,folder=False):
    return ("fileFormatVersion: 2\nguid: "+guid+"\n"+("folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n" if folder else "")).encode("ascii")

def metadata_universe():
    raw = git(["ls-files","-z","--cached","--others","--exclude-standard","--","client/HobbyDS9.Unity/Assets"])
    return sorted({entry.decode("utf-8") for entry in raw.split(b"\0") if entry.endswith(b".meta")})

def authority_pins(manifest):
    expected=[{"path":p,"bytes":n,"sha256":h} for p,n,h in AUTHORITIES]
    require(manifest.get("authorityInputs")==expected,"EXACT_STANDING_AND_CONTINUATION_AUTHORITY_PINS")
    for pin in expected:
        raw=plain(pin["path"]).read_bytes()
        require(len(raw)==pin["bytes"] and digest(raw).upper()==pin["sha256"],"OWNER_AUTHORITY_CHANGED")
        document=parse(raw)
        require(type(document.get("stopAtUsedPercent")) is int and document["stopAtUsedPercent"]==80
                and document.get("ordinaryUsageAllowedMustBe") is True,"OWNER_AUTHORITY_80_CONTRACT")
    return [pin["sha256"] for pin in expected]

def approved(pin,owner,manifest_sha,writer_sha,usage_sha,authorities):
    require(os.path.commonpath((os.path.abspath(pin["path"]),str(ADMISSION_DIRECTORY))) == str(ADMISSION_DIRECTORY)
            and Path(pin["path"]).suffix == ".json","ROLE_OUTSIDE_NEW_ADMISSION_DIRECTORY")
    path = plain(pin["path"]); raw = path.read_bytes()
    require(type(pin["bytes"]) is int and len(raw)==pin["bytes"] and digest(raw).upper()==pin["sha256"],"WRITE_ROLE_BYTES_PIN")
    role = parse(raw)
    require(role.get("owner")==owner and role.get("decision")=="ADMIT_"+TAG and role.get("closed") is True and role.get("durable") is True
            and role.get("sourceOnly") is False and role.get("dataWriteOnlyNoCensusLeaseNativeAuthority") is True,"ACTUAL_DUAL_DATA_WRITE_ROLE_REQUIRED")
    require(role.get("inputManifestSha256")==manifest_sha and role.get("writerSha256")==writer_sha
            and role.get("officialUsageSha256")==usage_sha
            and role.get("authoritySha256")==authorities
            and role.get("scenarioOutput")==SCENARIO_OUTPUT and role.get("resourceOutput")==RESOURCE_OUTPUT,"DATA_WRITE_BINDINGS")
    observed = dt.datetime.fromisoformat(role["observedUtc"].replace("Z","+00:00"))
    require(observed.tzinfo is not None and 0 <= (dt.datetime.now(dt.timezone.utc)-observed).total_seconds() < 300,"STALE_OR_FUTURE_WRITE_ROLE")
    return role

def actual_usage(pin):
    path=Path(os.path.abspath(pin["path"]))
    require(os.path.commonpath((str(path),str(ADMISSION_DIRECTORY)))==str(ADMISSION_DIRECTORY) and path.suffix==".json","USAGE_RECORD_BOUNDARY")
    raw=plain(path).read_bytes();require(len(raw)==pin["bytes"] and digest(raw).upper()==pin["sha256"],"USAGE_RECORD_PIN")
    record=json.loads(raw.decode("utf-8-sig"))
    require(record.get("owner")=="/root" and record.get("decision")=="ACTUAL_OFFICIAL_ENGINEERING_VARIANT_USAGE" and record.get("closed") is True
            and record.get("durable") is True and record.get("toolName")=="mcp__codex_app__get_usage_limits","ACTUAL_OFFICIAL_USAGE_REQUIRED")
    observed=dt.datetime.fromisoformat(record["observedUtc"].replace("Z","+00:00"))
    require(observed.tzinfo is not None and 0<=(dt.datetime.now(dt.timezone.utc)-observed).total_seconds()<300,"STALE_OR_FUTURE_OFFICIAL_USAGE")
    result=record["actualResult"];require(result.get("ordinaryUsageAllowed") is True,"ORDINARY_INCLUDED_USAGE_DENIED")
    buckets=result.get("rateLimitsByLimitId") or {"legacy":result.get("rateLimits")}
    require(type(buckets) is dict and bool(buckets),"USAGE_BUCKETS_REQUIRED")
    for bucket in buckets.values():
        require(type(bucket) is dict and {"primary","spendControlReached","rateLimitReachedType","individualLimit"}<=set(bucket)
                and bucket["spendControlReached"] is False and bucket["rateLimitReachedType"] is None and bucket["individualLimit"] is None,"USAGE_DENIAL_OR_AMBIGUITY")
        require(bucket["primary"] is not None,"PRIMARY_USAGE_REQUIRED")
        for key,value in bucket.items():
            if key in ("primary","secondary"):
                if value is None:continue
                require(type(value) is dict and "usedPercent" in value,"USAGE_WINDOW_REQUIRED")
            if type(value) is dict and "usedPercent" in value:
                number=value["usedPercent"];require(type(number) in (int,float) and math.isfinite(number) and 0<=number<80,"INCLUDED_USAGE_CEILING")
                duration=value.get("windowDurationMins");reset=value.get("resetsAt")
                require(type(duration) in (int,float) and math.isfinite(duration) and duration>0 and type(reset) is int
                        and observed.timestamp()<reset<=observed.timestamp()+duration*60+300,"USAGE_WINDOW_DURATION_OR_RESET_INVALID")

def create_file(path,raw):
    plain(path.parent); plain(path,missing_final=True)
    with path.open("xb") as output:
        require(output.write(raw)==len(raw),"NEW_FILE_SHORT_WRITE"); output.flush(); os.fsync(output.fileno())
    require(path.read_bytes()==raw,"NEW_FILE_ROUNDTRIP_MISMATCH")

def main():
    started = time.monotonic()
    try:
        class QuietParser(argparse.ArgumentParser):
            def error(self,message):raise Hold("INVALID_WRITER_INVOCATION")
        parser = QuietParser()
        parser.add_argument("--write",action="store_true");parser.add_argument("--manifest",required=True);parser.add_argument("--manifest-sha256",required=True)
        parser.add_argument("--root-pin",required=True);parser.add_argument("--frontend-pin",required=True);parser.add_argument("--usage-pin",required=True)
        args = parser.parse_args();require(args.write,"WRITER_SOURCE_REVIEW_AND_ACTUAL_ADMISSIONS_REQUIRED")
        require(Path(os.path.abspath(__file__)).parent.parent == REPOSITORY,"WRITER_REPOSITORY_BOUNDARY")
        require(Path(os.path.abspath(args.manifest)) == REPOSITORY/"scripts/engineering-content-variant-inputs.json","FIXED_INPUT_MANIFEST_PATH_REQUIRED")
        manifest_raw = plain(args.manifest).read_bytes(); manifest_sha = digest(manifest_raw).upper()
        require(manifest_sha==args.manifest_sha256,"INPUT_MANIFEST_PIN_MISMATCH"); manifest = parse(manifest_raw)
        require(manifest.get("status")=="PINNED_ENGINEERING_VARIANT_SOURCE_INPUTS_ONLY" and manifest.get("repository")==str(REPOSITORY),"EXACT_REVIEWED_INPUT_MANIFEST")
        authorities=authority_pins(manifest)
        writer_sha = digest(plain(__file__).read_bytes()).upper();root_pin,front_pin = parse(args.root_pin.encode()),parse(args.frontend_pin.encode())
        usage_pin=parse(args.usage_pin.encode());actual_usage(usage_pin)
        root = approved(root_pin,"/root",manifest_sha,writer_sha,usage_pin["sha256"],authorities);front = approved(front_pin,"/root/frontend",manifest_sha,writer_sha,usage_pin["sha256"],authorities)
        require(root["recordId"] == front["counterpartyId"] and front["recordId"] == root["counterpartyId"] and root["recordId"] != front["recordId"]
                and front.get("rootRecordPin")==root_pin,"RECIPROCAL_DATA_WRITE_RECORDS")
        require(metadata_universe()==[pin["relativePath"] for pin in manifest["unityMetadataInputs"]],"SOURCE_META_UNIVERSE_CHANGED")
        existing_guids = set()
        for pin in manifest["unityMetadataInputs"]:
            match = re.search(rb"(?m)^guid: ([0-9a-f]{32})\r?$",input_bytes(pin))
            require(match is not None,"SOURCE_META_GUID_MISSING");guid=match.group(1).decode("ascii")
            require(guid not in existing_guids,"PREEXISTING_SOURCE_META_GUID_COLLISION");existing_guids.add(guid)
        files,hashes = prepare(manifest)
        planned = {}
        for name,raw in files.items():
            planned[SCENARIO_OUTPUT+"/"+name] = raw;planned[RESOURCE_OUTPUT+"/"+name] = raw
            guid = guid_for(name);require(guid not in existing_guids,"NEW_META_GUID_COLLISION");existing_guids.add(guid)
            planned[RESOURCE_OUTPUT+"/"+name+".meta"] = meta(guid)
        folder_guid = guid_for("folder");require(folder_guid not in existing_guids,"NEW_FOLDER_GUID_COLLISION")
        planned[RESOURCE_OUTPUT+".meta"] = meta(folder_guid,True)
        manifest_guid = guid_for("manifest.json");require(manifest_guid not in existing_guids and manifest_guid != folder_guid,"NEW_MANIFEST_GUID_COLLISION")
        planned[RESOURCE_OUTPUT+"/manifest.json.meta"] = meta(manifest_guid)
        # Manifest hashes payload and metadata, excluding its own JSON to avoid a circular pin.
        report = encoded({"status":"NEW_DATA_ONLY_NOT_PACKAGE_VALIDATED","inputsManifestSha256":manifest_sha,"writerSha256":writer_sha,"hashes":hashes,
            "files":[{"relativePath":name,"bytes":len(raw),"sha256":digest(raw).upper()} for name,raw in sorted(planned.items())]})
        planned[SCENARIO_OUTPUT+"/manifest.json"] = report;planned[RESOURCE_OUTPUT+"/manifest.json"] = report
        # Validate ROOTS first. Never inspect descendants of an existing/reparse root.
        for relative in (SCENARIO_OUTPUT,RESOURCE_OUTPUT):
            target = REPOSITORY/relative;plain(target.parent);plain(target,missing_final=True)
            require(not os.path.lexists(target),"OUTPUT_ROOT_ALREADY_EXISTS")
        require(not os.path.lexists(REPOSITORY/(RESOURCE_OUTPUT+".meta")),"OUTPUT_FOLDER_META_ALREADY_EXISTS")
        for relative in planned:
            require(relative.startswith(SCENARIO_OUTPUT+"/") or relative.startswith(RESOURCE_OUTPUT+"/") or relative==RESOURCE_OUTPUT+".meta","OUTPUT_PLAN_ESCAPE")
        require(time.monotonic()-started < 30,"ORIGINAL_WRITER_30_EXPIRED")
        actual_usage(usage_pin)
        authority_pins(manifest)
        created_roots = {}
        for relative in (SCENARIO_OUTPUT,RESOURCE_OUTPUT):
            target=REPOSITORY/relative;plain(target.parent);target.mkdir();plain(target)
            info=target.stat();created_roots[relative]=(info.st_dev,info.st_ino)
        for relative,raw in planned.items():
            require(time.monotonic()-started < 30,"ORIGINAL_WRITER_30_EXPIRED")
            for root,identity in created_roots.items():
                info=plain(REPOSITORY/root).stat();require((info.st_dev,info.st_ino)==identity,"NEW_OUTPUT_ROOT_REPLACED")
            create_file(REPOSITORY/relative,raw)
        for pin in manifest["inputs"]+manifest["unityMetadataInputs"]:input_bytes(pin)
        expected_meta=sorted([pin["relativePath"] for pin in manifest["unityMetadataInputs"]]+[name for name in planned if name.endswith(".meta")])
        require(metadata_universe()==expected_meta,"POST_WRITE_METADATA_UNIVERSE_CHANGED")
        actual_usage(usage_pin)
        authority_pins(manifest)
        require(time.monotonic()-started < 30,"ORIGINAL_WRITER_30_EXPIRED")
        print(json.dumps({"status":"NEW_DATA_WRITTEN_NOT_PACKAGE_VALIDATED","prospectivePackageHash":hashes["playablePackageHash"],"filesCreated":len(planned)}));return 0
    except BaseException as error:
        print(json.dumps({"status":"HOLD_PRESERVE_PARTIAL_NEW_OUTPUTS_NO_RETRY","assertion":str(error) if isinstance(error,Hold) else "WRITER_EXCEPTION"}));return 1

if __name__ == "__main__":
    sys.exit(main())
