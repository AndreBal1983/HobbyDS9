using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;

internal static class Program
{
    private static int Main(string[] args)
    {
        var rows=new List<object>();
        foreach(var path in args)
        {
            using var stream=File.OpenRead(path);
            using var pe=new PEReader(stream);
            var references=new List<string>();var imports=new SortedSet<string>(StringComparer.Ordinal);
            var entryPoints=new SortedSet<string>(StringComparer.Ordinal);var exports=new SortedSet<string>(StringComparer.Ordinal);
            string? name=null;string? version=null;
            if(pe.HasMetadata)
            {
                var md=pe.GetMetadataReader();var assembly=md.GetAssemblyDefinition();
                name=md.GetString(assembly.Name);version=assembly.Version.ToString();
                foreach(var h in md.AssemblyReferences)
                {var a=md.GetAssemblyReference(h);references.Add(md.GetString(a.Name)+", Version="+a.Version);}
                foreach(var h in md.MethodDefinitions)
                {
                    var m=md.GetMethodDefinition(h);
                    if((m.Attributes&MethodAttributes.PinvokeImpl)==0)continue;
                    var i=m.GetImport();imports.Add(md.GetString(md.GetModuleReference(i.Module).Name));entryPoints.Add(md.GetString(i.Name));
                }
            }
            var e=pe.PEHeaders.PEHeader!.ExportTableDirectory;
            if(e.RelativeVirtualAddress!=0)
            {
                var table=pe.GetSectionData(e.RelativeVirtualAddress).GetContent();
                var count=BitConverter.ToInt32(table.AsSpan(24,4));var names=BitConverter.ToInt32(table.AsSpan(32,4));
                var pointers=pe.GetSectionData(names).GetContent();
                for(var i=0;i<count;i++)
                {var rva=BitConverter.ToInt32(pointers.AsSpan(i*4,4));var bytes=pe.GetSectionData(rva).GetContent();exports.Add(System.Text.Encoding.ASCII.GetString(bytes.TakeWhile(b=>b!=0).ToArray()));}
            }
            if(!pe.HasMetadata)
            {
                var d=pe.PEHeaders.PEHeader!.ImportTableDirectory;
                if(d.RelativeVirtualAddress!=0)
                {
                    var table=pe.GetSectionData(d.RelativeVirtualAddress).GetContent();
                    for(var offset=0;offset+20<=table.Length;offset+=20)
                    {
                        var rva=BitConverter.ToInt32(table.AsSpan(offset+12,4));if(rva==0)break;
                        var bytes=pe.GetSectionData(rva).GetContent();
                        imports.Add(System.Text.Encoding.ASCII.GetString(bytes.TakeWhile(b=>b!=0).ToArray()));
                    }
                }
            }
            rows.Add(new{file=Path.GetFileName(path),path=Path.GetFullPath(path),length=stream.Length,
                sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant(),
                machine=pe.PEHeaders.CoffHeader.Machine.ToString(),managed=pe.HasMetadata,
                corFlags=pe.PEHeaders.CorHeader?.Flags.ToString(),name,version,references,imports,entryPoints,exports});
        }
        Console.WriteLine(JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));return 0;
    }
}
