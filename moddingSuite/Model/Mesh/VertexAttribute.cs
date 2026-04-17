namespace moddingSuite.Model.Mesh
{
    /// <summary>
    /// The scalar element type of a vertex attribute, decoded from the type-name suffix.
    /// </summary>
    public enum VertexElementType
    {
        /// <summary>32-bit float  (_Nf)</summary>
        Float32,

        /// <summary>8-bit unsigned integer, NOT normalized  (_Nub)</summary>
        UByte,

        /// <summary>8-bit unsigned integer, normalized to [0,1]  (_Nubn)</summary>
        UByteNorm,

        /// <summary>16-bit unsigned integer, normalized to [0,1]  (_Nwn)</summary>
        UShortNorm,

        Unknown,
    }

    /// <summary>
    /// One decoded attribute (semantic + element layout) within a vertex format.
    /// </summary>
    public class VertexAttribute
    {
        /// <summary>Semantic name extracted from the type string (e.g. "Position", "NormalIn01", "TexCoord0").</summary>
        public string Semantic { get; set; }

        /// <summary>Scalar element type.</summary>
        public VertexElementType ElementType { get; set; }

        /// <summary>Number of scalar components (e.g. 3 for float3, 4 for 4ubn).</summary>
        public int ComponentCount { get; set; }

        /// <summary>Byte size of this attribute = ComponentCount × sizeof(ElementType).</summary>
        public int ByteSize { get; set; }

        public override string ToString() =>
            $"{Semantic} [{ElementType}×{ComponentCount} = {ByteSize}B]";
    }
}
