using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;

// Read only PE metadata/IL body length. Never load/execute a compiled Unity assembly.
var results=new List<object>();
foreach(var path in args)
{
    using var file=File.OpenRead(path);using var pe=new PEReader(file);
    var m=pe.GetMetadataReader();var definition=m.GetAssemblyDefinition();var framework="";
    foreach(var handle in definition.GetCustomAttributes())
    {
        var a=m.GetCustomAttribute(handle);if(a.Constructor.Kind!=HandleKind.MemberReference)continue;
        var parent=m.GetMemberReference((MemberReferenceHandle)a.Constructor).Parent;if(parent.Kind!=HandleKind.TypeReference)continue;
        var type=m.GetTypeReference((TypeReferenceHandle)parent);
        if(m.GetString(type.Namespace)!="System.Runtime.Versioning"||m.GetString(type.Name)!="TargetFrameworkAttribute")continue;
        var b=m.GetBlobReader(a.Value);if(b.ReadUInt16()!=1)throw new InvalidDataException();framework=b.ReadSerializedString()??"";
    }
    var types=new List<object>();
    foreach(var handle in m.TypeDefinitions)
    {
        var t=m.GetTypeDefinition(handle);var name=m.GetString(t.Namespace)+"."+m.GetString(t.Name);
        if(name!="HobbyDS9.UnityClient.ClientEntry"&&name!="HobbyDS9.UnityReview.ReviewLaunch")continue;
        var methods=t.GetMethods().Select(h=>m.GetMethodDefinition(h)).Select(method=>new {
            name=m.GetString(method.Name),ilBytes=method.RelativeVirtualAddress==0?0:pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!.Length,
            isStatic=(method.Attributes&MethodAttributes.Static)!=0,isPublic=(method.Attributes&MethodAttributes.MemberAccessMask)==MethodAttributes.Public
        }).ToArray();
        types.Add(new {name,methods});
    }
    var references=m.AssemblyReferences.Select(h=>m.GetString(m.GetAssemblyReference(h).Name)).OrderBy(v=>v,StringComparer.Ordinal).ToArray();
    results.Add(new {path=Path.GetFullPath(path),name=m.GetString(definition.Name),framework,references,types});
}
Console.WriteLine(JsonSerializer.Serialize(results));
