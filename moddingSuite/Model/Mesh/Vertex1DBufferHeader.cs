namespace moddingSuite.Model.Mesh
{
    /// <summary>
    /// One entry in the Vertex1DBufferHeaders section. Each entry is 16 bytes,
    /// mirroring the layout of Index1DBufferHeader:
    ///
    ///   bytes [0..3]   uint32  Offset  — byte offset into Vertex1DBufferStreams
    ///                                    verified: Offset[n+1] == Offset[n] + Length[n]
    ///   bytes [4..7]   uint32  Length  — byte length of this vertex buffer slice
    ///                                    actual vertex count = Length / stride
    ///
    ///   bytes [ 8.. 9] uint16  S0      — varies per entry; candidate: vertex count (Length / stride)
    ///   bytes [10..11] uint16  S1      — always 0x0000; padding
    ///   bytes [12..13] uint16  S2      — vertex format index; maps into VertexTypeNames / VertexFormats
    ///   bytes [14..15] uint16  S3      — always 0xC000; fixed flag word
    /// </summary>
    public class Vertex1DBufferHeader
    {
        /// <summary>
        /// Start position in Vertex1DBufferStreams in vertex units (not bytes).
        /// Offset[n+1] == Offset[n] + Length[n].
        /// </summary>
        public uint Offset { get; set; }

        /// <summary>Number of vertices in this slice.</summary>
        public uint Length { get; set; }

        /// <summary>
        /// Varies per entry; always fits in uint16.
        /// Candidate: per-vertex byte stride, or total byte size of this slice.
        /// Divide by Length to get bytes-per-vertex candidate.
        /// </summary>
        public ushort S0 { get; set; }

        /// <summary>Always 0x0000. Padding.</summary>
        public ushort S1 { get; set; }

        /// <summary>
        /// Vertex format / layout type. Observed values: 0, 1, 2, 3.
        /// Maps to an entry in the VertexTypeNames section (index into that string table).
        /// </summary>
        public ushort S2 { get; set; }

        /// <summary>Always 0xC000 (49152). Fixed format flag word, same as Index1DBufferHeader.S3.</summary>
        public ushort S3 { get; set; }
    }
}
