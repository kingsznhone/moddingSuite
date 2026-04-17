using System.Collections.Generic;
using System.Linq;

namespace moddingSuite.Model.Mesh
{
    /// <summary>
    /// Decoded vertex format: the full attribute list and computed per-vertex byte stride.
    /// Parsed from a VertexTypeNames entry such as:
    ///   "$/M3D/System/VertexType/TVertex__Position_3f__NormalIn01_4ubn__TexCoord0_2wn__TexPackedAtlas0_4ubn"
    /// </summary>
    public class VertexFormat
    {
        /// <summary>Full raw type name string from the file.</summary>
        public string TypeName { get; set; }

        /// <summary>Decoded attribute list in declaration order.</summary>
        public List<VertexAttribute> Attributes { get; set; } = new();

        /// <summary>
        /// Total bytes per vertex = sum of all attribute byte sizes.
        /// This should match Vertex1DBufferHeader.S0 / Vertex1DBufferHeader.Length.
        /// </summary>
        public int Stride => Attributes.Sum(a => a.ByteSize);

        public override string ToString() => $"{TypeName} | stride={Stride}";
    }
}
