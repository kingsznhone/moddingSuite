namespace moddingSuite.Model.Mesh
{
    /// <summary>
    /// One entry in the Index1DBufferHeaders section. Each entry is 16 bytes:
    ///
    ///   bytes [0..3]  uint32  Offset      — byte offset into Index1DBufferStreams
    ///                                       verified: Offset[n+1] == Offset[n] + Length[n]
    ///   bytes [4..7]  uint32  Length      — byte length of this index buffer slice
    ///                                       actual index count = Length / indexStride (2 for ushort)
    ///
    ///   The last 8 bytes are 4 × uint16:
    ///
    ///   bytes [ 8.. 9]  uint16  S0   — varies per entry; always fits in uint16 (< 65536)
    ///                                  candidate: vertex count referenced by this index slice
    ///                                  (= max valid vertex index + 1, used for bounds checking)
    ///   bytes [10..11]  uint16  S1   — always 0x0000 in observed data
    ///   bytes [12..13]  uint16  S2   — always 0x0001; encodes 16-bit index format
    ///   bytes [14..15]  uint16  S3   — always 0xC000 in observed data
    /// </summary>
    public class Index1DBufferHeader
    {
        /// <summary>
        /// Start position in Index1DBufferStreams in index units (not bytes).
        /// Offset[n+1] == Offset[n] + Length[n].
        /// </summary>
        public uint Offset { get; set; }

        /// <summary>Number of indices in this slice (triangles × 3 for tri-lists).</summary>
        public uint Length { get; set; }

        /// <summary>
        /// Varies per entry; fits in a uint16 (always &lt; 65536).
        /// Candidate: vertex count, stride, or sub-mesh ID. Needs cross-referencing with
        /// SingleMaterialMeshes / VertexTypeNames to determine meaning.
        /// </summary>
        public ushort S0 { get; set; }

        /// <summary>Always 0x0000 in all observed entries. Likely padding.</summary>
        public ushort S1 { get; set; }

        /// <summary>Always 0x0001 in all observed entries. Likely a fixed format version or type tag.</summary>
        public ushort S2 { get; set; }

        /// <summary>Always 0xC000 (49152) in all observed entries. Likely a fixed format/flag word.</summary>
        public ushort S3 { get; set; }
    }
}
