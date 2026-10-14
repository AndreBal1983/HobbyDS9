using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;

// Host-only PE metadata inspection. Never load/execute a plugin or import this tool into Unity.
var inventory=new List<object>();
foreach(var path in args)
{
    using var stream=File.OpenRead(path);using var pe=new PEReader(stream);
    var metadata=pe.GetMetadataReader();var assembly=metadata.GetAssemblyDefinition();
    var framework="";
    foreach(var handle in assembly.GetCustomAttributes())
    {
        var attribute=metadata.GetCustomAttribute(handle);
        if(attribute.Constructor.Kind!=HandleKind.MemberReference)continue;
        var parent=metadata.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent;
        if(parent.Kind!=HandleKind.TypeReference)continue;
        var type=metadata.GetTypeReference((TypeReferenceHandle)parent);
        if(metadata.GetString(type.Namespace)!="System.Runtime.Versioning"||metadata.GetString(type.Name)!="TargetFrameworkAttribute")continue;
        var blob=metadata.GetBlobReader(attribute.Value);
        if(blob.ReadUInt16()!=1)throw new InvalidDataException("Invalid framework attribute.");
        framework=blob.ReadSerializedString()??"";
    }
    var names=metadata.TypeDefinitions.Select(h=>metadata.GetTypeDefinition(h)).Where(t=>(t.Attributes&TypeAttributes.VisibilityMask)==TypeAttributes.Public)
        .Select(t=>metadata.GetString(t.Namespace)+"."+metadata.GetString(t.Name)).OrderBy(s=>s,StringComparer.Ordinal).ToArray();
    var references=metadata.AssemblyReferences.Select(h=>metadata.GetString(metadata.GetAssemblyReference(h).Name)).OrderBy(s=>s,StringComparer.Ordinal).ToArray();
    var typeReferences=metadata.TypeReferences.Select(h=>metadata.GetTypeReference(h))
        .Select(t=>metadata.GetString(t.Namespace)+"."+metadata.GetString(t.Name)).OrderBy(s=>s,StringComparer.Ordinal).ToArray();
    inventory.Add(new {path=Path.GetFullPath(path),name=metadata.GetString(assembly.Name),framework,references,publicTypes=names,typeReferences});
}
Console.WriteLine(JsonSerializer.Serialize(inventory));
