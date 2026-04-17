using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace moddingSuite.BL.Mesh
{
    /// <summary>
    /// Parses vertex type-name strings produced by the Eugen engine into a <see cref="Model.Mesh.VertexFormat"/>.
    ///
    /// Naming convention (double-underscore separated segments after the path):
    ///
    ///   TVertex__Semantic1_NX__Semantic2_NX__...
    ///
    /// where the suffix NX encodes element type and component count:
    ///
    ///   _Nf    → N × float32        (4 bytes each)   e.g. Position_3f   → 12 B
    ///   _Nubn  → N × uint8 norm     (1 byte  each)   e.g. NormalIn01_4ubn →  4 B
    ///   _Nub   → N × uint8 raw      (1 byte  each)   e.g. BlIdx_4ub      →  4 B
    ///   _Nwn   → N × uint16 norm    (2 bytes each)   e.g. TexCoord0_2wn  →  4 B
    /// </summary>
    public static class VertexFormatParser
    {
        // Matches the trailing _NX suffix: group 1 = N (count), group 2 = type code
        private static readonly Regex SuffixRegex =
            new Regex(@"_(\d+)(f|ubn|ub|wn)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static Model.Mesh.VertexFormat Parse(string typeName)
        {
            var fmt = new Model.Mesh.VertexFormat { TypeName = typeName };

            // Strip the path prefix up to and including the last '/'
            int slash = typeName.LastIndexOf('/');
            string bare = slash >= 0 ? typeName[(slash + 1)..] : typeName;

            // Split on double underscore; first segment is the class name ("TVertex"), skip it
            string[] segments = bare.Split(new[] { "__" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < segments.Length; i++)
            {
                string seg = segments[i];
                var m = SuffixRegex.Match(seg);
                if (!m.Success)
                    continue;

                int  count    = int.Parse(m.Groups[1].Value);
                string typeCode = m.Groups[2].Value.ToLowerInvariant();
                string semantic = seg[..m.Index];          // everything before the suffix

                var (elemType, elemBytes) = typeCode switch
                {
                    "f"   => (Model.Mesh.VertexElementType.Float32,    4),
                    "ubn" => (Model.Mesh.VertexElementType.UByteNorm,  1),
                    "ub"  => (Model.Mesh.VertexElementType.UByte,      1),
                    "wn"  => (Model.Mesh.VertexElementType.UShortNorm, 2),
                    _     => (Model.Mesh.VertexElementType.Unknown,    0),
                };

                fmt.Attributes.Add(new Model.Mesh.VertexAttribute
                {
                    Semantic       = semantic,
                    ElementType    = elemType,
                    ComponentCount = count,
                    ByteSize       = count * elemBytes,
                });
            }

            return fmt;
        }
    }
}
