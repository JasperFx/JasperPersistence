#nullable enable
using System;
using System.Collections.Generic;
using JasperFx.CodeGeneration;
using JasperFx.CodeGeneration.Frames;

namespace Marten.Schema.Identity;

/// <summary>
///     User-assigned identity strategy
/// </summary>
public class NoOpIdGeneration: IIdGeneration
{
    public IEnumerable<Type> KeyTypes { get; } = new[] { typeof(int), typeof(long), typeof(string), typeof(Guid) };

    public bool RequiresSequences => false;

    public void GenerateCode(GeneratedMethod method, DocumentMapping mapping)
    {
        var document = new Use(mapping.DocumentType);
        method.Frames.Code($"return {{0}}.{mapping.IdMember.Name};", document);
    }
}
